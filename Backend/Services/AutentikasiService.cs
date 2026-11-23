using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
    public record HasilDaftar(bool Berhasil, string Pesan, AkunOperator? Akun = null);

    // TokenSesi hanya terisi kalau operator mencentang "Ingat saya".
    public record HasilMasuk(bool Berhasil, string Pesan, AkunOperator? Akun = null, string? TokenSesi = null);

    // Aturan login, pendaftaran, dan persetujuan akun operator.
    // Tidak bergantung pada Avalonia, jadi bisa dites tanpa membuka aplikasi.
    public class AutentikasiService
    {
        public const int BatasGagalLogin = 5;
        public const int MenitKunci = 5;
        public const int MasaSesiHari = 30;

        private const string PesanLoginSalah = "Nama pengguna atau kata sandi salah.";
        private static readonly Regex PolaUsername = new("^[a-z0-9._]{3,30}$", RegexOptions.Compiled);

        private readonly AkunOperatorRepository _akunRepo;
        private readonly KodeUndanganRepository _kodeRepo;
        private readonly SesiLoginRepository _sesiRepo;

        public AutentikasiService(KoneksiDatabase db)
        {
            _akunRepo = new AkunOperatorRepository(db);
            _kodeRepo = new KodeUndanganRepository(db);
            _sesiRepo = new SesiLoginRepository(db);
        }

        // ---------- Pendaftaran ----------

        public async Task<HasilDaftar> DaftarAsync(
            string namaLengkap, string username, string email,
            string kataSandi, string konfirmasiKataSandi, string kodeUndangan)
        {
            namaLengkap = (namaLengkap ?? "").Trim();
            username = (username ?? "").Trim().ToLowerInvariant();
            email = (email ?? "").Trim().ToLowerInvariant();
            kodeUndangan = (kodeUndangan ?? "").Trim().ToUpperInvariant();
            kataSandi ??= "";

            if (namaLengkap.Length == 0)
                return new HasilDaftar(false, "Nama lengkap wajib diisi.");
            if (namaLengkap.Length > 100)
                return new HasilDaftar(false, "Nama lengkap maksimal 100 karakter.");
            if (!PolaUsername.IsMatch(username))
                return new HasilDaftar(false, "Username 3-30 karakter, hanya huruf, angka, titik, atau garis bawah.");
            if (!EmailValid(email))
                return new HasilDaftar(false, "Format email tidak valid.");
            if (kataSandi.Length < KataSandi.PanjangMinimal)
                return new HasilDaftar(false, $"Kata sandi minimal {KataSandi.PanjangMinimal} karakter.");
            if (kataSandi.Length > KataSandi.PanjangMaksimal)
                return new HasilDaftar(false, $"Kata sandi maksimal {KataSandi.PanjangMaksimal} karakter.");
            if (kataSandi != konfirmasiKataSandi)
                return new HasilDaftar(false, "Konfirmasi kata sandi tidak sama.");

            string hash = await Task.Run(() => KataSandi.Hash(kataSandi));

            try
            {
                var akun = await _akunRepo.DaftarAsync(namaLengkap, username, email, hash, kodeUndangan);
                return new HasilDaftar(true,
                    "Pendaftaran terkirim. Akun bisa dipakai setelah disetujui admin posko.", akun);
            }
            catch (PendaftaranDitolakException ex)
            {
                return new HasilDaftar(false, ex.Message);
            }
        }

        // ---------- Login ----------

        public async Task<HasilMasuk> MasukAsync(string username, string kataSandi, bool ingatSaya)
        {
            username = (username ?? "").Trim();
            kataSandi ??= "";

            if (username.Length == 0)
                return new HasilMasuk(false, "Nama pengguna wajib diisi.");
            if (kataSandi.Length == 0)
                return new HasilMasuk(false, "Kata sandi wajib diisi.");
            if (kataSandi.Length > KataSandi.PanjangMaksimal)
                return new HasilMasuk(false, PesanLoginSalah);

            var data = await _akunRepo.AmbilDataLoginAsync(username);
            if (data is null)
            {
                await Task.Run(() => KataSandi.HitungPalsu(kataSandi));
                return new HasilMasuk(false, PesanLoginSalah);
            }

            if (data.DetikTerkunci > 0)
                return new HasilMasuk(false, PesanTerkunci(data.DetikTerkunci));

            bool cocok = await Task.Run(() => KataSandi.Cocok(kataSandi, data.PasswordHash));
            if (!cocok)
            {
                bool terkunci = await _akunRepo.CatatLoginGagalAsync(data.Akun.Id, BatasGagalLogin, MenitKunci);
                return new HasilMasuk(false, terkunci ? PesanTerkunci(MenitKunci * 60) : PesanLoginSalah);
            }

            // Status akun baru diberi tahu setelah kata sandinya benar, supaya orang
            // lain tidak bisa mengecek status akun hanya dengan menebak username.
            if (data.Akun.Status == StatusAkun.Menunggu)
                return new HasilMasuk(false, "Akun belum disetujui admin posko.");
            if (data.Akun.Status == StatusAkun.Ditolak)
                return new HasilMasuk(false, "Pendaftaran akun ini ditolak admin posko.");
            if (data.Akun.Status == StatusAkun.Nonaktif)
                return new HasilMasuk(false, "Akun ini dinonaktifkan admin posko.");

            await _akunRepo.CatatLoginBerhasilAsync(data.Akun.Id);

            string? token = null;
            if (ingatSaya)
            {
                token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                await _sesiRepo.BuatAsync(data.Akun.Id, HashToken(token), MasaSesiHari);
            }

            return new HasilMasuk(true, "Berhasil masuk.", data.Akun, token);
        }

        // Dipakai saat aplikasi dibuka: token tersimpan -> akun, atau null kalau
        // sesi tidak berlaku lagi.
        public async Task<AkunOperator?> MasukDenganTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            int? operatorId = await _sesiRepo.CariOperatorIdAsync(HashToken(token));
            return operatorId is null ? null : await _akunRepo.CariPerIdAsync(operatorId.Value);
        }

        public Task KeluarAsync(string token)
        {
            return _sesiRepo.HapusAsync(HashToken(token));
        }

        // ---------- Ganti kata sandi sendiri ----------

        // Wajib membuktikan kata sandi lama. Setelah berhasil, semua sesi
        // "Ingat saya" akun ini dicabut, termasuk di komputer lain.
        public async Task<HasilDaftar> GantiKataSandiAsync(int akunId, string sandiLama, string sandiBaru, string konfirmasi)
        {
            sandiLama ??= "";
            sandiBaru ??= "";

            if (sandiLama.Length == 0)
                return new HasilDaftar(false, "Kata sandi lama wajib diisi.");
            if (sandiBaru.Length < KataSandi.PanjangMinimal)
                return new HasilDaftar(false, $"Kata sandi baru minimal {KataSandi.PanjangMinimal} karakter.");
            if (sandiBaru.Length > KataSandi.PanjangMaksimal)
                return new HasilDaftar(false, $"Kata sandi baru maksimal {KataSandi.PanjangMaksimal} karakter.");
            if (sandiBaru != konfirmasi)
                return new HasilDaftar(false, "Konfirmasi kata sandi baru tidak sama.");
            if (sandiBaru == sandiLama)
                return new HasilDaftar(false, "Kata sandi baru harus berbeda dari yang lama.");

            var akun = await _akunRepo.CariPerIdAsync(akunId);
            var data = akun is null ? null : await _akunRepo.AmbilDataLoginAsync(akun.Username);
            if (data is null || data.Akun.Status != StatusAkun.Aktif)
                return new HasilDaftar(false, "Akun tidak ditemukan atau tidak aktif.");

            bool cocok = await Task.Run(() => KataSandi.Cocok(sandiLama, data.PasswordHash));
            if (!cocok)
                return new HasilDaftar(false, "Kata sandi lama salah.");

            string hash = await Task.Run(() => KataSandi.Hash(sandiBaru));
            await _akunRepo.UbahPasswordHashAsync(akunId, hash);
            await _sesiRepo.HapusSemuaMilikAsync(akunId);
            return new HasilDaftar(true, "Kata sandi berhasil diganti.", data.Akun);
        }

        // ---------- Kelola akun (Admin dan SuperAdmin) ----------
        // Setiap aksi mengambil ulang akun pelaku dan akun target dari database,
        // lalu memakai aturan AkunOperator.BolehMengelola. Peran tidak pernah
        // dipercaya dari objek yang ada di memori UI.

        public async Task<List<AkunOperator>> AmbilSemuaAkunAsync(int pelakuId)
        {
            await PastikanPengelolaAsync(pelakuId);
            return await _akunRepo.AmbilSemuaAsync();
        }

        public async Task<List<AkunOperator>> AmbilPendaftarMenungguAsync(int pelakuId)
        {
            await PastikanPengelolaAsync(pelakuId);
            return await _akunRepo.AmbilPerStatusAsync(StatusAkun.Menunggu);
        }

        public Task<bool> SetujuiAsync(int pelakuId, int akunId)
        {
            return UbahStatusAsync(pelakuId, akunId, StatusAkun.Menunggu, StatusAkun.Aktif);
        }

        public Task<bool> TolakAsync(int pelakuId, int akunId)
        {
            return UbahStatusAsync(pelakuId, akunId, StatusAkun.Menunggu, StatusAkun.Ditolak);
        }

        // Akun yang dinonaktifkan langsung kehilangan semua sesi "Ingat saya".
        public async Task<bool> NonaktifkanAsync(int pelakuId, int akunId)
        {
            bool berubah = await UbahStatusAsync(pelakuId, akunId, StatusAkun.Aktif, StatusAkun.Nonaktif);
            if (berubah)
                await _sesiRepo.HapusSemuaMilikAsync(akunId);
            return berubah;
        }

        // Mengaktifkan lagi akun yang Nonaktif, atau memberi kesempatan kedua
        // pada pendaftaran yang sebelumnya Ditolak.
        public async Task<bool> AktifkanAsync(int pelakuId, int akunId)
        {
            var (pelaku, target) = await PastikanBolehMengelolaAsync(pelakuId, akunId);
            if (target.Status != StatusAkun.Nonaktif && target.Status != StatusAkun.Ditolak)
                return false;

            return await _akunRepo.UbahStatusAsync(akunId, target.Status, StatusAkun.Aktif, pelaku.Id);
        }

        // Hanya SuperAdmin yang boleh mengubah peran, dan hanya antara Admin dan Operator.
        public async Task<bool> UbahPeranAsync(int pelakuId, int akunId, PeranOperator peranBaru)
        {
            var (pelaku, _) = await PastikanBolehMengelolaAsync(pelakuId, akunId);
            if (!pelaku.SuperAdmin)
                throw new UnauthorizedAccessException("Hanya super admin yang boleh mengubah peran akun.");
            if (peranBaru == PeranOperator.SuperAdmin)
                throw new UnauthorizedAccessException("Peran super admin tidak bisa diberikan dari aplikasi.");

            return await _akunRepo.UbahPeranAsync(akunId, peranBaru);
        }

        // Membuat kata sandi sementara untuk akun lain. Kata sandi itu hanya
        // dikembalikan sekali di sini; database menyimpan hash-nya saja.
        public async Task<string> ResetKataSandiAsync(int pelakuId, int akunId)
        {
            await PastikanBolehMengelolaAsync(pelakuId, akunId);

            string sementara = BuatKataSandiSementara();
            string hash = await Task.Run(() => KataSandi.Hash(sementara));
            await _akunRepo.UbahPasswordHashAsync(akunId, hash);
            await _sesiRepo.HapusSemuaMilikAsync(akunId);
            return sementara;
        }

        public async Task<string?> AmbilKodeUndanganAsync(int pelakuId)
        {
            await PastikanPengelolaAsync(pelakuId);
            return await _kodeRepo.AmbilAktifAsync();
        }

        public async Task<string> BuatKodeUndanganAsync(int pelakuId)
        {
            await PastikanPengelolaAsync(pelakuId);
            return await _kodeRepo.BuatBaruAsync(pelakuId);
        }

        private async Task<bool> UbahStatusAsync(int pelakuId, int akunId, StatusAkun asal, StatusAkun baru)
        {
            var (pelaku, _) = await PastikanBolehMengelolaAsync(pelakuId, akunId);
            return await _akunRepo.UbahStatusAsync(akunId, asal, baru, pelaku.Id);
        }

        private async Task<AkunOperator> PastikanPengelolaAsync(int pelakuId)
        {
            var pelaku = await _akunRepo.CariPerIdAsync(pelakuId);
            if (pelaku is null || !pelaku.Pengelola || pelaku.Status != StatusAkun.Aktif)
                throw new UnauthorizedAccessException("Hanya admin posko yang boleh melakukan ini.");
            return pelaku;
        }

        private async Task<(AkunOperator Pelaku, AkunOperator Target)> PastikanBolehMengelolaAsync(int pelakuId, int akunId)
        {
            var pelaku = await PastikanPengelolaAsync(pelakuId);
            var target = await _akunRepo.CariPerIdAsync(akunId)
                         ?? throw new UnauthorizedAccessException("Akun tidak ditemukan.");

            if (!pelaku.BolehMengelola(target))
                throw new UnauthorizedAccessException("Anda tidak berwenang mengelola akun ini.");

            return (pelaku, target);
        }

        // ---------- Bantuan ----------

        // 12 karakter acak tanpa huruf/angka yang mirip (0/O, 1/l/I).
        private static string BuatKataSandiSementara()
        {
            const string alfabet = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var huruf = new char[12];
            for (int i = 0; i < huruf.Length; i++)
            {
                huruf[i] = alfabet[RandomNumberGenerator.GetInt32(alfabet.Length)];
            }
            return new string(huruf);
        }

        private static string PesanTerkunci(int detik)
        {
            int menit = Math.Max(1, (int)Math.Ceiling(detik / 60.0));
            return $"Terlalu banyak percobaan gagal. Coba lagi dalam {menit} menit.";
        }

        private static bool EmailValid(string email)
        {
            if (email.Length == 0 || email.Length > 254 || email.Contains(' '))
                return false;
            if (!MailAddress.TryCreate(email, out var alamat) || alamat.Address != email)
                return false;
            return alamat.Host.Contains('.') && !alamat.Host.StartsWith('.') && !alamat.Host.EndsWith('.');
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }
    }
}

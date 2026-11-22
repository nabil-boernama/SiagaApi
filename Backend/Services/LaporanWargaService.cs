using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
    // Satu baris di tabel "Laporan Warga" pada UI. Tidak bergantung pada Avalonia,
    // jadi bisa dites tanpa membuka aplikasi.
    public record RingkasanLaporan(
        int Id,
        DateTime DibuatPadaUtc,
        string Pelapor,
        string NamaWilayah,
        Keparahan Tingkat,
        StatusLaporan Status,
        int JumlahHotspotDekat);

    // Penghubung halaman Laporan Warga dengan database: ambil daftar wilayah,
    // ambil daftar laporan, dan simpan laporan baru lalu verifikasi otomatis
    // terhadap titik panas di sekitarnya (LaporanWarga.Verifikasi, radius 5 km).
    public class LaporanWargaService
    {
        private readonly WilayahRepository _wilayahRepo;
        private readonly TitikPanasRepository _titikPanasRepo;
        private readonly LaporanWargaRepository _laporanRepo;

        public LaporanWargaService(KoneksiDatabase db)
        {
            _wilayahRepo = new WilayahRepository(db);
            _titikPanasRepo = new TitikPanasRepository(db);
            _laporanRepo = new LaporanWargaRepository(db);
        }

        // Pilihan wilayah di form: tingkat kabupaten saja, karena laporan warga
        // selalu terjadi di lokasi yang lebih spesifik dari provinsi.
        public async Task<List<Wilayah>> AmbilKabupatenAsync()
        {
            var semua = await _wilayahRepo.AmbilSemuaAsync();
            return semua.Where(w => w.Tingkat == TingkatWilayah.Kabupaten).ToList();
        }

        // Daftar laporan terbaru (tanpa yang Ditolak, lihat override di LaporanWargaRepository).
        public async Task<List<RingkasanLaporan>> AmbilDaftarAsync()
        {
            var wilayahTask = _wilayahRepo.AmbilSemuaAsync();
            // Cukup hotspot 30 hari terakhir: laporan dicocokkan dengan deteksi di sekitar waktunya.
            var titikPanasTask = _titikPanasRepo.AmbilSejakAsync(DateTime.UtcNow.AddDays(-30));
            var laporanTask = _laporanRepo.AmbilSemuaAsync();
            await Task.WhenAll(wilayahTask, titikPanasTask, laporanTask);

            var namaWilayah = wilayahTask.Result.ToDictionary(w => w.Id, w => w.Nama);
            var titikPanas = titikPanasTask.Result;

            return laporanTask.Result
                .Select(l => KeRingkasan(l, namaWilayah, titikPanas))
                .ToList();
        }

        // CREATE + UPDATE: simpan laporan baru, lalu kalau ada titik panas dalam
        // radius 5 km, status langsung diubah jadi Diverifikasi.
        public async Task<RingkasanLaporan> KirimAsync(
            string pelapor, int wilayahId, Keparahan tingkat, string deskripsi)
        {
            var wilayah = (await _wilayahRepo.AmbilSemuaAsync()).FirstOrDefault(w => w.Id == wilayahId)
                          ?? throw new ArgumentException("Wilayah tidak ditemukan.", nameof(wilayahId));

            // Form belum punya input koordinat, jadi lokasi laporan memakai
            // titik tengah kabupaten yang dipilih.
            var laporan = new LaporanWarga
            {
                Pelapor = pelapor,
                Deskripsi = deskripsi,
                Tingkat = tingkat,
                Status = StatusLaporan.Baru,
                Lat = wilayah.Lat,
                Lon = wilayah.Lon,
                WilayahId = wilayah.Id
            };

            await _laporanRepo.SimpanAsync(laporan);

            var titikPanas = await _titikPanasRepo.AmbilSejakAsync(DateTime.UtcNow.AddHours(-JendelaCocokJam));
            int jumlahDekat = HitungHotspotDekat(laporan, titikPanas);

            if (jumlahDekat > 0)
            {
                laporan.Status = StatusLaporan.Diverifikasi;
                await _laporanRepo.UbahStatusAsync(laporan.Id, laporan.Status);
            }

            return new RingkasanLaporan(
                laporan.Id, laporan.DibuatPada, laporan.Pelapor, wilayah.Nama, laporan.Tingkat, laporan.Status, jumlahDekat);
        }

        private static RingkasanLaporan KeRingkasan(
            LaporanWarga l, IReadOnlyDictionary<int, string> namaWilayah, List<TitikPanas> titikPanas)
        {
            return new RingkasanLaporan(
                l.Id,
                l.DibuatPada,
                l.Pelapor,
                namaWilayah.TryGetValue(l.WilayahId, out var nama) ? nama : "-",
                l.Tingkat,
                l.Status,
                HitungHotspotDekat(l, titikPanas));
        }

        // Hotspot dianggap cocok dengan laporan kalau jaraknya dalam 5 km DAN
        // terdeteksi dalam 48 jam sebelum/sesudah laporan dibuat. Tanpa batas
        // waktu, hotspot lama di tempat yang sama akan ikut "memverifikasi"
        // laporan baru.
        private const double JendelaCocokJam = 48;

        // Memakai aturan domain LaporanWarga.Verifikasi (Haversine, radius 5 km).
        // Verifikasi mengubah Status, jadi dijalankan pada salinan agar laporan
        // aslinya tidak ikut berubah.
        private static int HitungHotspotDekat(LaporanWarga laporan, IEnumerable<TitikPanas> titikPanas)
        {
            // Laporan yang belum tersimpan belum punya DibuatPada; anggap dibuat sekarang.
            DateTime waktuLaporan = laporan.DibuatPada == default ? DateTime.UtcNow : laporan.DibuatPada;
            return titikPanas.Count(tp =>
                Math.Abs((tp.WaktuDeteksi - waktuLaporan).TotalHours) <= JendelaCocokJam &&
                new LaporanWarga { Lat = laporan.Lat, Lon = laporan.Lon }.Verifikasi(tp));
        }
    }
}

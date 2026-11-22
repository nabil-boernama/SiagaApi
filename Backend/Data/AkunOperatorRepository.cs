using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Data
{
    // Data yang hanya dibutuhkan saat pengecekan login. Hash kata sandi
    // berhenti di AutentikasiService dan tidak pernah sampai ke UI.
    public record DataLogin(AkunOperator Akun, string PasswordHash, int DetikTerkunci);

    // Dilempar saat pendaftaran ditolak karena aturan (kode undangan salah,
    // username/email sudah dipakai). Pesannya aman ditampilkan ke pengguna.
    public class PendaftaranDitolakException : Exception
    {
        public PendaftaranDitolakException(string pesan) : base(pesan)
        {
        }
    }

    public class AkunOperatorRepository : Repository<AkunOperator>
    {
        private const string KolomSelect =
            @"SELECT id, nama_lengkap, username, email, peran, status, dibuat_pada
              FROM akun_operator";

        public AkunOperatorRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "akun_operator";

        protected override string QueryAmbilSemua => KolomSelect + " ORDER BY dibuat_pada DESC";

        protected override AkunOperator Petakan(NpgsqlDataReader rd)
        {
            return new AkunOperator
            {
                Id = rd.GetInt32(0),
                NamaLengkap = rd.GetString(1),
                Username = rd.GetString(2),
                Email = rd.GetString(3),
                Peran = Enum.Parse<PeranOperator>(rd.GetString(4)),
                Status = Enum.Parse<StatusAkun>(rd.GetString(5)),
                DibuatPada = rd.GetDateTime(6)
            };
        }

        public async Task<AkunOperator?> CariPerIdAsync(int id)
        {
            var hasil = await JalankanQueryAsync(KolomSelect + " WHERE id = @id", new NpgsqlParameter("id", id));
            return hasil.FirstOrDefault();
        }

        public Task<List<AkunOperator>> AmbilPerStatusAsync(StatusAkun status)
        {
            return JalankanQueryAsync(
                KolomSelect + " WHERE status = @status ORDER BY dibuat_pada",
                new NpgsqlParameter("status", status.ToString()));
        }

        // READ untuk login: akun + hash + sisa waktu terkunci (0 kalau tidak terkunci).
        // Sisa waktu dihitung di database supaya tidak bergantung jam komputer operator.
        public async Task<DataLogin?> AmbilDataLoginAsync(string username)
        {
            const string sql =
                @"SELECT id, nama_lengkap, username, email, peran, status, dibuat_pada,
                         password_hash,
                         GREATEST(0, CEIL(EXTRACT(EPOCH FROM (terkunci_sampai - (NOW() AT TIME ZONE 'UTC')))))::int
                  FROM akun_operator
                  WHERE LOWER(username) = LOWER(@username)";

            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);
            await using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return null;

            return new DataLogin(Petakan(rd), rd.GetString(7), rd.IsDBNull(8) ? 0 : rd.GetInt32(8));
        }

        // UPDATE: salah kata sandi. Setelah batas tercapai akun dikunci sementara
        // dan hitungan diulang dari nol. Mengembalikan true kalau akun jadi terkunci.
        public async Task<bool> CatatLoginGagalAsync(int id, int batasGagal, int menitKunci)
        {
            const string sql =
                @"UPDATE akun_operator
                  SET terkunci_sampai = CASE WHEN gagal_login + 1 >= @batas
                                             THEN (NOW() AT TIME ZONE 'UTC') + make_interval(mins => @menit)
                                             ELSE terkunci_sampai END,
                      gagal_login     = CASE WHEN gagal_login + 1 >= @batas THEN 0 ELSE gagal_login + 1 END
                  WHERE id = @id
                  RETURNING gagal_login = 0";

            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("batas", batasGagal);
            cmd.Parameters.AddWithValue("menit", menitKunci);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteScalarAsync() is true;
        }

        public async Task CatatLoginBerhasilAsync(int id)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"UPDATE akun_operator
                  SET gagal_login = 0, terkunci_sampai = NULL, login_terakhir = (NOW() AT TIME ZONE 'UTC')
                  WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // CREATE: daftar akun baru. Selalu jadi Operator berstatus Menunggu, dan
        // hanya tersimpan kalau kode undangannya aktif. Pengecekan kode dan
        // penyimpanan digabung dalam satu perintah SQL, jadi kode yang dimatikan
        // admin di tengah proses tidak bisa lolos.
        public async Task<AkunOperator> DaftarAsync(
            string namaLengkap, string username, string email, string passwordHash, string kodeUndangan)
        {
            const string sql =
                @"INSERT INTO akun_operator (nama_lengkap, username, email, password_hash, peran, status)
                  SELECT @nama, @username, @email, @hash, 'Operator', 'Menunggu'
                  WHERE EXISTS (SELECT 1 FROM kode_undangan WHERE aktif AND kode = @kode)
                  RETURNING id, nama_lengkap, username, email, peran, status, dibuat_pada";

            try
            {
                await using var conn = await Db.BukaKoneksiAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("nama", namaLengkap);
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("hash", passwordHash);
                cmd.Parameters.AddWithValue("kode", kodeUndangan);

                await using var rd = await cmd.ExecuteReaderAsync();
                if (!await rd.ReadAsync())
                    throw new PendaftaranDitolakException("Kode undangan posko tidak valid. Minta kode terbaru ke admin posko.");

                return Petakan(rd);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new PendaftaranDitolakException(
                    ex.ConstraintName == "uq_akun_operator_email"
                        ? "Email ini sudah terdaftar."
                        : "Username ini sudah dipakai. Pilih username lain.");
            }
        }

        // UPDATE: keputusan admin atas akun yang masih Menunggu. Mengembalikan
        // false kalau akun tidak ada atau sudah diproses admin lain.
        public async Task<bool> ProsesPendaftaranAsync(int id, StatusAkun keputusan, int adminId)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"UPDATE akun_operator SET status = @status, diproses_oleh = @admin
                  WHERE id = @id AND status = 'Menunggu'", conn);
            cmd.Parameters.AddWithValue("status", keputusan.ToString());
            cmd.Parameters.AddWithValue("admin", adminId);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        // UPDATE: aktifkan / nonaktifkan akun. statusAsal ikut dicek supaya dua
        // admin yang menekan tombol bersamaan tidak saling menimpa.
        public async Task<bool> UbahStatusAsync(int id, StatusAkun statusAsal, StatusAkun statusBaru, int adminId)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"UPDATE akun_operator SET status = @baru, diproses_oleh = @admin
                  WHERE id = @id AND status = @asal", conn);
            cmd.Parameters.AddWithValue("baru", statusBaru.ToString());
            cmd.Parameters.AddWithValue("asal", statusAsal.ToString());
            cmd.Parameters.AddWithValue("admin", adminId);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        // UPDATE: ubah peran. Akun SuperAdmin tidak pernah tersentuh oleh perintah ini.
        public async Task<bool> UbahPeranAsync(int id, PeranOperator peranBaru)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"UPDATE akun_operator SET peran = @peran
                  WHERE id = @id AND peran <> 'SuperAdmin' AND peran <> @peran", conn);
            cmd.Parameters.AddWithValue("peran", peranBaru.ToString());
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        // UPDATE: ganti hash kata sandi, sekaligus membuka kunci percobaan login.
        public async Task<bool> UbahPasswordHashAsync(int id, string passwordHash)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"UPDATE akun_operator SET password_hash = @hash, gagal_login = 0, terkunci_sampai = NULL
                  WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("hash", passwordHash);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }
    }
}

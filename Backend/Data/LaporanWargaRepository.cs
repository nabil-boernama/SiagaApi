using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Data
{
    public class LaporanWargaRepository : Repository<LaporanWarga>
    {
        private const string KolomSelect =
            @"SELECT id, pelapor, deskripsi, tingkat, status, dibuat_pada, lat, lon, wilayah_id
              FROM laporan_warga";

        public LaporanWargaRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "laporan_warga";

        protected override string QueryAmbilSemua =>
            KolomSelect + " ORDER BY dibuat_pada DESC";

        // Enum Keparahan dan StatusLaporan disimpan sebagai teks di database
        // ("Berat", "Diverifikasi", ...) lalu dikembalikan jadi enum di sini.
        protected override LaporanWarga Petakan(NpgsqlDataReader rd)
        {
            return new LaporanWarga
            {
                Id = rd.GetInt32(0),
                Pelapor = rd.GetString(1),
                Deskripsi = rd.GetString(2),
                Tingkat = Enum.Parse<Keparahan>(rd.GetString(3)),
                Status = Enum.Parse<StatusLaporan>(rd.GetString(4)),
                DibuatPada = rd.GetDateTime(5),
                Lat = rd.GetDouble(6),
                Lon = rd.GetDouble(7),
                WilayahId = rd.GetInt32(8)
            };
        }

        // POLYMORPHISM (override): untuk laporan, yang tampil di daftar utama
        // cukup yang belum ditolak, jadi perilaku AmbilSemuaAsync() di kelas
        // dasar diganti.
        public override Task<List<LaporanWarga>> AmbilSemuaAsync()
        {
            return JalankanQueryAsync(
                KolomSelect + " WHERE status <> 'Ditolak' ORDER BY dibuat_pada DESC");
        }

        public Task<List<LaporanWarga>> AmbilPerStatusAsync(StatusLaporan status)
        {
            return JalankanQueryAsync(
                KolomSelect + " WHERE status = @status ORDER BY dibuat_pada DESC",
                new NpgsqlParameter("status", status.ToString()));
        }

        // CREATE: simpan laporan baru dari form. Id dan DibuatPada diisi balik
        // dari database ke objek yang sama.
        public async Task<LaporanWarga> SimpanAsync(LaporanWarga laporan)
        {
            if (string.IsNullOrWhiteSpace(laporan.Pelapor))
                throw new ArgumentException("Nama pelapor wajib diisi.", nameof(laporan));
            if (string.IsNullOrWhiteSpace(laporan.Deskripsi))
                throw new ArgumentException("Deskripsi wajib diisi.", nameof(laporan));

            const string sql =
                @"INSERT INTO laporan_warga (pelapor, deskripsi, tingkat, status, lat, lon, wilayah_id)
                  VALUES (@pelapor, @deskripsi, @tingkat, @status, @lat, @lon, @wilayah_id)
                  RETURNING id, dibuat_pada";

            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("pelapor", laporan.Pelapor.Trim());
            cmd.Parameters.AddWithValue("deskripsi", laporan.Deskripsi.Trim());
            cmd.Parameters.AddWithValue("tingkat", laporan.Tingkat.ToString());
            cmd.Parameters.AddWithValue("status", laporan.Status.ToString());
            cmd.Parameters.AddWithValue("lat", laporan.Lat);
            cmd.Parameters.AddWithValue("lon", laporan.Lon);
            cmd.Parameters.AddWithValue("wilayah_id", laporan.WilayahId);

            await using var rd = await cmd.ExecuteReaderAsync();
            await rd.ReadAsync();
            laporan.Id = rd.GetInt32(0);
            laporan.DibuatPada = rd.GetDateTime(1);
            return laporan;
        }

        // UPDATE: dipakai setelah LaporanWarga.Verifikasi(titikPanas) mengubah Status.
        public async Task<bool> UbahStatusAsync(int id, StatusLaporan status)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                "UPDATE laporan_warga SET status = @status WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("status", status.ToString());
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;
using SiagaApi.Backend.Services;

namespace SiagaApi.Backend.Data
{
    // Satu baris log notifikasi untuk ditampilkan di halaman Langganan.
    public record CatatanNotifikasi(
        DateTime WaktuUtc, string Email, string NamaWilayah, string Confidence, HasilNotifikasi Hasil, string Keterangan);

    // Riwayat notifikasi di database. Sekaligus mengisi kontrak
    // IRiwayatNotifikasiStore yang diminta NotifikasiService (milik Nabil),
    // jadi service itu bisa dipakai tanpa diubah.
    public class RiwayatNotifikasiRepository : Repository<RiwayatNotifikasi>, IRiwayatNotifikasiStore
    {
        public RiwayatNotifikasiRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "riwayat_notifikasi";

        protected override string QueryAmbilSemua =>
            @"SELECT id, waktu_dikirim, hasil, keterangan, subscriber_id, titik_panas_id
              FROM riwayat_notifikasi
              ORDER BY waktu_dikirim DESC, id DESC";

        protected override RiwayatNotifikasi Petakan(NpgsqlDataReader rd)
        {
            return new RiwayatNotifikasi
            {
                Id = rd.GetInt32(0),
                WaktuDikirim = rd.GetDateTime(1),
                Hasil = Enum.Parse<HasilNotifikasi>(rd.GetString(2)),
                Keterangan = rd.GetString(3),
                SubscriberId = rd.GetInt32(4),
                TitikPanasId = rd.GetInt32(5)
            };
        }

        public async Task<bool> SudahPernahDikirim(int subscriberId, int titikPanasId)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"SELECT EXISTS (SELECT 1 FROM riwayat_notifikasi
                                 WHERE subscriber_id = @subscriber AND titik_panas_id = @titik)", conn);
            cmd.Parameters.AddWithValue("subscriber", subscriberId);
            cmd.Parameters.AddWithValue("titik", titikPanasId);
            return await cmd.ExecuteScalarAsync() is true;
        }

        // Waktu diisi database (UTC), bukan dari jam komputer operator.
        public async Task Catat(RiwayatNotifikasi riwayat)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO riwayat_notifikasi (hasil, keterangan, subscriber_id, titik_panas_id)
                  VALUES (@hasil, @keterangan, @subscriber, @titik)
                  ON CONFLICT (subscriber_id, titik_panas_id) DO NOTHING", conn);
            cmd.Parameters.AddWithValue("hasil", riwayat.Hasil.ToString());
            cmd.Parameters.AddWithValue("keterangan", riwayat.Keterangan);
            cmd.Parameters.AddWithValue("subscriber", riwayat.SubscriberId);
            cmd.Parameters.AddWithValue("titik", riwayat.TitikPanasId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Log terbaru beserta email, wilayah, dan confidence hotspot pemicunya.
        public async Task<List<CatatanNotifikasi>> AmbilTerbaruAsync(int batas)
        {
            const string sql =
                @"SELECT r.waktu_dikirim, s.email, w.nama, t.confidence, r.hasil, r.keterangan
                  FROM riwayat_notifikasi r
                  JOIN subscriber s ON s.id = r.subscriber_id
                  JOIN wilayah w ON w.id = s.wilayah_id
                  JOIN titik_panas t ON t.id = r.titik_panas_id
                  ORDER BY r.waktu_dikirim DESC, r.id DESC
                  LIMIT @batas";

            var hasil = new List<CatatanNotifikasi>();
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("batas", batas);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                hasil.Add(new CatatanNotifikasi(
                    rd.GetDateTime(0), rd.GetString(1), rd.GetString(2), rd.GetString(3),
                    Enum.Parse<HasilNotifikasi>(rd.GetString(4)), rd.GetString(5)));
            }
            return hasil;
        }
    }
}

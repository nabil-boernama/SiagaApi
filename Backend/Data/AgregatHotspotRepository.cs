using System;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;
using SiagaApi.Backend.Services;

namespace SiagaApi.Backend.Data
{
    // Hitungan per provinsi untuk "mode lihat nasional". Mengisi kontrak
    // IAgregatHotspotQuery yang diminta AgregasiNasionalService (milik Nabil),
    // jadi service itu bisa dipakai tanpa diubah.
    public class AgregatHotspotRepository : IAgregatHotspotQuery
    {
        private readonly KoneksiDatabase _db;

        public AgregatHotspotRepository(KoneksiDatabase db)
        {
            _db = db;
        }

        // Hotspot 24 jam terakhir di semua kabupaten sebuah provinsi.
        public async Task<int> HitungJumlahHotspot(int wilayahProvinsiId)
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"SELECT COUNT(*)
                  FROM titik_panas t
                  JOIN wilayah w ON w.id = t.wilayah_id
                  WHERE (w.wilayah_induk_id = @provinsi OR w.id = @provinsi)
                    AND t.waktu_deteksi >= (NOW() AT TIME ZONE 'UTC') - INTERVAL '24 hours'", conn);
            cmd.Parameters.AddWithValue("provinsi", wilayahProvinsiId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Status siaga yang paling banyak muncul di antara indeks risiko terbaru
        // kabupaten-kabupatennya. Kalau jumlahnya sama, diambil yang lebih berat.
        // Provinsi yang belum punya indeks risiko dianggap Hijau.
        public async Task<StatusSiaga> AmbilStatusDominan(int wilayahProvinsiId)
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"SELECT terbaru.status
                  FROM (
                      SELECT DISTINCT ON (r.wilayah_id) r.status
                      FROM indeks_risiko r
                      JOIN wilayah w ON w.id = r.wilayah_id
                      WHERE w.wilayah_induk_id = @provinsi
                      ORDER BY r.wilayah_id, r.waktu DESC
                  ) terbaru
                  GROUP BY terbaru.status
                  ORDER BY COUNT(*) DESC,
                           CASE terbaru.status WHEN 'Merah' THEN 3 WHEN 'Kuning' THEN 2 ELSE 1 END DESC
                  LIMIT 1", conn);
            cmd.Parameters.AddWithValue("provinsi", wilayahProvinsiId);
            return await cmd.ExecuteScalarAsync() is string status
                ? Enum.Parse<StatusSiaga>(status)
                : StatusSiaga.Hijau;
        }
    }
}

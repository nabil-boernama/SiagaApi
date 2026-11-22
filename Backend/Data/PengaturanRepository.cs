using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace SiagaApi.Backend.Data
{
    // Catatan satu pengambilan data dari luar.
    public record CatatanSinkron(string Sumber, DateTime WaktuUtc, bool Berhasil, int Jumlah, string Keterangan);

    // Pengaturan posko (pasangan kunci-nilai) dan catatan sinkronisasi.
    public class PengaturanRepository
    {
        private readonly KoneksiDatabase _db;

        public PengaturanRepository(KoneksiDatabase db)
        {
            _db = db;
        }

        public async Task<Dictionary<string, string>> AmbilSemuaAsync()
        {
            var hasil = new Dictionary<string, string>();
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand("SELECT kunci, nilai FROM pengaturan", conn);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                hasil[rd.GetString(0)] = rd.GetString(1);
            }
            return hasil;
        }

        // CREATE atau UPDATE (upsert) beberapa pengaturan dalam satu transaksi.
        public async Task SimpanAsync(IReadOnlyDictionary<string, string> nilai, int diubahOleh)
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var trx = await conn.BeginTransactionAsync();
            foreach (var (kunci, isi) in nilai)
            {
                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO pengaturan (kunci, nilai, diubah_oleh) VALUES (@kunci, @nilai, @oleh)
                      ON CONFLICT (kunci) DO UPDATE
                      SET nilai = EXCLUDED.nilai, diubah_oleh = EXCLUDED.diubah_oleh,
                          diubah_pada = (NOW() AT TIME ZONE 'UTC')", conn, trx);
                cmd.Parameters.AddWithValue("kunci", kunci);
                cmd.Parameters.AddWithValue("nilai", isi);
                cmd.Parameters.AddWithValue("oleh", diubahOleh);
                await cmd.ExecuteNonQueryAsync();
            }
            await trx.CommitAsync();
        }

        public async Task CatatSinkronAsync(string sumber, bool berhasil, int jumlah, string keterangan)
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO log_sinkronisasi (sumber, berhasil, jumlah, keterangan)
                  VALUES (@sumber, @berhasil, @jumlah, @keterangan)", conn);
            cmd.Parameters.AddWithValue("sumber", sumber);
            cmd.Parameters.AddWithValue("berhasil", berhasil);
            cmd.Parameters.AddWithValue("jumlah", jumlah);
            cmd.Parameters.AddWithValue("keterangan", keterangan.Length > 500 ? keterangan[..500] : keterangan);
            await cmd.ExecuteNonQueryAsync();
        }

        // Catatan terakhir tiap sumber (FIRMS, Cuaca, Email).
        public async Task<Dictionary<string, CatatanSinkron>> AmbilSinkronTerakhirAsync()
        {
            var hasil = new Dictionary<string, CatatanSinkron>();
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"SELECT DISTINCT ON (sumber) sumber, waktu, berhasil, jumlah, keterangan
                  FROM log_sinkronisasi
                  ORDER BY sumber, waktu DESC, id DESC", conn);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                hasil[rd.GetString(0)] = new CatatanSinkron(
                    rd.GetString(0), rd.GetDateTime(1), rd.GetBoolean(2), rd.GetInt32(3), rd.GetString(4));
            }
            return hasil;
        }
    }
}

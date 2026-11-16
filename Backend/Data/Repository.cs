using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace SiagaApi.Backend.Data
{
    // INHERITANCE
    // Kelas dasar untuk semua repository. Alur baca data dari PostgreSQL
    // (buka koneksi -> jalankan query -> baca baris -> petakan ke objek)
    // ditulis sekali di sini dan diwarisi oleh WilayahRepository,
    // TitikPanasRepository, dan LaporanWargaRepository.
    //
    // POLYMORPHISM
    // NamaTabel, QueryAmbilSemua, dan Petakan() abstract: tiap turunan
    // mengisinya sendiri. Kode pemanggil cukup memegang Repository<T>
    // dan memanggil AmbilSemuaAsync(); versi yang jalan ditentukan oleh
    // objek aslinya saat runtime.
    public abstract class Repository<T>
    {
        // ENCAPSULATION: turunan boleh memakai koneksi, kelas luar tidak.
        protected KoneksiDatabase Db { get; }

        protected Repository(KoneksiDatabase db)
        {
            Db = db;
        }

        protected abstract string NamaTabel { get; }

        protected abstract string QueryAmbilSemua { get; }

        // Mengubah satu baris hasil query menjadi objek model.
        protected abstract T Petakan(NpgsqlDataReader rd);

        public virtual Task<List<T>> AmbilSemuaAsync()
        {
            return JalankanQueryAsync(QueryAmbilSemua);
        }

        public async Task<int> HitungAsync()
        {
            await using var conn = await Db.BukaKoneksiAsync();
            // NamaTabel berasal dari konstanta di kelas turunan, bukan input pengguna.
            await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {NamaTabel}", conn);
            return System.Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Semua nilai dari luar WAJIB lewat parameter (@nama), tidak boleh
        // disambung ke string SQL, supaya aman dari SQL injection.
        protected async Task<List<T>> JalankanQueryAsync(string sql, params NpgsqlParameter[] parameter)
        {
            var hasil = new List<T>();
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameter);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                hasil.Add(Petakan(rd));
            }
            return hasil;
        }
    }
}

using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Data
{
    public class HasilSimulasiRepository : Repository<HasilSimulasi>
    {
        public HasilSimulasiRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "hasil_simulasi";

        protected override string QueryAmbilSemua =>
            @"SELECT id, waktu_dijalankan, parameter_angin, parameter_kelembapan, snapshot_grid, wilayah_id
              FROM hasil_simulasi
              ORDER BY waktu_dijalankan DESC";

        protected override HasilSimulasi Petakan(NpgsqlDataReader rd)
        {
            return new HasilSimulasi
            {
                Id = rd.GetInt32(0),
                WaktuDijalankan = rd.GetDateTime(1),
                ParameterAngin = rd.GetDouble(2),
                ParameterKelembapan = rd.GetDouble(3),
                SnapshotGrid = rd.GetString(4),
                WilayahId = rd.GetInt32(5)
            };
        }

        // CREATE. Id dan waktu diisi balik dari database.
        public async Task<HasilSimulasi> SimpanAsync(HasilSimulasi hasil)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO hasil_simulasi (parameter_angin, parameter_kelembapan, snapshot_grid, wilayah_id)
                  VALUES (@angin, @kelembapan, @snapshot, @wilayah)
                  RETURNING id, waktu_dijalankan", conn);
            cmd.Parameters.AddWithValue("angin", hasil.ParameterAngin);
            cmd.Parameters.AddWithValue("kelembapan", hasil.ParameterKelembapan);
            cmd.Parameters.AddWithValue("snapshot", hasil.SnapshotGrid);
            cmd.Parameters.AddWithValue("wilayah", hasil.WilayahId);
            await using var rd = await cmd.ExecuteReaderAsync();
            await rd.ReadAsync();
            hasil.Id = rd.GetInt32(0);
            hasil.WaktuDijalankan = rd.GetDateTime(1);
            return hasil;
        }
    }
}

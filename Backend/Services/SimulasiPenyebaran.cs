using System;
using System.Collections.Generic;
using System.Text.Json;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public class SimulasiPenyebaran
      {
            private readonly GridSimulasi _grid;
            private readonly DataCuaca _cuaca;
            private readonly Wilayah _wilayah;
            private readonly List<TitikPanas> _klasterPemicu;

            public SimulasiPenyebaran(Wilayah wilayah, List<TitikPanas> klasterPemicu, DataCuaca cuaca, GridGenerator gridGenerator)
            {
                  _wilayah = wilayah;
                  _klasterPemicu = klasterPemicu;
                  _cuaca = cuaca;
                  _grid = gridGenerator.BuatGridDariKlaster(klasterPemicu);
            }

            public void Langkah()
            {
                  var statusLama = new StatusSel[_grid.Baris, _grid.Kolom];
                  for (int b = 0; b < _grid.Baris; b++)
                  for (int k = 0; k < _grid.Kolom; k++)
                        statusLama[b, k] = _grid[b, k].Status;

                  var random = new Random();

                  for (int b = 0; b < _grid.Baris; b++)
                  {
                  for (int k = 0; k < _grid.Kolom; k++)
                  {
                        var selSekarang = _grid[b, k];

                        if (statusLama[b, k] == StatusSel.Terbakar)
                        {
                              selSekarang.Status = StatusSel.Habis;
                              continue;
                        }

                        if (statusLama[b, k] != StatusSel.Aman)
                              continue; 

                        foreach (var (db, dk) in Tetangga8())
                        {
                              int nb = b + db, nk = k + dk;
                              if (nb < 0 || nb >= _grid.Baris || nk < 0 || nk >= _grid.Kolom) continue;
                              if (statusLama[nb, nk] != StatusSel.Terbakar) continue;

                              double peluang = PeluangRambat(_grid[nb, nk], selSekarang, db, dk);
                              if (random.NextDouble() < peluang)
                              {
                              selSekarang.Status = StatusSel.Terbakar;
                              break;
                              }
                        }
                  }
                  }
            }

            public GridSimulasi JalankanSampai(int jam)
            {
                  for (int i = 0; i < jam; i++)
                  {
                  Langkah();
                  }
                  return _grid;
            }

            private double PeluangRambat(Sel asal, Sel tujuan, int db, int dk)
            {
                  if (tujuan.Status == StatusSel.Air) return 0.0;
                  if (tujuan.BahanBakar <= 0) return 0.0;

                  double arahRambatRad = Math.Atan2(dk, -db); 
                  double arahAnginRad = _cuaca.ArahAngin * Math.PI / 180.0;
                  double kesamaanArah = Math.Cos(arahRambatRad - arahAnginRad); // -1..1

                  double basePeluang = 0.15; // peluang dasar tanpa pengaruh apapun
                  double pengaruhAngin = (_cuaca.KecepatanAngin / 50.0) * Math.Max(kesamaanArah, 0);
                  double pengaruhKelembapan = -(_cuaca.Kelembapan / 100.0) * 0.3;

                  double total = basePeluang + pengaruhAngin + pengaruhKelembapan;
                  return Math.Clamp(total * tujuan.BahanBakar, 0.0, 1.0);
            }

            private static IEnumerable<(int, int)> Tetangga8()
            {
                  for (int db = -1; db <= 1; db++)
                  for (int dk = -1; dk <= 1; dk++)
                        if (!(db == 0 && dk == 0))
                              yield return (db, dk);
            }

            public HasilSimulasi BuatHasilSimulasi()
            {
                  var statusGrid = new string[_grid.Baris * _grid.Kolom];
                  for (int i = 0; i < _grid.Peta.Count; i++)
                  {
                  statusGrid[i] = _grid.Peta[i].Status.ToString();
                  }

                  return new HasilSimulasi
                  {
                  WaktuDijalankan = DateTime.UtcNow,
                  ParameterAngin = _cuaca.KecepatanAngin,
                  ParameterKelembapan = _cuaca.Kelembapan,
                  SnapshotGrid = JsonSerializer.Serialize(new
                  {
                        _grid.Baris,
                        _grid.Kolom,
                        Status = statusGrid
                  }),
                  WilayahId = _wilayah.Id
                  };
            }
      }
}
using System;
using System.Collections.Generic;
using System.Linq;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public class GridGenerator
      {
            // Ukuran satu sel, kira-kira 550 m di khatulistiwa.
            public const double UkuranSelDerajat = 0.005;

            // Jumlah sel kosong di sekeliling klaster. Default 2 seperti semula;
            // simulasi beberapa jam butuh ruang lebih supaya api tidak "mentok" di tepi grid.
            public int Buffer { get; }

            public GridGenerator(int buffer = 2)
            {
                  Buffer = System.Math.Max(buffer, 0);
            }

            public GridSimulasi BuatGridDariKlaster(List<TitikPanas> titikPanasList)
            {
                  if (titikPanasList == null || titikPanasList.Count == 0)
                  throw new ArgumentException("Klaster titik panas tidak boleh kosong.", nameof(titikPanasList));

                  double latMin = titikPanasList.Min(t => t.Lat);
                  double latMax = titikPanasList.Max(t => t.Lat);
                  double lonMin = titikPanasList.Min(t => t.Lon);
                  double lonMax = titikPanasList.Max(t => t.Lon);

                  int buffer = Buffer;
                  int baris = (int)System.Math.Ceiling((latMax - latMin) / UkuranSelDerajat) + buffer * 2;
                  int kolom = (int)System.Math.Ceiling((lonMax - lonMin) / UkuranSelDerajat) + buffer * 2;

                  var grid = new GridSimulasi(System.Math.Max(baris, 1), System.Math.Max(kolom, 1));

                  foreach (var sel in grid.Peta)
                  {
                  sel.BahanBakar = 1.0; 
                  }

                  foreach (var titik in titikPanasList)
                  {
                  int baridx = (int)((titik.Lat - latMin) / UkuranSelDerajat) + buffer;
                  int kolidx = (int)((titik.Lon - lonMin) / UkuranSelDerajat) + buffer;

                  baridx = System.Math.Clamp(baridx, 0, grid.Baris - 1);
                  kolidx = System.Math.Clamp(kolidx, 0, grid.Kolom - 1);

                  grid[baridx, kolidx].Status = StatusSel.Terbakar;
                  }

                  return grid;
            }
      }
}
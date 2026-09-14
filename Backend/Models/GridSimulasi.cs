using System;
using System.Collections.Generic;

namespace SiagaApi.Backend.Models
{
      public class GridSimulasi
      {
            public int Kolom { get; }
            public int Baris { get; }
            public List<Sel> Peta { get; }

            public GridSimulasi(int baris, int kolom)
            {
                  Baris = baris;
                  Kolom = kolom;
                  Peta = new List<Sel>(baris * kolom);
                  for (int i = 0; i < baris * kolom; i++)
                  {
                  Peta.Add(new Sel());
                  }
            }

            public Sel this[int baris, int kolom]
            {
                  get => Peta[IndexDari(baris, kolom)];
                  set => Peta[IndexDari(baris, kolom)] = value;
            }

            private int IndexDari(int baris, int kolom)
            {
                  if (baris < 0 || baris >= Baris || kolom < 0 || kolom >= Kolom)
                  throw new ArgumentOutOfRangeException($"Indeks ({baris},{kolom}) di luar grid {Baris}x{Kolom}.");

                  return baris * Kolom + kolom;
            }
      }
}
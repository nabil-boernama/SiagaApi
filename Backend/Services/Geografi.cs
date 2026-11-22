using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public enum TingkatConfidence
      {
            Rendah,
            Nominal,
            Tinggi
      }

      // Confidence FIRMS datang dalam dua bentuk: kategori untuk VIIRS
      // (low/nominal/high atau l/n/h) dan angka 0-100 untuk MODIS.
      public static class Confidence
      {
            public static TingkatConfidence Tingkat(string nilai)
            {
                  string v = (nilai ?? "").Trim().ToLowerInvariant();
                  if (v is "h" or "high") return TingkatConfidence.Tinggi;
                  if (v is "n" or "nominal") return TingkatConfidence.Nominal;
                  if (v is "l" or "low") return TingkatConfidence.Rendah;

                  if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double angka))
                        return angka >= 80 ? TingkatConfidence.Tinggi : angka >= 30 ? TingkatConfidence.Nominal : TingkatConfidence.Rendah;

                  return TingkatConfidence.Rendah;
            }

            // Teks untuk ditampilkan: angka apa adanya ("85%"), kategori dalam bahasa Indonesia.
            public static string Label(string nilai)
            {
                  string v = (nilai ?? "").Trim();
                  if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double angka))
                        return $"{angka:0}%";

                  return Tingkat(v) switch
                  {
                        TingkatConfidence.Tinggi => "Tinggi",
                        TingkatConfidence.Nominal => "Nominal",
                        _ => "Rendah"
                  };
            }
      }

      public static class Geo
      {
            public static double JarakKm(double lat1, double lon1, double lat2, double lon2)
            {
                  const double radiusBumiKm = 6371.0;
                  double dLat = (lat2 - lat1) * Math.PI / 180.0;
                  double dLon = (lon2 - lon1) * Math.PI / 180.0;
                  double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                             Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                             Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                  return radiusBumiKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            }

            // Uji titik-di-dalam-poligon (ray casting).
            public static bool DiDalam(IReadOnlyList<(double Lat, double Lon)> poligon, double lat, double lon)
            {
                  bool dalam = false;
                  for (int i = 0, j = poligon.Count - 1; i < poligon.Count; j = i++)
                  {
                        var (latI, lonI) = poligon[i];
                        var (latJ, lonJ) = poligon[j];
                        if ((latI > lat) != (latJ > lat) &&
                            lon < (lonJ - lonI) * (lat - latI) / (latJ - latI) + lonI)
                        {
                              dalam = !dalam;
                        }
                  }
                  return dalam;
            }

            // Nama arah mata angin dari derajat (0 = utara, searah jarum jam).
            public static string NamaArah(double derajat)
            {
                  string[] nama = { "Utara", "Timur laut", "Timur", "Tenggara", "Selatan", "Barat daya", "Barat", "Barat laut" };
                  int i = (int)Math.Round(((derajat % 360) + 360) % 360 / 45.0) % 8;
                  return nama[i];
            }
      }

      // Menentukan sebuah koordinat masuk kabupaten/kota mana.
      //  - Kabupaten yang punya garis batas: titik harus berada DI DALAM batasnya.
      //  - Kabupaten tanpa garis batas: dipakai kabupaten terdekat, maksimal 40 km
      //    dari titik tengahnya.
      // Titik di luar semua kabupaten yang dikenal (laut, provinsi/negara lain)
      // mengembalikan null dan tidak disimpan.
      public class PenentuWilayah
      {
            private const double JarakMaksTanpaBatasKm = 40;

            private readonly List<(Wilayah Wilayah, List<List<(double Lat, double Lon)>>? Batas, double LatMin, double LatMax, double LonMin, double LonMax)> _kabupaten = new();

            public PenentuWilayah(
                  IEnumerable<Wilayah> kabupaten,
                  IReadOnlyDictionary<int, List<List<(double Lat, double Lon)>>> batas)
            {
                  foreach (var w in kabupaten.Where(w => w.Tingkat == TingkatWilayah.Kabupaten))
                  {
                        if (batas.TryGetValue(w.Id, out var poligon) && poligon.Count > 0)
                        {
                              var semua = poligon.SelectMany(p => p).ToList();
                              _kabupaten.Add((w, poligon, semua.Min(t => t.Lat), semua.Max(t => t.Lat), semua.Min(t => t.Lon), semua.Max(t => t.Lon)));
                        }
                        else
                        {
                              _kabupaten.Add((w, null, 0, 0, 0, 0));
                        }
                  }
            }

            public Wilayah? Tentukan(double lat, double lon)
            {
                  foreach (var k in _kabupaten)
                  {
                        if (k.Batas is null || lat < k.LatMin || lat > k.LatMax || lon < k.LonMin || lon > k.LonMax)
                              continue;
                        if (k.Batas.Any(p => Geo.DiDalam(p, lat, lon)))
                              return k.Wilayah;
                  }

                  return _kabupaten
                        .Where(k => k.Batas is null)
                        .Select(k => (k.Wilayah, Jarak: Geo.JarakKm(lat, lon, k.Wilayah.Lat, k.Wilayah.Lon)))
                        .Where(x => x.Jarak <= JarakMaksTanpaBatasKm)
                        .OrderBy(x => x.Jarak)
                        .Select(x => x.Wilayah)
                        .FirstOrDefault();
            }
      }
}

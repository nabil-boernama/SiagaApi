using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SiagaApi.Backend.Services
{
      // Peta dasar dari tile web (proyeksi Web Mercator, ubin 256 piksel).
      //  - Peta jalan : OpenStreetMap, (c) OpenStreetMap contributors
      //  - Satelit    : Esri World Imagery
      // Ubin yang sudah diunduh disimpan di komputer, jadi tidak diunduh ulang
      // dan tetap tampil saat internet terputus.
      public class PetaDasarService
      {
            public const int UkuranUbin = 256;
            public const int ZoomMin = 5;
            public const int ZoomMaks = 13;

            private readonly HttpClient _http;
            private readonly string _folder;

            public PetaDasarService(HttpClient? http = null, string? folder = null)
            {
                  _http = http ?? KlienHttp.Bersama;
                  _folder = folder ?? Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SiagaApi", "ubin");
            }

            public static string Atribusi(string jenis)
            {
                  return jenis == PengaturanPosko.PetaSatelit
                        ? "Citra: Esri, Maxar, Earthstar Geographics"
                        : "© OpenStreetMap contributors";
            }

            // Posisi piksel dunia (Web Mercator) untuk satu koordinat pada zoom tertentu.
            public static (double X, double Y) KePiksel(double lat, double lon, double zoom)
            {
                  double skala = UkuranUbin * Math.Pow(2, zoom);
                  double x = (lon + 180.0) / 360.0 * skala;
                  double sinLat = Math.Sin(Math.Clamp(lat, -85.05, 85.05) * Math.PI / 180.0);
                  double y = (0.5 - Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI)) * skala;
                  return (x, y);
            }

            // Kebalikan KePiksel: koordinat dari posisi piksel dunia.
            public static (double Lat, double Lon) DariPiksel(double x, double y, double zoom)
            {
                  double skala = UkuranUbin * Math.Pow(2, zoom);
                  double lon = x / skala * 360.0 - 180.0;
                  double n = Math.PI - 2.0 * Math.PI * y / skala;
                  double lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
                  return (lat, lon);
            }

            // Zoom (boleh pecahan) yang membuat kotak wilayah pas memenuhi kanvas.
            public static double ZoomPas(double latMin, double latMaks, double lonMin, double lonMaks, double lebar, double tinggi)
            {
                  var (x1, y1) = KePiksel(latMaks, lonMin, 0);
                  var (x2, y2) = KePiksel(latMin, lonMaks, 0);
                  double skala = Math.Min(lebar / Math.Max(x2 - x1, 1e-9), tinggi / Math.Max(y2 - y1, 1e-9));
                  return Math.Clamp(Math.Log2(skala), 0, ZoomMaks);
            }

            // Zoom terbesar yang masih memuat kotak wilayah di dalam kanvas.
            public static int ZoomUntuk(double latMin, double latMaks, double lonMin, double lonMaks, double lebar, double tinggi)
            {
                  for (int z = ZoomMaks; z > ZoomMin; z--)
                  {
                        var (x1, y1) = KePiksel(latMaks, lonMin, z);
                        var (x2, y2) = KePiksel(latMin, lonMaks, z);
                        if (x2 - x1 <= lebar && y2 - y1 <= tinggi)
                              return z;
                  }
                  return ZoomMin;
            }

            // Isi satu ubin (PNG/JPEG), atau null kalau tidak bisa diambil.
            public async Task<byte[]?> AmbilUbinAsync(string jenis, int zoom, int x, int y)
            {
                  int jumlah = 1 << zoom;
                  if (y < 0 || y >= jumlah)
                        return null;
                  x = ((x % jumlah) + jumlah) % jumlah;

                  bool satelit = jenis == PengaturanPosko.PetaSatelit;
                  string berkas = Path.Combine(_folder, satelit ? "satelit" : "jalan", zoom.ToString(), x.ToString(), y + (satelit ? ".jpg" : ".png"));

                  try
                  {
                        if (File.Exists(berkas))
                              return await File.ReadAllBytesAsync(berkas);

                        string url = satelit
                              ? $"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{zoom}/{y}/{x}"
                              : $"https://tile.openstreetmap.org/{zoom}/{x}/{y}.png";

                        byte[] isi = await _http.GetByteArrayAsync(url);
                        Directory.CreateDirectory(Path.GetDirectoryName(berkas)!);
                        await File.WriteAllBytesAsync(berkas, isi);
                        return isi;
                  }
                  catch (HttpRequestException)
                  {
                        return null;
                  }
                  catch (TaskCanceledException)
                  {
                        return null;
                  }
                  catch (IOException)
                  {
                        return null;
                  }
            }
      }
}

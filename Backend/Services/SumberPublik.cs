using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      // Satu HttpClient untuk semua pengambilan data dari luar.
      public static class KlienHttp
      {
            public static readonly HttpClient Bersama = Buat();

            private static HttpClient Buat()
            {
                  var klien = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
                  // Penyedia tile dan data publik meminta User-Agent yang jelas.
                  klien.DefaultRequestHeaders.UserAgent.ParseAdd("DesktopKarhutlaMonitor/1.0");
                  return klien;
            }
      }

      public enum RentangFirms
      {
            Jam24,
            Hari7
      }

      // INHERITANCE + POLYMORPHISM: sumber hotspot kedua di samping FirmsApiService.
      // Membaca file CSV publik NASA FIRMS (VIIRS NOAA-20, Asia Tenggara) yang
      // bisa diunduh TANPA MAP_KEY, jadi aplikasi tetap punya data hotspot asli
      // walau kunci API belum diisi di Pengaturan.
      public class FirmsPublikService : SumberData<List<TitikPanas>>
      {
            private readonly HttpClient _httpClient;
            private readonly RentangFirms _rentang;

            public FirmsPublikService(HttpClient httpClient, RentangFirms rentang = RentangFirms.Jam24)
                  : base("https://firms.modaps.eosdis.nasa.gov/data/active_fire/noaa-20-viirs-c2/csv", string.Empty)
            {
                  _httpClient = httpClient;
                  _rentang = rentang;
            }

            // Semua deteksi di Asia Tenggara pada rentang yang dipilih. WilayahId
            // masih 0; kabupatennya ditentukan kemudian oleh PenentuWilayah.
            public async Task<List<TitikPanas>> AmbilSemua()
            {
                  string berkas = _rentang == RentangFirms.Hari7
                        ? "J1_VIIRS_C2_SouthEast_Asia_7d.csv"
                        : "J1_VIIRS_C2_SouthEast_Asia_24h.csv";
                  try
                  {
                        string csv = await _httpClient.GetStringAsync($"{BaseUrl}/{berkas}");
                        return FirmsApiService.ParseCsv(csv, 0);
                  }
                  catch (HttpRequestException ex)
                  {
                        throw new InvalidOperationException("Gagal mengunduh data hotspot publik FIRMS.", ex);
                  }
            }

            // Kontrak SumberData: deteksi dalam kotak 3 derajat di sekitar wilayah,
            // sama seperti FirmsApiService.
            public override async Task<List<TitikPanas>> AmbilData(Wilayah w)
            {
                  var semua = await AmbilSemua();
                  var hasil = semua
                        .Where(t => Math.Abs(t.Lat - w.Lat) <= 3 && Math.Abs(t.Lon - w.Lon) <= 3)
                        .ToList();
                  foreach (var t in hasil)
                  {
                        t.WilayahId = w.Id;
                  }
                  return hasil;
            }
      }

      // INHERITANCE + POLYMORPHISM: sumber cuaca kedua di samping CuacaApiService
      // (AccuWeather). Open-Meteo tidak membutuhkan kunci API.
      public class OpenMeteoService : SumberData<DataCuaca>
      {
            public const string NamaSumber = "Open-Meteo";

            private readonly HttpClient _httpClient;

            public OpenMeteoService(HttpClient httpClient)
                  : base("https://api.open-meteo.com/v1/forecast", string.Empty)
            {
                  _httpClient = httpClient;
            }

            public override async Task<DataCuaca> AmbilData(Wilayah w)
            {
                  return (await AmbilBanyak(new[] { w })).Single();
            }

            // Cuaca terkini untuk beberapa wilayah dalam satu permintaan.
            public async Task<List<DataCuaca>> AmbilBanyak(IReadOnlyList<Wilayah> wilayah)
            {
                  if (wilayah.Count == 0)
                        return new List<DataCuaca>();

                  string lat = string.Join(",", wilayah.Select(w => w.Lat.ToString("0.####", CultureInfo.InvariantCulture)));
                  string lon = string.Join(",", wilayah.Select(w => w.Lon.ToString("0.####", CultureInfo.InvariantCulture)));
                  string url = $"{BaseUrl}?latitude={lat}&longitude={lon}" +
                               "&current=temperature_2m,relative_humidity_2m,wind_speed_10m,wind_direction_10m" +
                               "&wind_speed_unit=kmh&timezone=UTC";

                  string json;
                  try
                  {
                        json = await _httpClient.GetStringAsync(url);
                  }
                  catch (HttpRequestException ex)
                  {
                        throw new InvalidOperationException("Gagal mengambil data cuaca Open-Meteo.", ex);
                  }

                  using var doc = JsonDocument.Parse(json);
                  // Satu lokasi -> objek; beberapa lokasi -> array objek, urutannya sama dengan permintaan.
                  var lokasi = doc.RootElement.ValueKind == JsonValueKind.Array
                        ? doc.RootElement.EnumerateArray().ToList()
                        : new List<JsonElement> { doc.RootElement };

                  if (lokasi.Count != wilayah.Count)
                        throw new InvalidOperationException("Jumlah lokasi pada jawaban Open-Meteo tidak sesuai permintaan.");

                  var hasil = new List<DataCuaca>();
                  for (int i = 0; i < wilayah.Count; i++)
                  {
                        var c = lokasi[i].GetProperty("current");
                        hasil.Add(new DataCuaca
                        {
                              Suhu = c.GetProperty("temperature_2m").GetDouble(),
                              Kelembapan = c.GetProperty("relative_humidity_2m").GetDouble(),
                              KecepatanAngin = c.GetProperty("wind_speed_10m").GetDouble(),
                              ArahAngin = c.GetProperty("wind_direction_10m").GetDouble(),
                              Waktu = DateTime.Parse(c.GetProperty("time").GetString()!, CultureInfo.InvariantCulture),
                              Sumber = NamaSumber,
                              WilayahId = wilayah[i].Id
                        });
                  }
                  return hasil;
            }
      }
}

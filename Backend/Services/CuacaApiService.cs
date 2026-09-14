using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public class CuacaApiService : SumberData<DataCuaca>
      {
            private readonly HttpClient _httpClient;

            public CuacaApiService(HttpClient httpClient, string apiKey)
                  : base("http://dataservice.accuweather.com", apiKey)
            {
                  _httpClient = httpClient;
            }

            public override async Task<DataCuaca> AmbilData(Wilayah w)
            {
                  string locationKey = await ResolveLocationKey(w);

                  string url = $"{BaseUrl}/currentconditions/v1/{locationKey}?apikey={ApiKey}&details=true";
                  string json;
                  try
                  {
                  json = await _httpClient.GetStringAsync(url);
                  }
                  catch (HttpRequestException ex)
                  {
                  throw new InvalidOperationException($"Gagal mengambil data cuaca untuk wilayah {w.Nama}.", ex);
                  }

                  return ParseCurrentConditions(json, w.Id);
            }

            private async Task<string> ResolveLocationKey(Wilayah w)
            {
                  string url = $"{BaseUrl}/locations/v1/cities/geoposition/search" +
                              $"?apikey={ApiKey}&q={w.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                              $"{w.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                  string json = await _httpClient.GetStringAsync(url);
                  using var doc = JsonDocument.Parse(json);
                  return doc.RootElement.GetProperty("Key").GetString()
                  ?? throw new InvalidOperationException("Response AccuWeather tidak punya field Key.");
            }

            private static DataCuaca ParseCurrentConditions(string json, int wilayahId)
            {
                  using var doc = JsonDocument.Parse(json);
                  var elemen = doc.RootElement[0];

                  double suhu = elemen.GetProperty("Temperature").GetProperty("Metric").GetProperty("Value").GetDouble();
                  double kelembapan = elemen.GetProperty("RelativeHumidity").GetDouble();
                  double kecepatanAngin = elemen.GetProperty("Wind").GetProperty("Speed").GetProperty("Metric").GetProperty("Value").GetDouble();
                  double arahAngin = elemen.GetProperty("Wind").GetProperty("Direction").GetProperty("Degrees").GetDouble();
                  DateTime waktu = elemen.GetProperty("LocalObservationDateTime").GetDateTime();

                  return new DataCuaca
                  {
                  Suhu = suhu,
                  Kelembapan = kelembapan,
                  KecepatanAngin = kecepatanAngin,
                  ArahAngin = arahAngin,
                  Waktu = waktu,
                  WilayahId = wilayahId
                  };
            }
      }
}
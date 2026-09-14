using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public class FirmsApiService : SumberData<List<TitikPanas>>
      {
            private readonly HttpClient _httpClient;
            private const string DefaultSource = "VIIRS_NOAA20_NRT";
            private const int DefaultDayRange = 1;

            public FirmsApiService(HttpClient httpClient, string mapKey)
                  : base("https://firms.modaps.eosdis.nasa.gov/api/area/csv", mapKey)
            {
                  _httpClient = httpClient;
            }

            public override async Task<List<TitikPanas>> AmbilData(Wilayah w)
            {
                  string bbox = $"{w.Lon - 3},{w.Lat - 3},{w.Lon + 3},{w.Lat + 3}";
                  string url = $"{BaseUrl}/{ApiKey}/{DefaultSource}/{bbox}/{DefaultDayRange}";
                  string csv;
                  try
                  {
                  csv = await _httpClient.GetStringAsync(url);
                  }
                  catch (HttpRequestException ex)
                  {
                  throw new InvalidOperationException($"Gagal mengambil data FIRMS untuk wilayah {w.Nama}.", ex);
                  }

                  return ParseCsv(csv, w.Id);
            }

            private static List<TitikPanas> ParseCsv(string csv, int wilayahId)
            {
                  var baris = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                  if (baris.Length <= 1) return new List<TitikPanas>();

                  var header = baris[0].Split(',');
                  var idx = header
                  .Select((nama, i) => (nama, i))
                  .ToDictionary(x => x.nama.Trim(), x => x.i, StringComparer.OrdinalIgnoreCase);

                  var hasil = new List<TitikPanas>();

                  for (int i = 1; i < baris.Length; i++)
                  {
                  var kolom = baris[i].Split(',');
                  if (kolom.Length < header.Length) continue;

                  double lat = double.Parse(kolom[idx["latitude"]], CultureInfo.InvariantCulture);
                  double lon = double.Parse(kolom[idx["longitude"]], CultureInfo.InvariantCulture);

                  var tanggal = DateOnly.Parse(kolom[idx["acq_date"]], CultureInfo.InvariantCulture);
                  var jam = TimeOnly.MinValue;
                  if (idx.TryGetValue("acq_time", out int idxJam) && int.TryParse(kolom[idxJam], out int hhmm))
                  {
                        jam = new TimeOnly(hhmm / 100, hhmm % 100);
                  }

                  string confidence = kolom[idx["confidence"]].Trim();

                  double brightness = 0;
                  if (idx.TryGetValue("bright_ti4", out int idxTi4))
                        double.TryParse(kolom[idxTi4], NumberStyles.Any, CultureInfo.InvariantCulture, out brightness);
                  else if (idx.TryGetValue("brightness", out int idxBright))
                        double.TryParse(kolom[idxBright], NumberStyles.Any, CultureInfo.InvariantCulture, out brightness);

                  string satelit = idx.TryGetValue("satellite", out int idxSat) ? kolom[idxSat].Trim() : string.Empty;

                  hasil.Add(new TitikPanas
                  {
                        Lat = lat,
                        Lon = lon,
                        Confidence = confidence,
                        Brightness = brightness,
                        Satelit = satelit,
                        WaktuDeteksi = tanggal.ToDateTime(jam),
                        WilayahId = wilayahId
                  });
                  }

                  return hasil;
            }
      }
}
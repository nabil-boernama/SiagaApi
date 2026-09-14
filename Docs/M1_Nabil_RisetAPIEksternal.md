# M1 — Riset Struktur API Eksternal (FIRMS & AccuWeather)

> Catatan backend SiagaApi. Fokus: field apa saja yang tersedia, terutama field confidence,
> dan bagaimana itu memengaruhi desain kelas backend.

---

## 1. NASA FIRMS — Area API

### Endpoint

```
https://firms.modaps.eosdis.nasa.gov/api/area/csv/{MAP_KEY}/{SOURCE}/{AREA_COORDINATES}/{DAY_RANGE}
https://firms.modaps.eosdis.nasa.gov/api/area/csv/{MAP_KEY}/{SOURCE}/{AREA_COORDINATES}/{DAY_RANGE}/{DATE}
```

- **MAP_KEY** — key gratis, daftar di halaman FIRMS (limit 5000 transaksi / 10 menit; query besar dihitung sebagai beberapa transaksi).
- **SOURCE** — salah satu dari: `MODIS_NRT`, `MODIS_SP`, `VIIRS_SNPP_NRT`, `VIIRS_SNPP_SP`, `VIIRS_NOAA20_NRT`, `VIIRS_NOAA20_SP`, `VIIRS_NOAA21_NRT`, `LANDSAT_NRT` (US/Canada saja).
- **AREA_COORDINATES** — `west,south,east,north` (bounding box), atau `world`. **Bukan** format north/south/east/west terpisah.
- **DAY_RANGE** — 1..5 hari.
- **DATE** (opsional) — `YYYY-MM-DD`, kalau kosong = data terbaru.

Response berupa **CSV**, bukan JSON — perlu parsing manual atau library CSV (mis. CsvHelper).

### Field CSV — VIIRS (SNPP / NOAA-20 / NOAA-21)

| Kolom | Tipe | Keterangan |
|---|---|---|
| `latitude`, `longitude` | double | pusat pixel 375m |
| `bright_ti4` | double | brightness temperature channel I4 (Kelvin) |
| `bright_ti5` | double | brightness temperature channel I5 (Kelvin) |
| `scan`, `track` | double | ukuran pixel aktual |
| `acq_date` | date | tanggal akuisisi (UTC) |
| `acq_time` | int | jam akuisisi (UTC), format HHMM |
| `satellite` | string | mis. `N`, `1` (NOAA-20), `2` (NOAA-21) |
| `instrument` | string | `VIIRS` |
| **`confidence`** | **string kategorikal** | **`low` / `nominal` / `high`** — bukan angka |
| `version` | string | versi koleksi & sumber (NRT/RT/URT) |
| `frp` | double | Fire Radiative Power (MW) |
| `daynight` | string | `D` / `N` |

### Field CSV — MODIS

| Kolom | Tipe | Keterangan |
|---|---|---|
| `latitude`, `longitude` | double | pusat pixel 1km |
| `brightness` | double | brightness temperature channel 21/22 (Kelvin) |
| `scan`, `track` | double | ukuran pixel aktual |
| `acq_date`, `acq_time` | date/int | akuisisi UTC |
| `satellite` | string | `A` (Aqua) / `T` (Terra) |
| **`confidence`** | **int 0–100 (%)** | **beda tipe dari VIIRS** |
| `version` | string | versi koleksi |
| `bright_t31` | double | brightness channel 31 |
| `frp` | double | Fire Radiative Power (MW) |
| `daynight` | string | `D` / `N` |
| `type`* | int | hanya di data SP (0=veg fire, 1=aktif volcano, 2=other static, 3=offshore) |

### Implikasi desain penting

1. **Confidence VIIRS ≠ confidence MODIS secara tipe.** VIIRS = kategori teks, MODIS = persentase.
   → Kolom `confidence` di tabel `titik_panas` harus **string**, disimpan apa adanya dari sumber
   (selaras prinsip #2: confidence berasal dari NASA, backend tidak menghitung ulang).
   Jangan konversi VIIRS "high" jadi angka buatan sendiri — itu bukan tugas backend.
2. CSV FIRMS **tidak mengembalikan nama wilayah/kabupaten** — hanya lat/lon. Pencocokan ke `wilayah`
   (kabupaten mana titik ini berada) harus dilakukan di sisi backend kita sendiri (point-in-polygon
   atau bounding box kabupaten — koordinasi dengan Axel soal skema `wilayah`).
3. VIIRS 375m jauh lebih presisi dari MODIS 1km — untuk klaster & grid simulasi (M6), VIIRS lebih disarankan sebagai sumber utama, MODIS sebagai pelengkap riwayat lebih panjang.
4. Rate limit 5000 transaksi/10 menit per MAP_KEY relevan untuk mode sync manual (M2) dan sync harian (M9) — cukup longgar untuk satu wilayah provinsi, tapi tetap perlu di-log biar tidak habis saat testing.

---

## 2. AccuWeather — Current Conditions API

### Alur wajib dua langkah

1. **Locations API** — resolve koordinat wilayah kita ke `LocationKey` (sekali per wilayah, cache di DB — LocationKey untuk satu titik jarang berubah).
   ```
   GET http://dataservice.accuweather.com/locations/v1/cities/geoposition/search?apikey={KEY}&q={lat},{lon}
   ```
2. **Current Conditions API** — pakai LocationKey hasil langkah 1.
   ```
   GET http://dataservice.accuweather.com/currentconditions/v1/{locationKey}?apikey={KEY}&details=true
   ```
   Tanpa `details=true`, response terpotong (cuma datetime observasi, teks cuaca, ikon, flag hujan, suhu) — data angin dan kelembapan **tidak ikut**. Untuk `KalkulatorRisiko` dan `PeluangRambat` (butuh angin + kelembapan), `details=true` **wajib**.

### Field response (dengan `details=true`) yang relevan untuk kita

| Field | Tipe | Dipakai untuk |
|---|---|---|
| `Temperature.Metric.Value` | double (°C) | konteks risiko |
| `RelativeHumidity` | int (%) | `KalkulatorRisiko`, `PeluangRambat` (kelembapan tinggi → rambat lebih lambat) |
| `Wind.Speed.Metric.Value` | double (km/h) | `PeluangRambat` (arah & kecepatan angin dominan mendorong sel terbakar) |
| `Wind.Direction.Degrees` | int (0–360) | arah angin, dipakai untuk bobot arah di `PeluangRambat` |
| `HasPrecipitation` | bool | indikasi hujan turun → bisa menurunkan risiko |
| `PrecipitationSummary.PastHour.Metric.Value` | double (mm) | opsional, histori 24 jam tersedia lewat historical endpoint terpisah |
| `LocalObservationDateTime` | datetime | timestamp data cuaca untuk disimpan di `data_cuaca` |

### Implikasi desain

- Perlu tabel/kolom cache `LocationKey` per `wilayah` (bukan re-resolve tiap request — hemat quota API).
- `AccuWeatherService` butuh dua HTTP call berbeda di baliknya, tapi tetap satu method `AmbilData(Wilayah w)` ke luar — cocok dengan pola `SumberData` (detail resolusi LocationKey disembunyikan di implementasi).
- AccuWeather free tier ada limit call/hari — job sync sebaiknya di-throttle per wilayah kabupaten, bukan dipanggil ulang tiap detik.

---

## Sumber

- https://firms.modaps.eosdis.nasa.gov/api/area/
- https://www.earthdata.nasa.gov/data/tools/firms/active-fire-data-attributes-modis-viirs
- https://www.earthdata.nasa.gov/data/tools/firms/faq
- https://firms.modaps.eosdis.nasa.gov/content/academy/data_api/firms_api_use.html
- https://apidev.accuweather.com/developers/current-conditions/general
- https://apidev.accuweather.com/developers/currentConditionsAPI

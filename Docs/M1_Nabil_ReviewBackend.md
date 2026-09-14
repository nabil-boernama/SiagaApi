# Dokumentasi Backend SiagaApi — untuk Review Tim

**Dikerjakan oleh:** Nabil (Backend)
**Cakupan:** M1 (riset API) + implementasi awal seluruh kelas di class diagram
**Status:** Draft untuk direview Axel & Yuki — beberapa keputusan masih perlu dikonfirmasi tim (lihat bagian "Pertanyaan Terbuka" di bawah)

---

## 1. Apa yang sudah dikerjakan

1. Riset struktur response NASA FIRMS Area API dan (rencana) API cuaca — lihat `docs/M1-riset-api-eksternal.md`.
2. Uji coba nyata MAP_KEY FIRMS — berhasil, dapat 2035 titik hotspot di Kalimantan Tengah pada percobaan 12/09/2026 (bukti lewat console app terpisah).
3. Riset perbandingan AccuWeather vs BMKG sebagai sumber cuaca (lihat bagian 4 — ini yang paling butuh keputusan tim).
4. Implementasi C# untuk **seluruh 22 kelas** di `UML_Class_SiagaApi.drawio`, dibagi jadi `Models/` (data) dan `Services/` (logic).

Semua penamaan kelas, method, dan field di kode **sengaja dipertahankan dalam Bahasa Indonesia**, identik dengan yang ada di diagram yang sudah dikumpulkan — supaya penilai/dosen bisa langsung mencocokkan diagram ↔ kode tanpa terjemahan di kepala.

---

## 2. Struktur file

```
Backend/
├── Models/              (data murni — 12 file)
│   ├── Enums.cs          → TingkatWilayah, StatusSiaga, StatusSel, Keparahan, StatusLaporan
│   ├── Wilayah.cs
│   ├── TitikPanas.cs
│   ├── DataCuaca.cs
│   ├── IndeksRisiko.cs
│   ├── LaporanWarga.cs
│   ├── Sel.cs
│   ├── GridSimulasi.cs
│   ├── HasilSimulasi.cs
│   ├── Subscriber.cs
│   ├── RiwayatNotifikasi.cs
│   └── RingkasanProvinsi.cs
│
└── Services/            (logic — 8 file)
    ├── SumberData.cs             → abstract class dasar (generik, lihat #3.1)
    ├── FirmsApiService.cs        → ambil & parsing hotspot FIRMS
    ├── CuacaApiService.cs        → ambil cuaca (saat ini: AccuWeather)
    ├── KalkulatorRisiko.cs       → hitung skor & status siaga (placeholder, lihat #3.4)
    ├── GridGenerator.cs          → konversi klaster hotspot → grid simulasi (M6)
    ├── SimulasiPenyebaran.cs     → engine cellular automaton (M7)
    ├── NotifikasiService.cs      → cek subscriber & kirim email (M8)
    └── AgregasiNasionalService.cs → ringkasan per provinsi (M4)
```

---

## 3. Keputusan desain yang perlu diketahui tim (bukan cuma "FYI", tapi bagian dari cara kerjanya)

### 3.1 — `SumberData` dibuat generik: `SumberData<THasil>`

Di diagram, `AmbilData(w)` abstrak tidak punya tipe balik eksplisit, tapi `FirmsApiService` mengembalikan `List<TitikPanas>` sedangkan `CuacaApiService` mengembalikan `DataCuaca` tunggal. C# tidak mengizinkan override dengan tipe balik berbeda dari satu method abstrak yang sama — jadi kalau dipaksa non-generik, salah satu dari dua kelas ini pasti tidak bisa compile sesuai diagram.

**Solusi:** `SumberData<THasil>` generik. `FirmsApiService : SumberData<List<TitikPanas>>`, `CuacaApiService : SumberData<DataCuaca>`. Tetap satu abstraksi, tetap polimorfik, tapi tipe balik masing-masing type-safe.

### 3.2 — Confidence tetap `string`, bukan `int`

Sesuai riset M1: VIIRS pakai kode huruf (`l`/`n`/`h`, dikonfirmasi dari data asli hasil testing), MODIS pakai persen 0-100. Menyimpannya sebagai `string` berarti backend tidak "menciptakan" skala angka sendiri untuk sesuatu yang sifatnya kategorikal dari NASA (prinsip #2 di project brief).

### 3.3 — `GridSimulasi.Peta` disimpan sebagai `List<Sel>` rata (bukan `Sel[,]`)

Ikut apa yang digambar di diagram (`List~Sel~`), diakses lewat indexer `this[baris, kolom]` supaya tetap nyaman dipakai tanpa menghitung index manual.

### 3.4 — Formula di `KalkulatorRisiko` dan `SimulasiPenyebaran.PeluangRambat` itu PLACEHOLDER

Ini bukan lupa dikerjakan — memang sengaja ditandai jelas di komentar kode. Formulanya masih tebakan kasar (kelembapan rendah + angin kencang menaikkan skor/peluang), belum divalidasi ke literatur fire weather index atau ke dosen pembimbing. Nilai yang keluar sekarang JANGAN dipakai sebagai klaim akurasi di laporan — statusnya masih "kerangka logic sudah ada, kalibrasi belum".

### 3.5 — `NotifikasiService` dan `AgregasiNasionalService` bergantung ke interface, bukan DbContext langsung

`IRiwayatNotifikasiStore` dan `IAgregatHotspotQuery` sengaja dibuat sebagai interface kosong dulu. Alasannya: implementasi query database itu wilayah Axel (skema final belum ada), jadi backend service ini ditulis supaya **siap dipasangkan** begitu Axel selesai, tanpa aku menebak-nebak struktur tabel final.

---

## 4. Yang masih jadi keputusan terbuka (perlu dibahas bareng, bukan aku putuskan sendiri)

| # | Isu | Kenapa penting | Siapa yang perlu diajak diskusi |
|---|---|---|---|
| 1 | **AccuWeather vs BMKG** untuk sumber cuaca | AccuWeather baru ubah model jadi trial 14 hari lalu berbayar — berisiko untuk proyek 10 minggu. BMKG gratis permanen, tapi datanya prakiraan 3-jaman (bukan observasi real-time), dan wajib mencantumkan atribusi "BMKG" di UI. `CuacaApiService` saat ini ditulis untuk AccuWeather (sesuai method `ResolveLocationKey` di diagram) — kalau tim pilih BMKG, method ini perlu diganti dan diagram perlu diupdate. | Semua — terutama Axel (dampak ke skema) dan Yuki (kewajiban atribusi di UI kalau pilih BMKG) |
| 2 | **`Wilayah` belum punya bounding box** | FIRMS Area API butuh bbox (west/south/east/north), tapi `Wilayah` di diagram cuma punya satu titik `Lat`/`Lon`. Sementara ini `FirmsApiService` pakai jalan pintas kasar (±3 derajat dari titik pusat) — bukan solusi final. | Axel (perubahan skema/migration) |
| 3 | **`LaporanWarga` ditambah `Lat`/`Lon`** (di luar diagram asli) | Tanpa ini, `Verifikasi(titikPanas)` untuk pencocokan radius 5km (M5) tidak mungkin benar-benar menghitung jarak. Sudah ditambahkan di kode supaya method-nya valid, tapi diagram & migration belum menyesuaikan. | Axel |
| 4 | **Cache `LocationKey` AccuWeather** | Kalau tetap pakai AccuWeather, tiap panggil `CuacaApiService.AmbilData()` sekarang resolve ulang LocationKey (boros quota). Perlu kolom cache di `Wilayah` kalau mau dioptimalkan — belum ada di diagram. | Axel |
| 5 | **Ukuran sel di `GridGenerator`** (`0.005` derajat, ~500m) | Angka ini tebakan kasar. Terlalu halus = terlalu banyak sel = simulasi lambat; terlalu kasar = tidak presisi. Belum ada patokan performa dari tim. | Semua (trade-off performa vs presisi) |

---

## 5. Cara mereview / menguji hasil kerja ini

1. **FIRMS** — sudah terbukti jalan (lihat bagian 1.2). Console app uji coba terpisah ada di folder `TestFirmsConsole/` (tidak masuk solution utama, cuma alat bantu verifikasi).
2. **Kode di `Backend/Models` dan `Backend/Services`** — belum pernah dikompilasi dalam satu solution penuh (karena `.csproj` solution utama ada di sisi Axel). Sebelum dianggap "selesai", perlu dicoba build bareng di solution yang sebenarnya untuk pastikan tidak ada konflik nama/namespace dengan kerjaan Axel/Yuki.
3. **`SimulasiPenyebaran` dan `KalkulatorRisiko`** — logic-nya jalan (bisa di-compile & dites unit), tapi angka yang dihasilkan belum divalidasi — jangan dites seolah itu klaim akurasi final.
4. **`NotifikasiService`** — belum bisa dites end-to-end karena `IRiwayatNotifikasiStore` belum ada implementasinya (nunggu Axel). Bisa dites parsial pakai implementasi palsu (in-memory) kalau mau verifikasi logic pengiriman emailnya saja.

---

## 6. Referensi terkait

- `docs/M1-riset-api-eksternal.md` — detail field FIRMS & AccuWeather, termasuk temuan soal tipe confidence.
- `UML_Class_SiagaApi.drawio` — diagram acuan yang diimplementasikan di dokumen ini.

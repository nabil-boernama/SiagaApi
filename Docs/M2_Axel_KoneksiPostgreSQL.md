# Koneksi PostgreSQL dan Skema Database

**Dikerjakan oleh:** Axel Urwawuska Atarubby (24/545465/TK/60670)
**Cakupan:** skema database, lapisan akses data `Backend/Data` (Tugas Kelompok Modul 8: PostgreSQL to C#)
**Status:** siap direview, belum dipasang ke UI

---

## 1. Apa yang sudah dikerjakan

- Skema PostgreSQL `Database/siagaapi.sql` untuk tabel `wilayah`, `titik_panas`, `laporan_warga`, lengkap dengan data awal.
- Kelas koneksi `KoneksiDatabase` dan repository generik `Repository<T>`.
- Tiga repository turunan: `WilayahRepository`, `TitikPanasRepository`, `LaporanWargaRepository`.
- Operasi yang tersedia:
  - **Read**: semua data, titik panas per provinsi, laporan per status, hitung baris.
  - **Create**: `LaporanWargaRepository.SimpanAsync`, `TitikPanasRepository.SimpanBanyakAsync` (untuk hasil `FirmsApiService`).
  - **Update**: `LaporanWargaRepository.UbahStatusAsync` (dipakai setelah `LaporanWarga.Verifikasi()`).

## 2. Struktur file

```
Database/
  siagaapi.sql                 skema + data awal
Backend/Data/
  KoneksiDatabase.cs           connection string (private) + buka koneksi
  Repository.cs                kelas dasar abstract Repository<T>
  WilayahRepository.cs
  TitikPanasRepository.cs
  LaporanWargaRepository.cs
```

## 3. Penerapan konsep OOP (yang diminta worksheet)

| Konsep | Di mana | Penjelasan |
| --- | --- | --- |
| **Encapsulation** | `KoneksiDatabase._connectionString` (private), `Repository<T>.Db` (protected) | Detail koneksi tersembunyi. Kelas luar hanya memanggil `BukaKoneksiAsync()` atau method repository. |
| **Inheritance** | `WilayahRepository`, `TitikPanasRepository`, `LaporanWargaRepository` mewarisi `Repository<T>` | Alur buka koneksi, jalankan query, baca baris (`JalankanQueryAsync`, `HitungAsync`) ditulis sekali di kelas dasar. |
| **Polymorphism** | `Petakan()`, `NamaTabel`, `QueryAmbilSemua` (abstract); `AmbilSemuaAsync()` (virtual, di-override `LaporanWargaRepository`) | `Repository<LaporanWarga> r = new LaporanWargaRepository(db); r.AmbilSemuaAsync()` menjalankan versi turunan, yang tidak menampilkan laporan berstatus `Ditolak`. |

Polanya sama dengan `SumberData<THasil>` milik Nabil (kelas dasar abstract + turunan per sumber).

## 4. Keputusan desain dan alasannya

- **Npgsql langsung, belum EF Core.** Worksheet meminta method buatan sendiri yang memakai Inheritance/Encapsulation/Polymorphism. Dengan EF Core, query dibuat otomatis sehingga konsep itu tidak terlihat. Nama tabel dan kolom sudah mengikuti model, jadi kalau nanti pindah ke EF Core, skemanya tidak perlu diubah.
- **Enum disimpan sebagai teks** (`'Berat'`, `'Diverifikasi'`), bukan angka. Tabel bisa dibaca langsung di psql, dan urutan enum di `Enums.cs` boleh berubah tanpa merusak data lama. Nilai yang boleh masuk dijaga dengan `CHECK`.
- **`titik_panas.confidence` bertipe `VARCHAR`.** FIRMS VIIRS memberi `l`/`n`/`h`, sedangkan MODIS memberi angka. Ini sama dengan `TitikPanas.Confidence` (string).
- **Semua waktu disimpan dalam UTC** (`DEFAULT (NOW() AT TIME ZONE 'UTC')`). Jam deteksi FIRMS memakai UTC, dan Postgres yang dipasang di Windows biasanya memakai zona waktu lokal. Tanpa ini, urutan "terbaru" bisa kacau. Konversi ke WIB dilakukan di UI.
- **Connection string dibaca dari environment variable `SIAGAAPI_DB`**, supaya password asli tidak masuk repo (sesuai `Panduan_KontribusiGit.md` bagian 4). Default-nya `localhost` / `postgres` / `postgres`.
- **Semua input lewat parameter** (`@provinsi`, `@id`), tidak pernah disambung ke string SQL, supaya aman dari SQL injection.

## 5. Pertanyaan terbuka

| No | Isu | Kenapa penting | Siapa yang perlu diajak diskusi |
| --- | --- | --- | --- |
| 1 | Package `Npgsql` (8.0.3) perlu ditambahkan ke `SiagaApi.csproj`. Di `main` belum ada csproj; project Avalonia ada di branch Yuki (`545600`). | Tanpa package ini, `Backend/Data` tidak bisa di-build. Sesuai panduan, perubahan csproj perlu dikabari dulu. | Yuki |
| 2 | Kolom yang belum ada di model: `LaporanWarga` versi form lab punya `kontak`, model tim tidak. | Kalau form butuh nomor kontak pelapor, model dan tabel perlu ditambah. | Nabil, Yuki |
| 3 | Tabel lain dari class diagram (`data_cuaca`, `indeks_risiko`, `subscriber`, `riwayat_notifikasi`, `ringkasan_provinsi`) belum dibuat. | Belum dibutuhkan untuk tugas Modul 8. Tambahkan saat fiturnya dipakai. | Tim |

## 6. Cara menguji

```bash
# 1. buat database dan isi skema (contoh pakai Docker, container "pg")
docker exec pg psql -U postgres -c "CREATE DATABASE siagaapi"
docker exec -i pg psql -U postgres -d siagaapi < Database/siagaapi.sql

# 2. set connection string di terminal (jangan di-commit)
export SIAGAAPI_DB="Host=localhost;Port=5432;Username=postgres;Password=<password>;Database=siagaapi"
```

Contoh pemakaian dari kode:

```csharp
var db = new KoneksiDatabase();
var laporanRepo = new LaporanWargaRepository(db);

await laporanRepo.SimpanAsync(new LaporanWarga
{
    Pelapor = "Rina", Deskripsi = "Asap tebal", Tingkat = Keparahan.Sedang,
    Lat = -0.29, Lon = 102.10, WilayahId = 5
});

List<TitikPanas> riau = await new TitikPanasRepository(db).AmbilPerProvinsiAsync("Riau");
```

Hasil uji lokal (9 Nov 2026, PostgreSQL 16, .NET 10, Npgsql 8.0.3):

- Skema dijalankan dua kali berturut-turut tanpa error, jumlah data tetap (9 wilayah, 7 titik panas, 2 laporan).
- 21 cek integrasi lulus: CRUD, urutan terbaru, enum, input SQL injection tidak merusak tabel, rollback transaksi saat `wilayah_id` tidak valid, override `AmbilSemuaAsync`, dan alur `Verifikasi()` lalu `UbahStatusAsync`.
- Load test: 600 operasi campuran (insert laporan, baca laporan, baca titik panas) dengan 50 berjalan bersamaan, 0 error, sekitar 470–560 ops/detik, semua insert tersimpan. Diuji dengan zona waktu server `UTC` dan `Asia/Jakarta`.

## 7. Referensi

- `Backend/Models/Wilayah.cs`, `TitikPanas.cs`, `LaporanWarga.cs`, `Enums.cs`
- `Backend/Services/SumberData.cs` (pola kelas dasar yang sama)
- `Docs/Panduan_KontribusiGit.md`

# Panduan Dokumentasi Tim — SiagaApi

Dibuat supaya Nabil, Axel, dan Yuki bikin dokumentasi dengan format yang sama —
biar siapapun yang baca dokumentasi siapapun, strukturnya sudah familiar dan
gampang dicari bagian pentingnya (terutama bagian "pertanyaan terbuka").

---

## 1. Prompt template (copy-paste ke Claude)

Ganti bagian dalam `[...]` sesuai kerjaanmu, sisanya biarkan apa adanya:

```
Tolong buatkan dokumentasi review untuk tim berdasarkan kerjaan yang sudah aku lakukan di
[bagian: backend / UI Avalonia / skema database / dst], minggu ke-[M berapa]
dari roadmap.

Formatnya ikuti struktur ini:
1. Header: Dikerjakan oleh, Cakupan, Status
2. Apa yang sudah dikerjakan (poin-poin ringkas, bukan narasi panjang)
3. Struktur file (kalau ada file/kode baru — dalam bentuk pohon folder singkat)
4. Keputusan desain penting yang perlu diketahui tim, beserta ALASANNYA
   (bukan cuma "aku pilih X", tapi kenapa X dan bukan Y)
5. Pertanyaan terbuka / hal yang masih perlu didiskusikan tim — dalam bentuk
   tabel: No | Isu | Kenapa penting | Siapa yang perlu diajak diskusi
6. Cara mereview atau menguji hasil kerja ini
7. Referensi terkait (file/dokumen lain yang relevan)

Konteks kerjaan aku: [jelaskan singkat apa yang dikerjakan, file/kode yang
terlibat, keputusan yang sudah diambil, dan hal yang masih mengganjal/belum
yakin]

Bahasa: Indonesia, dan penamaan kelas/variabel/tabel di dokumentasi HARUS
identik dengan yang dipakai di kode dan diagram — jangan diterjemahkan atau
diparafrase, supaya orang lain bisa langsung cocokkan ke kode aslinya.
```

**Saran tambahan khusus Axel dan Yuki:** kalau kalian mulai sesi baru dengan Claude untuk bagian kalian masing-masing, awali dengan dokumen konteks proyek dulu (mirip yang dipakai Nabil di awal — berisi gambaran aplikasi, prinsip wajib, tech stack, dan scope kerjaan kalian masing-masing) sebelum minta dokumentasi. Ini bikin dokumentasi yang dihasilkan lebih nyambung ke keseluruhan proyek, bukan cuma potongan kerjaan yang berdiri sendiri.

---

## 2. Konvensi penamaan file dokumentasi

Format: **`M[nomor-minggu]_[Nama]_[TopikSingkat].md`**

- `[nomor-minggu]` — sesuai roadmap (M1, M2, ..., M10). Ini bikin file otomatis terurut secara kronologis kalau dilihat di file explorer/GitHub.
- `[Nama]` — Nabil / Axel / Yuki. Supaya jelas siapa penanggung jawabnya tanpa buka isinya dulu.
- `[TopikSingkat]` — PascalCase, singkat, tidak perlu kata sambung. Contoh: `RisetAPI`, `SkemaDatabase`, `SetupAvalonia`, `ReviewBackend`.

Contoh konkret:

- `M1_Nabil_RisetAPI.md`
- `M1_Nabil_ReviewBackend.md`
- `M2_Axel_SkemaDatabase.md`
- `M2_Yuki_SetupAvalonia.md`
- `M4_Axel_AgregasiNasional.md`

**Untuk dokumen yang sifatnya lintas-minggu / proses tim** (bukan tugas mingguan spesifik), pakai prefix `Panduan_` tanpa nomor minggu:

- `Panduan_DokumentasiTim.md` (dokumen ini sendiri)
- `Panduan_KontribusiGit.md` (kalau nanti mau didokumentasikan juga alur git kalian)

Semua taruh rata di satu folder `docs/` di root repo — jangan bikin subfolder per orang, karena itu bikin orang harus tahu dulu "punya siapa" sebelum bisa cari topik yang dia mau baca.

---

## 3. Kapan PERLU membuat dokumentasi

| Situasi                                                                                                                      | Kenapa                                                                                              |
| ---------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| Ada **keputusan desain yang mempengaruhi bagian lain** (skema DB, kontrak antar kelas, format data yang dikirim antar layer) | Orang lain butuh tahu supaya tidak salah asumsi saat mengintegrasikan kerjaannya                    |
| **Menyimpang dari diagram/rencana awal** (nambah field, ganti tipe data, dsb)                                                | Diagram jadi tidak sinkron dengan kode kalau tidak dicatat — bikin bingung pas dinilai              |
| **Ada gap atau keputusan yang masih terbuka**, butuh input tim                                                               | Ini justru fungsi paling penting dokumentasi — bukan cuma "laporan", tapi alat buat minta keputusan |
| **Riset atau integrasi API/library eksternal** yang hasilnya tidak jelas dari nama library-nya saja                          | Orang lain (atau kamu sendiri 2 minggu kemudian) tidak perlu riset ulang dari nol                   |
| **Akhir dari satu milestone (M1, M2, dst)** yang hasilnya dipakai anggota lain di milestone berikutnya                       | Jadi checkpoint yang jelas sebelum lanjut ke tahap berikutnya                                       |
| **Placeholder/asumsi sementara** yang belum divalidasi (formula, angka ambang batas, dsb)                                    | Supaya tidak keliru dianggap final oleh yang lain, atau oleh kamu sendiri nanti                     |

## 4. Kapan TIDAK PERLU membuat dokumentasi terpisah

| Situasi                                                                                             | Kenapa cukup skip                                                                                                         |
| --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| Bug fix kecil yang tidak mengubah kontrak/struktur apapun                                           | Commit message yang jelas sudah cukup                                                                                     |
| Refactor internal yang tidak terlihat dari luar kelas (nama variabel lokal, urutan method, dsb)     | Tidak ada yang perlu tahu di luar kamu sendiri                                                                            |
| Eksperimen/coba-coba yang belum stabil dan bisa saja dibuang                                        | Dokumentasi untuk sesuatu yang belum tentu dipakai itu kerja sia-sia — tunggu sampai stabil dulu                          |
| Sudah cukup jelas dari komentar di kode itu sendiri, dan tidak ada keputusan yang perlu dibahas tim | Jangan duplikasi — komentar kode dan dokumen terpisah yang bilang hal sama itu malah berisiko salah satu jadi basi duluan |
| Perubahan kecil di dalam scope kerjaanmu sendiri yang tidak disentuh siapa-siapa lagi               | Overhead menulis dokumentasi lebih besar dari manfaatnya                                                                  |

**Aturan praktis kalau ragu:** tanya ke diri sendiri — _"Kalau Axel/Yuki buka kode ini tanpa tanya aku dulu, apa mereka bakal salah paham atau kebingungan?"_ Kalau jawabannya "iya, kemungkinan besar", tulis dokumentasinya. Kalau "kayaknya jelas kok dari kodenya", skip saja — jangan dokumentasi demi dokumentasi.

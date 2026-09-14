# Panduan Kontribusi Git — SiagaApi

Ditulis untuk tim yang sebagian besar anggotanya (termasuk Nabil) baru pertama
kali kerja bareng pakai git, bukan solo project. Fokusnya: **cara paling
sederhana yang cukup untuk tim 3 orang, 10 minggu** — bukan alur git yang
rumit ala perusahaan besar.

---

## 1. Struktur branch

Cukup dua lapis, jangan bikin rumit:

- **`main`** — kode yang selalu dalam kondisi bisa di-build dan dijalankan. Tidak ada yang commit langsung ke sini.
- **Satu branch per orang** (`nabil`, `axel`, `yuki`) — kerja harian kalian di sini.

**Yang benar-benar menentukan risiko conflict bukan nama branch-nya, tapi seberapa sering kamu sinkron ke `main`** — lihat aturan di bagian 2.

### Yang perlu diperhatikan karena branch per-nama dipakai lama

Karena branch `nabil`/`axel`/`yuki` itu hidup dari M1 sampai M10 (bukan branch pendek yang dibuang setelah satu tugas), ada dua kebiasaan wajib supaya tidak numpuk jadi masalah besar di akhir:

1. **Jangan tunggu sampai "kerjaan 100% kelar" baru merge ke `main`.** Merge tiap kali ada potongan kerjaan yang stabil dan teruji — anggap tiap milestone mingguan (M1, M2, dst) sebagai titik alami buat merge, bukan ditahan sampai M9/M10.
2. **Merge `main` ke branch kamu sesering mungkin**, idealnya tiap kali mau mulai kerja hari itu (lihat langkah di bagian 2) — supaya conflict yang muncul kecil-kecil dan gampang diselesaikan satu-satu, bukan numpuk jadi conflict raksasa di akhir.

Kalau suatu saat mau coba sesuatu yang eksperimental/berisiko dan takut bikin branch utamamu berantakan kalau gagal, boleh bikin branch kecil sementara dari branch kamu sendiri untuk coba-coba, baru merge balik (atau buang kalau gagal) — tapi ini opsional, tidak perlu dipaksakan tiap tugas kecil.

---

## 2. Alur kerja harian

1. Sebelum mulai kerja hari itu, **update dulu branch kamu dari `main`**:

   ```
   git checkout main
   git pull origin main
   git checkout nabil
   git merge main
   ```

   Ini narik perubahan terbaru orang lain ke branch kamu SEBELUM kamu nambah kerjaan baru — jauh lebih gampang resolve conflict kecil tiap hari daripada conflict besar di akhir minggu.

2. Commit sesering mungkin dengan pesan jelas (lihat bagian 3), **push ke branch kamu sendiri** kapan saja — ini aman, tidak akan mengganggu siapapun karena belum masuk `main`:

   ```
   git add .
   git commit -m "pesan yang jelas"
   git push origin nabil
   ```

3. Kalau satu potongan kerjaan sudah stabil dan teruji (idealnya tiap selesai satu milestone mingguan, bukan ditahan sampai proyek kelar), buat **Pull Request (PR)** di GitHub dari branch kamu ke `main` — jangan `git merge` manual ke `main` dari command line. PR kasih kesempatan 1 orang lain di tim baca sekilas sebelum masuk `main`, dan riwayatnya tercatat rapi.

4. Setelah PR di-merge, **branch kamu (`nabil`) tidak perlu dihapus** — beda dari branch topik sekali pakai, ini memang dipakai terus sampai M10. Lanjut kerja seperti biasa di branch yang sama, cukup ulangi langkah 1 (merge `main` masuk lagi) sebelum lanjut kerjaan berikutnya.

---

## 3. Format pesan commit

Format simpel yang cukup untuk proyek ini:

```
[area]: penjelasan singkat, present tense

Contoh:
backend: tambah FirmsApiService dan parsing CSV
ui: perbaiki layout kartu wilayah di dashboard
db: tambah tabel subscriber dan riwayat_notifikasi
docs: update dokumentasi M1 dengan hasil uji coba
```

`[area]` bisa: `backend`, `ui`, `db`, `docs`, `fix`. Tidak perlu strict, yang penting orang lain bisa nebak isinya tanpa buka diff-nya dulu.

---

## 4. File yang JANGAN pernah di-commit

Ini paling penting buat proyek kalian karena ada API key (FIRMS MAP_KEY, AccuWeather/SMTP credentials):

- `appsettings.Development.json` atau file konfigurasi apapun yang isinya API key/password asli
- Folder `bin/` dan `obj/` (hasil build, bukan source code — ukurannya besar dan beda-beda tiap komputer)
- Folder `.vs/` (setting lokal Visual Studio)

**Setup sekali di awal** — buat file `.gitignore` di root repo (kalau belum ada) isinya minimal:

```
bin/
obj/
.vs/
*.user
appsettings.Development.json
appsettings.Local.json
.env
```

Kalau API key sudah kadung ke-commit sebelumnya: mengubah isinya di file dan commit lagi itu **tidak cukup** — riwayatnya masih ada di git history. Kalau ini kejadian, kabari aku, aku bantu jelaskan cara bersihkannya (beda kasus, jangan asal `git rm` saja).

---

## 5. Titik rawan conflict di proyek kalian spesifik, dan cara menghindarinya

| File/situasi                                                          | Kenapa rawan                                                                                                                                                          | Cara menghindari                                                                                                                                                      |
| --------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `.sln` dan `.csproj`                                                  | Sering ke-edit otomatis oleh Visual Studio/Rider tiap nambah file atau reference baru, formatnya XML linear jadi gampang bentrok walau maksudnya beda                 | Kabari di grup SEBELUM nambah project/reference baru ke solution. Kalau bisa, satu orang saja yang pegang perubahan struktur solution dalam satu waktu                |
| Model/Entity yang dipakai lintas layer (`Wilayah`, `TitikPanas`, dst) | Backend (Nabil) dan UI (Yuki) sama-sama butuh akses ke kelas yang sama                                                                                                | Kalau mau ubah struktur kelas yang sudah dipakai orang lain, kabari dulu — jangan diam-diam ubah nama field yang sudah dipakai di ViewModel Yuki                      |
| Migration EF Core (Axel)                                              | Dua migration yang dibuat bersamaan dari state database yang beda bisa bikin migration history kacau                                                                  | Cuma Axel yang generate migration dulu untuk sementara (sesuai pembagian scope kalian), sampai kalian nyaman dengan alurnya                                           |
| Branch `nabil`/`axel`/`yuki` jarang di-merge ke `main`                | Karena branch kalian dipakai terus dari M1-M10 (bukan sekali pakai), kalau jarang digabung ke `main`, bedanya makin lama makin besar dan conflict-nya numpuk di akhir | Merge ke `main` tiap milestone mingguan selesai, dan merge `main` MASUK ke branch kamu sesering mungkin (idealnya tiap hari mulai kerja) — bukan cuma sekali di akhir |

---

## 6. Kalau conflict tetap terjadi

Tidak usah panik — ini bagian normal kerja tim, bukan tanda ada yang salah.

1. Git akan tandai file yang bentrok dengan:
   ```
   <<<<<<< HEAD
   (versi kode di branch kamu)
   =======
   (versi kode dari branch yang digabung)
   >>>>>>> nama-branch-lain
   ```
2. Buka file itu, baca dua versinya, putuskan mana yang benar (atau gabungkan keduanya kalau memang perlu dua-duanya).
3. Hapus baris `<<<<<<<`, `=======`, `>>>>>>>` setelah selesai memutuskan.
4. `git add` file yang sudah diperbaiki, lalu `git commit` untuk menyelesaikan proses merge.
5. Kalau bingung mana versi yang benar (terutama kalau itu bukan file kamu yang biasa disentuh), **tanya dulu ke pemilik aslinya** sebelum asal pilih salah satu — jangan asumsi versi kamu yang lebih benar.

---

## 7. Alat bantu (opsional, tapi membantu buat pemula)

- **GitHub Desktop** — kalau belum nyaman dengan command line, ini kasih tampilan visual buat lihat perubahan, commit, push/pull, dan bahkan bantu resolve conflict sederhana lewat GUI.
- **Built-in Git di Visual Studio / Rider** — keduanya punya panel Git terintegrasi, seringnya sudah cukup tanpa perlu buka terminal sama sekali.
- Command line tetap berguna dipelajari pelan-pelan, tapi tidak wajib dikuasai penuh dari hari pertama.

---

## 8. Cheat sheet perintah yang paling sering dipakai

```
git status                          # lihat file apa yang berubah
git checkout -b nabil                # bikin branch baru dari branch sekarang (sekali di awal saja)
git add .                           # tandai semua perubahan untuk di-commit
git commit -m "pesan"               # simpan perubahan
git push origin nabil               # kirim branch ke GitHub
git pull origin main                # tarik perubahan terbaru dari main
git merge main                      # gabungkan main ke branch kamu sekarang
git log --oneline -10                # lihat 10 commit terakhir, ringkas
```

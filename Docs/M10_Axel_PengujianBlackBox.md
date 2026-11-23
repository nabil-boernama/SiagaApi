# Pengujian Black Box SiagaApi (Modul 10)

Format mengikuti worksheet Lab 10.2 Modul 10: Halaman yang Diuji, Aksi Aktor, Reaksi Sistem (Benar / Salah), dan Hasil.

| | |
| --- | --- |
| **Nama Aplikasi** | SiagaApi |
| **Versi yang diuji** | `SiagaApi.exe` dari `SiagaApi-win-x64.zip`, branch `545465` |
| **Tanggal Pengujian** | ........................................ |
| **Tester** | ........................................ |
| **Perangkat / OS** | ........................................ |

## Cara mengisi

- Jalankan tiap baris sesuai kolom **Aksi Aktor**.
- **Reaksi Sistem: Benar** adalah reaksi yang diharapkan saat data atau aksinya valid.
- **Reaksi Sistem: Salah** adalah reaksi yang diharapkan saat data tidak valid, atau reaksi yang menandakan fitur gagal.
- Beri tanda pada kolom **Hasil**: `Sesuai` kalau reaksi aplikasi sama dengan yang tertulis, `Tidak Sesuai` kalau berbeda. Kalau tidak sesuai, catat apa yang terjadi di bagian Catatan paling bawah.

## Prasyarat

- Komputer tersambung internet (database di Neon, data hotspot dari NASA FIRMS, cuaca dari Open-Meteo).
- Punya akun Super Admin dan kode undangan posko (diberikan lewat jalur pribadi, tidak ditulis di repo).
- Untuk bagian B sampai H, masuk dulu sebagai Super Admin kecuali disebut lain.

## A. Masuk, Daftar, dan Lupa Kata Sandi

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
| --- | --- | --- | --- | --- | --- |
| A1 | Halaman Masuk | Membuka aplikasi | Halaman Masuk tampil dengan logo, peta, dan formulir | Aplikasi tidak terbuka, atau muncul peringatan "Database tidak terjangkau" | Sesuai ( ) / Tidak Sesuai ( ) |
| A2 | Halaman Masuk | Mengisi nama pengguna dan kata sandi lalu menekan Masuk | Masuk ke halaman Dashboard | Tetap di halaman Masuk dengan pesan "Nama pengguna atau kata sandi salah." dan kolom kata sandi dikosongkan | Sesuai ( ) / Tidak Sesuai ( ) |
| A3 | Halaman Masuk | Salah kata sandi 5 kali berturut-turut | Akun dikunci 5 menit dengan pesan "Terlalu banyak percobaan gagal. Coba lagi dalam 5 menit." | Akun tetap bisa dicoba tanpa batas | Sesuai ( ) / Tidak Sesuai ( ) |
| A4 | Halaman Masuk | Menekan ikon mata di kolom kata sandi | Kata sandi terlihat; ditekan lagi tersembunyi | Kata sandi tidak berubah tampilannya | Sesuai ( ) / Tidak Sesuai ( ) |
| A5 | Halaman Masuk | Mencentang "Ingat saya di komputer ini", masuk, menutup aplikasi, lalu membukanya lagi | Langsung masuk ke Dashboard tanpa mengisi formulir | Diminta masuk lagi | Sesuai ( ) / Tidak Sesuai ( ) |
| A6 | Halaman Lupa Kata Sandi | Menekan "Lupa kata sandi?" | Tampil empat langkah reset lewat admin posko | Halaman tidak terbuka | Sesuai ( ) / Tidak Sesuai ( ) |
| A7 | Halaman Daftar | Mengisi semua data dengan kode undangan yang benar lalu menekan Buat akun | Pesan "Pendaftaran terkirim" dan formulir dikosongkan | Pesan kesalahan bila ada data kosong, kata sandi kurang dari 8 karakter, kata sandi tidak sama, atau kode undangan tidak valid | Sesuai ( ) / Tidak Sesuai ( ) |
| A8 | Halaman Masuk | Masuk dengan akun yang baru didaftarkan dan belum disetujui | (tidak berlaku) | Tetap di halaman Masuk dengan pesan "Akun belum disetujui admin posko." | Sesuai ( ) / Tidak Sesuai ( ) |
| A9 | Semua halaman dalam | Menekan Keluar di rel kiri | Kembali ke halaman Masuk dan "Ingat saya" tidak berlaku lagi | Tetap berada di halaman dalam | Sesuai ( ) / Tidak Sesuai ( ) |

## B. Dashboard

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
| --- | --- | --- | --- | --- | --- |
| B1 | Dashboard | Membuka Dashboard | Empat angka ringkas terisi dan peta kabupaten provinsi posko tampil | Angka tetap "–" atau peta kosong | Sesuai ( ) / Tidak Sesuai ( ) |
| B2 | Dashboard | Mengeklik satu kabupaten di peta | Panel kanan menampilkan nama kabupaten, indeks risiko, cuaca, dan daftar hotspot 24 jam | Panel kanan tidak berubah | Sesuai ( ) / Tidak Sesuai ( ) |
| B3 | Dashboard | Menekan Titik lalu Choropleth | Peta berganti antara titik per hotspot dan warna per kabupaten; legenda ikut berganti | Tampilan peta tidak berubah | Sesuai ( ) / Tidak Sesuai ( ) |
| B4 | Dashboard | Memilih peta dasar Jalan, Satelit, lalu Tanpa | Latar peta berganti sesuai pilihan | Latar tidak berubah atau peta hilang | Sesuai ( ) / Tidak Sesuai ( ) |
| B5 | Dashboard | Menggulir roda mouse atau dua jari di trackpad, lalu menyeret peta | Peta membesar / mengecil dan bergeser mengikuti gerakan | Peta diam atau melompat | Sesuai ( ) / Tidak Sesuai ( ) |
| B6 | Dashboard | Menekan tombol +, −, dan kembali ke awal | Peta membesar, mengecil, lalu kembali ke tampilan awal | Tombol tidak berpengaruh | Sesuai ( ) / Tidak Sesuai ( ) |
| B7 | Dashboard | Menekan Perbarui data | Pesan "Selesai ..." muncul dan jam FIRMS / Cuaca di bilah atas diperbarui | Pesan gagal mengambil data; angka lama tetap ditampilkan | Sesuai ( ) / Tidak Sesuai ( ) |
| B8 | Dashboard | Memilih kabupaten yang punya hotspot lalu menekan "Simulasikan klaster ini" | Pindah ke halaman Simulasi untuk kabupaten itu | Tetap di Dashboard | Sesuai ( ) / Tidak Sesuai ( ) |

## C. Simulasi Penyebaran

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
| --- | --- | --- | --- | --- | --- |
| C1 | Simulasi | Membuka halaman Simulasi | Grid, titik hotspot asli, arah angin, dan cuaca di lokasi tampil | Pesan bahwa tidak ada hotspot 7 hari terakhir untuk disimulasikan | Sesuai ( ) / Tidak Sesuai ( ) |
| C2 | Simulasi | Mengeklik t+0 sampai t+6 di garis waktu | Grid dan perkiraan luas berganti sesuai jam yang dipilih | Tampilan tidak berubah | Sesuai ( ) / Tidak Sesuai ( ) |
| C3 | Simulasi | Menekan tombol putar | Jam maju sendiri dari awal sampai t+6 lalu berhenti | Tidak bergerak atau tidak berhenti | Sesuai ( ) / Tidak Sesuai ( ) |
| C4 | Simulasi | Memilih kabupaten lain di dropdown | Simulasi dijalankan untuk klaster kabupaten itu | Hasil tidak berganti | Sesuai ( ) / Tidak Sesuai ( ) |
| C5 | Simulasi | Menekan "Jalankan ulang simulasi" | Simulasi dihitung ulang dan hasil diperbarui | Tidak ada reaksi | Sesuai ( ) / Tidak Sesuai ( ) |

## D. Laporan Warga

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
| --- | --- | --- | --- | --- | --- |
| D1 | Laporan Warga | Membuka halaman | Tabel laporan tampil, terbaru di atas, dengan jumlah di tiap saringan | Tabel tidak termuat | Sesuai ( ) / Tidak Sesuai ( ) |
| D2 | Laporan Warga | Menekan saringan Menunggu lalu Terverifikasi | Tabel hanya menampilkan laporan dengan status itu | Tabel tidak tersaring | Sesuai ( ) / Tidak Sesuai ( ) |
| D3 | Laporan Warga | Mengetik nama pelapor atau wilayah di kolom cari | Tabel menampilkan laporan yang cocok | Laporan yang dicari tidak ditemukan padahal ada | Sesuai ( ) / Tidak Sesuai ( ) |
| D4 | Laporan Warga | Menekan Laporan baru, mengisi nama, wilayah, keparahan, dan deskripsi, lalu Simpan laporan | Panel tertutup, laporan baru muncul paling atas, pesan "Laporan tersimpan" | Pesan "Nama pelapor wajib diisi." atau pesan sejenis bila ada data kosong; panel tetap terbuka | Sesuai ( ) / Tidak Sesuai ( ) |
| D5 | Laporan Warga | Menyimpan laporan di kabupaten yang punya hotspot dalam radius 5 km | Status laporan langsung Terverifikasi | Status Menunggu padahal ada hotspot di dekatnya | Sesuai ( ) / Tidak Sesuai ( ) |
| D6 | Laporan Warga | Membuka panel Laporan baru lalu menekan Batal | Panel tertutup tanpa menambah laporan | Laporan kosong ikut tersimpan | Sesuai ( ) / Tidak Sesuai ( ) |

## E. Langganan Warga

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
| --- | --- | --- | --- | --- | --- |
| E1 | Langganan | Mengisi email warga, memilih wilayah, lalu menekan Tambah langganan | Baris baru muncul di tabel dengan status Aktif | Pesan "Email warga wajib diisi." "Format email belum valid.", atau "Email tersebut sudah berlangganan di wilayah ini." | Sesuai ( ) / Tidak Sesuai ( ) |
| E2 | Langganan | Menekan Jeda pada satu baris | Status baris menjadi Dijeda dan muncul di saringan Dijeda | Status tidak berubah | Sesuai ( ) / Tidak Sesuai ( ) |
| E3 | Langganan | Mengetik email di kolom cari | Tabel menampilkan langganan yang cocok | Langganan yang dicari tidak ditemukan padahal ada | Sesuai ( ) / Tidak Sesuai ( ) |
| E4 | Langganan | Menekan Hapus pada satu baris | Baris hilang dari tabel | Baris tetap ada | Sesuai ( ) / Tidak Sesuai ( ) |

## F. Riwayat dan Tren

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
| --- | --- | --- | --- | --- | --- |
| F1 | Riwayat & Tren | Membuka halaman | Grafik 14 batang tampil; angka total sama dengan jumlah semua batang | Grafik kosong atau angka tidak cocok | Sesuai ( ) / Tidak Sesuai ( ) |
| F2 | Riwayat & Tren | Menekan 30 hari lalu 14 hari | Grafik berganti menjadi 30 batang lalu kembali 14 batang | Grafik tidak berubah | Sesuai ( ) / Tidak Sesuai ( ) |
| F3 | Riwayat & Tren | Memilih satu kabupaten di dropdown wilayah | Angka dan grafik dihitung ulang untuk kabupaten itu | Angka tidak berubah | Sesuai ( ) / Tidak Sesuai ( ) |
| F4 | Riwayat & Tren | Melihat tabel Lihat nasional | Tiap provinsi tampil dengan jumlah hotspot 24 jam; provinsi posko diberi tanda "posko ini" | Tabel tidak tampil padahal opsinya aktif di Pengaturan | Sesuai ( ) / Tidak Sesuai ( ) |

## G. Pengaturan

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
| --- | --- | --- | --- | --- | --- |
| G1 | Pengaturan, tab Posko | Mengubah peta dasar bawaan lalu menekan Simpan pengaturan | Pesan "Pengaturan tersimpan" dan Dashboard memakai peta dasar itu saat dibuka lagi | Setelan kembali ke nilai lama | Sesuai ( ) / Tidak Sesuai ( ) |
| G2 | Pengaturan, tab Posko | Mematikan "Tampilkan tabel Lihat nasional" lalu menyimpan | Tabel Lihat nasional hilang dari halaman Riwayat | Tabel tetap tampil | Sesuai ( ) / Tidak Sesuai ( ) |
| G3 | Pengaturan, tab Email | Mengisi server SMTP saja lalu menyimpan | (tidak berlaku) | Pesan "Alamat email pengirim tidak valid." dan setelan tidak tersimpan | Sesuai ( ) / Tidak Sesuai ( ) |
| G4 | Pengaturan, tab Akun operator | Menekan "Buat kode baru" | Kode undangan berganti dan kode lama tidak bisa dipakai mendaftar | Kode tidak berganti | Sesuai ( ) / Tidak Sesuai ( ) |
| G5 | Pengaturan, tab Akun operator | Menekan ikon salin di samping kode undangan | Kode tersalin dan bisa ditempel di aplikasi lain | Tidak ada yang tersalin | Sesuai ( ) / Tidak Sesuai ( ) |
| G6 | Pengaturan, tab Akun operator | Menekan Setujui pada akun berstatus Menunggu | Status menjadi Aktif dan akun itu bisa masuk | Status tetap Menunggu | Sesuai ( ) / Tidak Sesuai ( ) |
| G7 | Pengaturan, tab Akun operator | Menekan Tolak pada akun berstatus Menunggu | Status menjadi Ditolak dan akun itu tidak bisa masuk | Akun tetap bisa masuk | Sesuai ( ) / Tidak Sesuai ( ) |
| G8 | Pengaturan, tab Akun operator | Menekan Reset sandi pada satu akun | Kata sandi sementara tampil dan bisa dipakai akun itu untuk masuk | Kata sandi lama masih berlaku | Sesuai ( ) / Tidak Sesuai ( ) |
| G9 | Pengaturan, tab Akun operator | Menekan Jadikan admin lalu Jadikan operator (sebagai Super Admin) | Peran akun berganti sesuai tombol | Peran tidak berubah | Sesuai ( ) / Tidak Sesuai ( ) |
| G10 | Pengaturan, tab Akun operator | Menekan Nonaktifkan lalu Aktifkan pada satu akun | Akun nonaktif tidak bisa masuk; setelah diaktifkan bisa masuk lagi | Akun nonaktif tetap bisa masuk | Sesuai ( ) / Tidak Sesuai ( ) |
| G11 | Pengaturan, tab Keamanan | Mengisi kata sandi lama dan kata sandi baru (dua kali) lalu menekan Ganti kata sandi | Pesan "Kata sandi berhasil diganti" dan kata sandi lama tidak berlaku lagi | Pesan "Kata sandi lama salah." atau pesan kata sandi baru tidak sama / terlalu pendek | Sesuai ( ) / Tidak Sesuai ( ) |

## H. Hak Akses per Peran

| No | Halaman yang Diuji | Aksi Aktor | Reaksi Sistem: Benar | Reaksi Sistem: Salah | Hasil |
| --- | --- | --- | --- | --- | --- |
| H1 | Pengaturan | Masuk sebagai Operator lalu membuka Pengaturan | Tab Akun operator tidak ada; setelan posko terkunci dengan keterangan "Hanya admin posko yang bisa mengubah setelan di tab ini." | Operator bisa mengelola akun atau mengubah setelan posko | Sesuai ( ) / Tidak Sesuai ( ) |
| H2 | Pengaturan, tab Akun operator | Masuk sebagai Admin lalu melihat baris akun Super Admin dan akun sendiri | Tidak ada tombol aksi pada baris itu | Admin bisa mengubah akun Super Admin atau akunnya sendiri | Sesuai ( ) / Tidak Sesuai ( ) |
| H3 | Semua halaman dalam | Mengeklik tiap ikon di rel kiri | Berpindah ke halaman yang sesuai dan ikon aktif berganti | Halaman tidak berpindah | Sesuai ( ) / Tidak Sesuai ( ) |

## Catatan tester

| No uji | Yang terjadi | Tangkapan layar |
| --- | --- | --- |
| | | |
| | | |

## Lampiran: pengujian otomatis yang sudah dijalankan

Selain pengujian manual di atas, aplikasi diuji otomatis pada 22 sampai 23 November 2026 di macOS, terhadap branch database khusus tes (bukan data utama). Hasilnya bukan pengganti kolom Hasil di atas, karena dijalankan oleh skrip, bukan oleh tester di Windows.

| Rangkaian | Jenis | Hasil |
| --- | --- | --- |
| Tampilan: klik tombol sungguhan di semua halaman (headless) | System testing | 61 lulus, 0 gagal |
| Masuk, daftar, peran, sesi | Integration + load | 39 lulus, 0 gagal |
| Dashboard, riwayat, simulasi, langganan, notifikasi | Integration + load | 46 lulus, 0 gagal |
| Laporan warga dan konfigurasi koneksi | Integration + load | 22 lulus, 0 gagal |

Belum diuji: `SiagaApi.exe` di Windows, pengiriman email SMTP sungguhan, dan zoom cubit dua jari di trackpad.

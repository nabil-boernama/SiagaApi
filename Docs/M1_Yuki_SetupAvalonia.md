# Dokumentasi UI SiagaApi — untuk Review Tim

**Dikerjakan oleh:** Yuki  
**Cakupan:** M1 (Fondasi) — inisialisasi project Avalonia MVVM dan pembuatan skeleton awal Dashboard  
**Status:** Draft untuk direview Axel & Nabil

## 1. Apa yang sudah dikerjakan

### Commit `0778133e41eaa62bab9ec9e468198ed38691b802`
`chore: initialize Avalonia MVVM project`

- Menginisialisasi project desktop SiagaApi menggunakan Avalonia.
- Menyiapkan struktur dasar aplikasi dengan pola MVVM.
- Menambahkan `App.axaml` dan `App.axaml.cs` sebagai konfigurasi dan lifecycle aplikasi Avalonia.
- Menambahkan `Program.cs` sebagai entry point aplikasi desktop.
- Menambahkan `SiagaApi.csproj` beserta dependency yang diperlukan untuk Avalonia.
- Menambahkan `ViewLocator.cs` untuk menghubungkan View dengan ViewModel.
- Menambahkan `ViewModels/ViewModelBase.cs` sebagai base class untuk ViewModel.
- Menambahkan `ViewModels/MainViewModel.cs` sebagai ViewModel awal.
- Menambahkan `Views/MainWindow.axaml` dan `Views/MainWindow.axaml.cs` sebagai window utama aplikasi.
- Menambahkan `app.manifest`.
- Menambahkan `.gitignore` untuk mengabaikan file hasil build, file sementara IDE, dan file konfigurasi lokal.

### Commit `b4c3a71a2c898bc2c1be278be935d712d3e0ebc1`
`feat: add dashboard UI skeleton`

- Mengubah `Views/MainWindow.axaml` dari tampilan placeholder menjadi skeleton Dashboard.
- Menambahkan header aplikasi dengan nama `SiagaApi`.
- Menambahkan subtitle `Dashboard pemantauan Karhutla`.
- Menambahkan sidebar navigasi.
- Menambahkan menu navigasi:
  - `Dashboard`
  - `Hotspot`
  - `Simulasi`
  - `Laporan Warga`
  - `Subscriber`
- Menambahkan area konten utama untuk halaman `Dashboard`.
- Menambahkan placeholder informasi bahwa area Dashboard nantinya menampilkan ringkasan hotspot, cuaca, dan laporan lapangan.
- Menyusun layout awal menggunakan `Grid`, `Border`, dan `StackPanel`.
- Menetapkan warna dan spacing awal sebagai dasar visual UI yang akan dikembangkan pada milestone berikutnya.

## 2. Struktur file

Struktur file yang ditambahkan pada inisialisasi project Avalonia MVVM adalah sebagai berikut:

```text
```text
SiagaApi/
├── App.axaml
├── App.axaml.cs
├── Program.cs
├── SiagaApi.csproj
├── ViewLocator.cs
├── app.manifest
├── .gitignore
├── Assets/
│   └── avalonia-logo.ico
├── ViewModels/
│   ├── ViewModelBase.cs
│   └── MainViewModel.cs
└── Views/
    ├── MainWindow.axaml
    └── MainWindow.axaml.cs
```

File yang diperbarui untuk pembuatan skeleton Dashboard:
```text
Views/
└── MainWindow.axaml
```

## 3. Keputusan desain penting yang perlu diketahui tim

### 3.1 — Menggunakan struktur MVVM untuk project UI

**Keputusan:** Project UI menggunakan pemisahan `Views` dan `ViewModels`.

**Alasan:**  
Struktur ini sudah disiapkan sejak inisialisasi project agar tampilan dan ViewModel memiliki tempat masing-masing. `Views/` digunakan untuk file tampilan seperti `MainWindow.axaml`, sedangkan `ViewModels/` digunakan untuk `ViewModelBase.cs` dan `MainViewModel.cs`. `ViewLocator.cs` juga disiapkan untuk membantu menghubungkan View dengan ViewModel.

Dengan struktur ini, pengembangan UI berikutnya dapat dilanjutkan tanpa menempatkan seluruh komponen tampilan dan logic ViewModel dalam satu file.

### 3.2 — `MainWindow.axaml` digunakan sebagai skeleton awal Dashboard

**Keputusan:** Tampilan awal aplikasi dikembangkan dari `Views/MainWindow.axaml`.

**Alasan:**  
Pada commit `0778133`, `MainWindow.axaml` masih merupakan tampilan awal project. Pada commit `b4c3a71`, file yang sama dikembangkan menjadi skeleton Dashboard. Dengan demikian, `MainWindow.axaml` digunakan sebagai titik awal untuk membangun struktur tampilan utama sebelum halaman dan fitur lain dikembangkan.

### 3.3 — Dashboard menggunakan struktur layout header, sidebar, dan area konten

**Keputusan:** Skeleton Dashboard menggunakan pembagian layout menjadi header di bagian atas dan area utama yang terdiri dari sidebar navigasi serta konten Dashboard.

**Alasan:**  
Struktur ini membuat area navigasi dan area konten memiliki posisi yang jelas. Sidebar dapat digunakan untuk berpindah atau mengakses fitur utama aplikasi, sedangkan area konten dapat dikembangkan untuk menampilkan informasi sesuai halaman yang dipilih.

Implementasi awal menggunakan `Grid` untuk membagi area utama, kemudian `Border` dan `StackPanel` untuk menyusun komponen di dalamnya.

### 3.4 — Navigasi awal menggunakan fitur utama SiagaApi

**Keputusan:** Sidebar skeleton menggunakan menu:

- `Dashboard`
- `Hotspot`
- `Simulasi`
- `Laporan Warga`
- `Subscriber`

**Alasan:**  
Menu tersebut digunakan sebagai struktur navigasi awal untuk area fitur utama yang akan dikembangkan dalam aplikasi. Pada tahap ini menu masih berupa elemen tampilan dan belum memiliki mekanisme navigasi antar halaman.

### 3.5 — Menggunakan Fluent Theme dan Inter Font

**Keputusan:** Project menggunakan `Avalonia.Themes.Fluent` dan `Avalonia.Fonts.Inter`.

**Alasan:**  
Keduanya sudah ditambahkan sebagai dependency pada saat inisialisasi project dan digunakan sebagai dasar styling aplikasi. `App.axaml` juga menggunakan `FluentTheme`, sedangkan konfigurasi aplikasi pada `Program.cs` menggunakan `WithInterFont()`.

Penggunaan dependency tersebut ditetapkan sebagai fondasi visual awal sehingga pengembangan UI berikutnya dapat menggunakan dasar styling yang sama.

### 3.6 — Tampilan Dashboard masih berupa skeleton, bukan implementasi fitur final

**Keputusan:** Data pada area Dashboard belum dihubungkan dengan data backend.

**Alasan:**  
Commit `b4c3a71` berfokus pada perubahan tampilan `MainWindow.axaml` dari placeholder menjadi struktur Dashboard. Konten seperti ringkasan hotspot, cuaca, dan laporan lapangan masih ditampilkan sebagai placeholder.

Karena itu, skeleton ini diperlakukan sebagai fondasi UI dan belum sebagai implementasi Dashboard final.

## 4. Pertanyaan terbuka / hal yang masih perlu didiskusikan tim

| No | Isu | Kenapa penting | Siapa yang perlu diajak diskusi |
|---|---|---|---|
| 1 | Struktur navigasi final antar halaman | Sidebar sudah memiliki menu `Dashboard`, `Hotspot`, `Simulasi`, `Laporan Warga`, dan `Subscriber`, tetapi pada tahap ini menu tersebut masih berupa tampilan dan belum memiliki mekanisme navigasi antar halaman. Perlu disepakati struktur View dan mekanisme navigasi yang akan digunakan pada tahap berikutnya. | Yuki, Axel |
| 2 | Kontrak data antara UI dan backend | Dashboard nantinya perlu menampilkan data yang berasal dari backend, seperti hotspot, cuaca, risiko, dan laporan warga. Struktur data yang akan diterima oleh ViewModel perlu disepakati agar integrasi UI dengan backend dapat dilakukan tanpa perubahan besar pada struktur UI. | Yuki, Nabil, Axel |
| 3 | Tampilan dan indikator `StatusSiaga` | Backend memiliki `StatusSiaga` dengan nilai `Hijau`, `Kuning`, dan `Merah`. Bentuk visual dan komponen UI yang digunakan untuk merepresentasikan status tersebut belum ditentukan pada skeleton Dashboard. | Yuki, Nabil |
| 4 | Implementasi visualisasi peta dan simulasi | Skeleton Dashboard belum mencakup peta atau visualisasi simulasi. Perlu didiskusikan bagaimana data `TitikPanas` dan hasil simulasi akan diterjemahkan menjadi visualisasi pada UI. | Yuki, Nabil, Axel |
| 5 | Pembagian View dan ViewModel untuk fitur berikutnya | Saat fitur `Hotspot`, `Simulasi`, `Laporan Warga`, dan `Subscriber` mulai dikembangkan, perlu disepakati pembagian file View dan ViewModel agar struktur MVVM tetap konsisten. | Yuki, Axel |

## 5. Cara mereview atau menguji hasil kerja ini

### 5.1 Build project

Dari root repository, jalankan:

```bash
dotnet restore
dotnet build
```

### 5.2 Menjalankan aplikasi

Jalankan aplikasi dengan:

```bash
dotnet run
```

### Batasan pengujian saat ini

Pengujian pada tahap ini masih terbatas pada validasi struktur project, startup aplikasi, dan tampilan skeleton Dashboard.

Belum dilakukan pengujian integrasi dengan data backend seperti data hotspot, data cuaca, `IndeksRisiko`, maupun `LaporanWarga`, karena integrasi tersebut belum menjadi bagian dari dua commit yang didokumentasikan.

## 6. Referensi terkait

- `Docs/Panduan_DokumentasiTim.md` — panduan struktur dan konvensi dokumentasi review tim.
- `Docs/Panduan_KontribusiGit.md` — panduan workflow Git yang digunakan dalam project.
- `Views/MainWindow.axaml` — implementasi skeleton Dashboard yang dikembangkan pada commit `b4c3a71a2c898bc2c1be278be935d712d3e0ebc1`.
- `ViewModels/MainViewModel.cs` — ViewModel utama yang disiapkan pada tahap inisialisasi project.
- `ViewModels/ViewModelBase.cs` — base class untuk ViewModel.
- `ViewLocator.cs` — komponen yang digunakan untuk menghubungkan View dengan ViewModel.
- `SiagaApi.csproj` — konfigurasi project dan dependency Avalonia.
- Commit `0778133e41eaa62bab9ec9e468198ed38691b802` — inisialisasi project Avalonia MVVM.
- Commit `b4c3a71a2c898bc2c1be278be935d712d3e0ebc1` — pembuatan skeleton Dashboard UI.
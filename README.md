## 👥 Anggota Kelompok & Tanggung Jawab

| Nama | Peran | Tanggung Jawab Utama |
| :--- | :--- | :--- |
| **Axel Urwawuska Atarubby**<br>*(NIM: 24/545465/TK/60670)* | **Software Architect** | Desain arsitektur MVVM, Class Diagram (PBO), skema basis data, pembuatan model grid simulasi, dan manajemen repositori GitHub. |
| **Muhammad Nabil Fitriansyah Boernama**<br>*(NIM: 24/545232/TK/60628)* | **Backend Developer** | Integrasi NASA FIRMS & AccuWeather API, pengelolaan PostgreSQL + EF Core, pembuatan engine simulasi penyebaran api, dan kalkulator indeks risiko. |
| **Yuki Shafa Maheswari**<br>*(NIM: 24/545600/TK/60708)* | **Front-end Developer** | Implementasi antarmuka WPF (dashboard, form input, visualisasi grid simulasi, grafik tren), dan optimasi UX status siaga. |


# 🔥 SiagaApi

[![Category](https://shields.io)](#-kategori)
[![Framework](https://shields.io)](#-spesifikasi-teknis)
[![UI](https://shields.io)](#-spesifikasi-teknis)
[![Database](https://shields.io)](#-spesifikasi-teknis)

**SiagaApi** adalah aplikasi desktop berbasis Windows yang dirancang khusus untuk pemantauan, analisis risiko, dan simulasi mitigasi Kebakaran Hutan dan Lahan (Karhutla) di Indonesia secara terintegrasi dan real-time.

---

## 📌 Kategori
*   **Tema:** *Climate Action*
*   **Fokus:** Pemantauan & Mitigasi Kebakaran Hutan dan Lahan (Karhutla)

---

## ⚠️ Permasalahan yang Dipecahkan

Karhutla merupakan salah satu penyumbang emisi karbon terbesar di Indonesia. Sayangnya, penanganan di lapangan sering terhambat oleh beberapa kendala sistemik:
1.  **Data Terfragmentasi:** Data hotspot satelit, prakiraan cuaca, dan laporan warga tersebar di berbagai platform yang berbeda.
2.  **Kurangnya Prediksi Dinamis:** Platform yang ada saat ini hanya menunjukkan lokasi api yang *sudah terdeteksi*, tanpa memprediksi ke arah mana api akan menyebar berikutnya.
3.  **Laporan Warga Tidak Terverifikasi:** Informasi kebakaran dari masyarakat sering tercecer di media sosial tanpa terhubung ke data satelit untuk proses verifikasi.

---

## 💡 Solusi & Fitur Utama

**SiagaApi** hadir untuk menggabungkan data hotspot satelit, cuaca real-time, dan laporan warga dalam satu dashboard siaga terpadu. Aplikasi ini memprediksi arah penyebaran api menggunakan simulasi *Cellular Automata* yang dipengaruhi oleh arah/kecepatan angin serta kelembapan udara.

### Fitur Unggulan:
*   **📊 Dashboard Hotspot 24 Jam:** Visualisasi sebaran hotspot dalam 24 jam terakhir per provinsi menggunakan data dari **NASA FIRMS**.
*   **🌡️ Indeks Risiko Kebakaran:** Kalkulator skor risiko (0–100) per wilayah berbasis data cuaca, lengkap dengan status siaga (**Hijau** / **Kuning** / **Merah**).
*   **🔄 Simulasi Penyebaran Api:** Prediksi arah dan perluasan sebaran api dari waktu $t+1$ hingga $t+6$ jam ke depan.
*   **📝 Manajemen Laporan Warga:** Fitur CRUD (PostgreSQL) untuk pencatatan, pemetaan, dan verifikasi laporan kebakaran dari masyarakat.
*   **📈 Riwayat & Grafik Tren:** Analisis data statistik dan grafik tren perkembangan hotspot dalam rentang waktu 14 hari terakhir.

---

## 🛠️ Spesifikasi Teknis

Aplikasi ini dibangun menggunakan arsitektur modern untuk memastikan performa yang cepat dan manajemen data yang andal pada lingkungan desktop Windows:
*   **Bahasa & Framework:** C# (.NET 8) & WPF (Windows Presentation Foundation)
*   **Arsitektur UI:** MVVM (Model-View-ViewModel)
*   **Basis Data:** PostgreSQL & Entity Framework Core (EF Core)
*   **Integrasi API Pihak Ketiga:**
    *   **NASA FIRMS API:** Pengambilan data hotspot satelit aktual (VIIRS / MODIS).
    *   **AccuWeather API:** Pengambilan data parameter cuaca real-time.

---

## ⚖️ Analisis Kompetitor & Keunggulan

| Fitur / Komponen | SiPongi+ (KLHK) | Global Forest Watch | BMKG Fire Danger | 🔥 SiagaApi |
| :--- | :---: | :---: | :---: | :---: |
| **Data Hotspot Nasional** | ✅ | ✅ | ❌ | ✅ |
| **Indeks Risiko Cuaca** | ❌ | ❌ | ✅ | ✅ |
| **Laporan Warga** | ❌ | ❌ | ❌ | ✅ |
| **Simulasi Sebaran Api ($t+6$)** | ❌ | ❌ | ❌ | ✅ |
| **Platform & Bahasa** | Web (ID) | Web (EN) | Web (ID) | **Desktop (ID)** |

> ✨ **Pembeda Utama:** SiagaApi merupakan satu-satunya aplikasi desktop berbahasa Indonesia yang menggabungkan seluruh komponen pemantauan, cuaca, laporan masyarakat, dan simulasi prediksi penyebaran api dalam satu dashboard.

---


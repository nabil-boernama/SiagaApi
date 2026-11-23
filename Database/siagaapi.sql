-- =========================================================
-- Skema database SiagaApi (PostgreSQL)
-- Tugas Kelompok Modul 8: PostgreSQL to C#
--
-- Nama tabel dan kolom mengikuti kelas di Backend/Models
-- (Wilayah, TitikPanas, LaporanWarga) supaya repository di
-- Backend/Data bisa memetakan baris ke objek satu-satu.
--
-- Cara menjalankan (contoh pakai Docker, container bernama "pg"):
--   docker exec pg psql -U postgres -c "CREATE DATABASE siagaapi"
--   docker exec -i pg psql -U postgres -d siagaapi < Database/siagaapi.sql
--
-- Urutan lengkap untuk database baru:
--   1. Database/siagaapi.sql        tabel + daftar wilayah
--   2. Database/batas_wilayah.sql   garis batas kabupaten/kota
--   3. Database/akun_awal.sql       akun super admin (file rahasia, tidak ada di repo)
--
-- Script ini aman dijalankan berulang kali (IF NOT EXISTS dan
-- data awal hanya diisi kalau tabel masih kosong).
-- =========================================================

-- Wilayah: provinsi dan kabupaten (hierarki lewat wilayah_induk_id)
CREATE TABLE IF NOT EXISTS wilayah
(
    id               SERIAL PRIMARY KEY,
    nama             VARCHAR(100)     NOT NULL,
    tingkat          VARCHAR(20)      NOT NULL CHECK (tingkat IN ('Provinsi', 'Kabupaten')),
    provinsi         VARCHAR(100)     NOT NULL,
    lat              DOUBLE PRECISION NOT NULL,
    lon              DOUBLE PRECISION NOT NULL,
    wilayah_induk_id INT REFERENCES wilayah (id)
);

-- Titik panas hasil satelit NASA FIRMS (diisi dari FirmsApiService)
CREATE TABLE IF NOT EXISTS titik_panas
(
    id            SERIAL PRIMARY KEY,
    lat           DOUBLE PRECISION NOT NULL,
    lon           DOUBLE PRECISION NOT NULL,
    -- FIRMS VIIRS memberi "l"/"n"/"h", MODIS memberi angka 0-100 -> simpan sebagai teks
    confidence    VARCHAR(10)      NOT NULL,
    brightness    DOUBLE PRECISION NOT NULL DEFAULT 0,
    satelit       VARCHAR(20)      NOT NULL,
    waktu_deteksi TIMESTAMP        NOT NULL, -- UTC
    wilayah_id    INT              NOT NULL REFERENCES wilayah (id)
);

CREATE INDEX IF NOT EXISTS idx_titik_panas_wilayah ON titik_panas (wilayah_id, waktu_deteksi DESC);

-- Laporan warga dari form pelaporan
CREATE TABLE IF NOT EXISTS laporan_warga
(
    id          SERIAL PRIMARY KEY,
    pelapor     VARCHAR(100)     NOT NULL,
    deskripsi   TEXT             NOT NULL,
    tingkat     VARCHAR(20)      NOT NULL CHECK (tingkat IN ('Ringan', 'Sedang', 'Berat')),
    status      VARCHAR(20)      NOT NULL DEFAULT 'Baru'
                                 CHECK (status IN ('Baru', 'Diverifikasi', 'Ditolak')),
    -- semua waktu disimpan dalam UTC (FIRMS juga UTC), konversi ke WIB dilakukan di UI
    dibuat_pada TIMESTAMP        NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    lat         DOUBLE PRECISION NOT NULL,
    lon         DOUBLE PRECISION NOT NULL,
    wilayah_id  INT              NOT NULL REFERENCES wilayah (id)
);

CREATE INDEX IF NOT EXISTS idx_laporan_warga_dibuat ON laporan_warga (dibuat_pada DESC);

-- ---------------------------------------------------------
-- Data awal (hanya kalau tabel masih kosong).
-- Waktu dibuat relatif terhadap sekarang supaya selalu terlihat "baru".
-- ---------------------------------------------------------
INSERT INTO wilayah (nama, tingkat, provinsi, lat, lon)
SELECT * FROM (VALUES
    ('Riau',             'Provinsi', 'Riau',              0.507, 101.447),
    ('Kalimantan Barat', 'Provinsi', 'Kalimantan Barat', -0.026, 109.342),
    ('Sumatera Selatan', 'Provinsi', 'Sumatera Selatan', -2.976, 104.775)
) AS v(nama, tingkat, provinsi, lat, lon)
WHERE NOT EXISTS (SELECT 1 FROM wilayah);

INSERT INTO wilayah (nama, tingkat, provinsi, lat, lon, wilayah_induk_id)
SELECT v.nama, 'Kabupaten', v.provinsi, v.lat, v.lon, p.id
FROM (VALUES
    ('Pekanbaru',          'Riau',              0.507, 101.447),
    ('Pelalawan',          'Riau',             -0.301, 102.118),
    ('Siak',               'Riau',              0.221, 102.455),
    ('Ketapang',           'Kalimantan Barat', -1.854, 110.012),
    ('Kubu Raya',          'Kalimantan Barat',  0.031, 109.334),
    ('Ogan Komering Ilir', 'Sumatera Selatan', -3.402, 105.214)
) AS v(nama, provinsi, lat, lon)
JOIN wilayah p ON p.nama = v.provinsi AND p.tingkat = 'Provinsi'
WHERE NOT EXISTS (SELECT 1 FROM wilayah WHERE tingkat = 'Kabupaten');

-- Tabel titik_panas dan laporan_warga sengaja TIDAK diisi data contoh.
-- Titik panas diambil aplikasi dari NASA FIRMS (otomatis saat operator
-- masuk, atau lewat tombol "Perbarui data" di Dashboard), dan laporan warga
-- diisi operator dari halaman Laporan Warga.

-- =========================================================
-- Akun operator posko (login, daftar, persetujuan admin)
-- =========================================================

-- Akun operator. Kata sandi TIDAK disimpan, hanya hash PBKDF2-nya
-- (lihat Backend/Services/KataSandi.cs).
--
-- Peran:
--   SuperAdmin  mengelola semua akun (termasuk Admin) dan mengubah peran.
--               Dibuat lewat Database/akun_awal.sql (file rahasia, tidak ada
--               di repo), tidak bisa dibuat dari aplikasi.
--   Admin       menyetujui pendaftar, mengelola akun Operator, membuat kode undangan.
--   Operator    memakai aplikasi, tidak melihat panel kelola akun.
--
-- Pendaftaran dari halaman Daftar selalu jadi Operator berstatus Menunggu
-- dan wajib memakai kode undangan yang aktif.
CREATE TABLE IF NOT EXISTS akun_operator
(
    id              SERIAL PRIMARY KEY,
    nama_lengkap    VARCHAR(100) NOT NULL,
    username        VARCHAR(30)  NOT NULL,
    email           VARCHAR(254) NOT NULL,
    password_hash   TEXT         NOT NULL,
    peran           VARCHAR(20)  NOT NULL DEFAULT 'Operator'
                                 CHECK (peran IN ('SuperAdmin', 'Admin', 'Operator')),
    status          VARCHAR(20)  NOT NULL DEFAULT 'Menunggu'
                                 CHECK (status IN ('Menunggu', 'Aktif', 'Ditolak', 'Nonaktif')),
    gagal_login     INT          NOT NULL DEFAULT 0,
    terkunci_sampai TIMESTAMP,   -- UTC; terisi setelah 5 kali salah kata sandi
    login_terakhir  TIMESTAMP,   -- UTC
    dibuat_pada     TIMESTAMP    NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    diproses_oleh   INT REFERENCES akun_operator (id)  -- admin yang menyetujui/menolak
);

-- Username dan email unik tanpa membedakan huruf besar/kecil.
CREATE UNIQUE INDEX IF NOT EXISTS uq_akun_operator_username ON akun_operator (LOWER(username));
CREATE UNIQUE INDEX IF NOT EXISTS uq_akun_operator_email ON akun_operator (LOWER(email));

-- Kode undangan posko. Hanya satu yang aktif pada satu waktu; dibuat
-- Admin dari halaman Pengaturan lalu dibagikan ke calon operator.
CREATE TABLE IF NOT EXISTS kode_undangan
(
    id          SERIAL PRIMARY KEY,
    kode        VARCHAR(20) NOT NULL UNIQUE,
    aktif       BOOLEAN     NOT NULL DEFAULT TRUE,
    dibuat_oleh INT         NOT NULL REFERENCES akun_operator (id),
    dibuat_pada TIMESTAMP   NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC')
);

-- Sesi "Ingat saya di komputer ini". Yang disimpan hash SHA-256 dari
-- token; token aslinya hanya ada di komputer operator.
CREATE TABLE IF NOT EXISTS sesi_login
(
    id               SERIAL PRIMARY KEY,
    operator_id      INT       NOT NULL REFERENCES akun_operator (id) ON DELETE CASCADE,
    token_hash       CHAR(64)  NOT NULL UNIQUE,
    dibuat_pada      TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    kedaluwarsa_pada TIMESTAMP NOT NULL
);

-- =========================================================
-- Data pemantauan: batas wilayah, cuaca, indeks risiko, langganan,
-- notifikasi, simulasi, pengaturan, dan catatan sinkronisasi
-- =========================================================

-- Garis batas kabupaten/kota (diisi oleh Database/batas_wilayah.sql).
ALTER TABLE wilayah ADD COLUMN IF NOT EXISTS batas TEXT;

-- Satu deteksi satelit tidak boleh tersimpan dua kali walau sinkronisasi
-- dijalankan berulang (dipakai ON CONFLICT DO NOTHING di TitikPanasRepository).
CREATE UNIQUE INDEX IF NOT EXISTS uq_titik_panas_deteksi
    ON titik_panas (lat, lon, waktu_deteksi, satelit);
CREATE INDEX IF NOT EXISTS idx_titik_panas_waktu ON titik_panas (waktu_deteksi DESC);

-- Cuaca terkini per kabupaten (model DataCuaca).
CREATE TABLE IF NOT EXISTS data_cuaca
(
    id              SERIAL PRIMARY KEY,
    suhu            DOUBLE PRECISION NOT NULL,  -- derajat Celsius
    kelembapan      DOUBLE PRECISION NOT NULL,  -- persen
    kecepatan_angin DOUBLE PRECISION NOT NULL,  -- km/jam
    arah_angin      DOUBLE PRECISION NOT NULL,  -- derajat, arah DATANGNYA angin (0 = utara)
    waktu           TIMESTAMP        NOT NULL,  -- UTC
    sumber          VARCHAR(30)      NOT NULL,
    wilayah_id      INT              NOT NULL REFERENCES wilayah (id)
);
CREATE INDEX IF NOT EXISTS idx_data_cuaca_wilayah ON data_cuaca (wilayah_id, waktu DESC);

-- Indeks risiko hasil KalkulatorRisiko (model IndeksRisiko).
CREATE TABLE IF NOT EXISTS indeks_risiko
(
    id         SERIAL PRIMARY KEY,
    skor       INT         NOT NULL CHECK (skor BETWEEN 0 AND 100),
    status     VARCHAR(10) NOT NULL CHECK (status IN ('Hijau', 'Kuning', 'Merah')),
    waktu      TIMESTAMP   NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    wilayah_id INT         NOT NULL REFERENCES wilayah (id)
);
CREATE INDEX IF NOT EXISTS idx_indeks_risiko_wilayah ON indeks_risiko (wilayah_id, waktu DESC);

-- Warga yang berlangganan peringatan email (model Subscriber).
CREATE TABLE IF NOT EXISTS subscriber
(
    id             SERIAL PRIMARY KEY,
    email          VARCHAR(254) NOT NULL,
    status         VARCHAR(10)  NOT NULL DEFAULT 'Aktif' CHECK (status IN ('Aktif', 'Dijeda')),
    terdaftar_pada TIMESTAMP    NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    wilayah_id     INT          NOT NULL REFERENCES wilayah (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_subscriber_email_wilayah ON subscriber (LOWER(email), wilayah_id);

-- Catatan peringatan per pelanggan per hotspot (model RiwayatNotifikasi).
-- Pasangan (subscriber, titik panas) unik: satu hotspot hanya dicatat sekali.
CREATE TABLE IF NOT EXISTS riwayat_notifikasi
(
    id             SERIAL PRIMARY KEY,
    waktu_dikirim  TIMESTAMP   NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    hasil          VARCHAR(10) NOT NULL DEFAULT 'Terkirim' CHECK (hasil IN ('Terkirim', 'Dilewati', 'Gagal')),
    keterangan     TEXT        NOT NULL DEFAULT '',
    subscriber_id  INT         NOT NULL REFERENCES subscriber (id) ON DELETE CASCADE,
    titik_panas_id INT         NOT NULL REFERENCES titik_panas (id),
    UNIQUE (subscriber_id, titik_panas_id)
);
CREATE INDEX IF NOT EXISTS idx_riwayat_notifikasi_waktu ON riwayat_notifikasi (waktu_dikirim DESC);

-- Hasil simulasi penyebaran yang dijalankan operator (model HasilSimulasi).
CREATE TABLE IF NOT EXISTS hasil_simulasi
(
    id                  SERIAL PRIMARY KEY,
    waktu_dijalankan    TIMESTAMP        NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    parameter_angin     DOUBLE PRECISION NOT NULL,
    parameter_kelembapan DOUBLE PRECISION NOT NULL,
    snapshot_grid       TEXT             NOT NULL,  -- JSON
    wilayah_id          INT              NOT NULL REFERENCES wilayah (id)
);

-- Pengaturan posko yang dipakai bersama semua operator (provinsi, peta
-- dasar, interval). Kunci API dan kata sandi SMTP TIDAK disimpan di sini,
-- tetapi di komputer masing-masing (lihat PengaturanService).
CREATE TABLE IF NOT EXISTS pengaturan
(
    kunci       VARCHAR(50) PRIMARY KEY,
    nilai       TEXT        NOT NULL,
    diubah_pada TIMESTAMP   NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    diubah_oleh INT REFERENCES akun_operator (id)
);

-- Catatan tiap pengambilan data dari luar (FIRMS, cuaca, email).
CREATE TABLE IF NOT EXISTS log_sinkronisasi
(
    id         SERIAL PRIMARY KEY,
    sumber     VARCHAR(20) NOT NULL CHECK (sumber IN ('FIRMS', 'Cuaca', 'Email')),
    waktu      TIMESTAMP   NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    berhasil   BOOLEAN     NOT NULL,
    jumlah     INT         NOT NULL DEFAULT 0,
    keterangan TEXT        NOT NULL DEFAULT ''
);
CREATE INDEX IF NOT EXISTS idx_log_sinkronisasi ON log_sinkronisasi (sumber, waktu DESC);

SELECT 'wilayah' AS tabel, COUNT(*) AS jumlah FROM wilayah
UNION ALL SELECT 'titik_panas', COUNT(*) FROM titik_panas
UNION ALL SELECT 'laporan_warga', COUNT(*) FROM laporan_warga
UNION ALL SELECT 'akun_operator', COUNT(*) FROM akun_operator;

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

INSERT INTO titik_panas (lat, lon, confidence, brightness, satelit, waktu_deteksi, wilayah_id)
SELECT v.lat, v.lon, v.confidence, v.brightness, v.satelit, (NOW() AT TIME ZONE 'UTC') - v.lalu, w.id
FROM (VALUES
    ( 0.512, 101.447, 'h', 345.2, 'N20',   INTERVAL '1 hour', 'Pekanbaru'),
    ( 0.489, 101.502, 'n', 331.8, 'N20',   INTERVAL '61 minutes', 'Pekanbaru'),
    (-0.301, 102.118, '85', 320.4, 'Terra', INTERVAL '2 hours 27 minutes', 'Pelalawan'),
    ( 0.221, 102.455, '64', 310.0, 'Aqua',  INTERVAL '4 hours 55 minutes', 'Siak'),
    (-1.854, 110.012, 'n', 329.5, 'N20',   INTERVAL '22 minutes', 'Ketapang'),
    ( 0.031, 109.334, 'l', 305.1, 'N20',   INTERVAL '2 hours 40 minutes', 'Kubu Raya'),
    (-3.402, 105.214, '40', 301.7, 'Terra', INTERVAL '1 hour 32 minutes', 'Ogan Komering Ilir')
) AS v(lat, lon, confidence, brightness, satelit, lalu, wilayah)
JOIN wilayah w ON w.nama = v.wilayah
WHERE NOT EXISTS (SELECT 1 FROM titik_panas);

INSERT INTO laporan_warga (pelapor, deskripsi, tingkat, status, dibuat_pada, lat, lon, wilayah_id)
SELECT v.pelapor, v.deskripsi, v.tingkat, v.status, (NOW() AT TIME ZONE 'UTC') - v.lalu, v.lat, v.lon, w.id
FROM (VALUES
    ('Sutrisno', 'Api di kebun sawit',         'Berat',  'Diverifikasi', INTERVAL '44 minutes',  0.510, 101.450, 'Pekanbaru'),
    ('Rina',     'Asap tebal dari arah hutan', 'Sedang', 'Baru',         INTERVAL '1 hour 55 minutes', -0.290, 102.100, 'Pelalawan')
) AS v(pelapor, deskripsi, tingkat, status, lalu, lat, lon, wilayah)
JOIN wilayah w ON w.nama = v.wilayah
WHERE NOT EXISTS (SELECT 1 FROM laporan_warga);

SELECT 'wilayah' AS tabel, COUNT(*) AS jumlah FROM wilayah
UNION ALL SELECT 'titik_panas', COUNT(*) FROM titik_panas
UNION ALL SELECT 'laporan_warga', COUNT(*) FROM laporan_warga;

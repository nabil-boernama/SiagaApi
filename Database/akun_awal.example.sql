-- =========================================================
-- CONTOH akun awal. Salin jadi akun_awal.sql lalu isi hash-nya.
--
-- Repo ini publik, jadi hash kata sandi super admin TIDAK disimpan di
-- sini. File akun_awal.sql yang asli dibagikan lewat paket aplikasi
-- (zip) dan sudah masuk .gitignore.
--
-- Hash dibuat dengan Backend/Services/KataSandi.Hash(...), formatnya:
--   pbkdf2-sha256$600000$<salt base64>$<hash base64>
--
-- Jalankan sekali setelah siagaapi.sql:
--   psql -d siagaapi -f Database/akun_awal.sql
-- =========================================================

INSERT INTO akun_operator (nama_lengkap, username, email, password_hash, peran, status)
SELECT 'Nama Super Admin', 'username', 'email@contoh.id',
       'pbkdf2-sha256$600000$GANTI_SALT$GANTI_HASH',
       'SuperAdmin', 'Aktif'
WHERE NOT EXISTS (SELECT 1 FROM akun_operator WHERE peran = 'SuperAdmin');

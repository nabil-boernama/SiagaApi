namespace SiagaApi.Backend.Models
{
    public enum TingkatWilayah
    {
        Provinsi,
        Kabupaten
    }

    public enum StatusSiaga
    {
        Hijau,
        Kuning,
        Merah
    }

    public enum StatusSel
    {
        Aman,
        Terbakar,
        Habis,
        Air
    }

    public enum Keparahan
    {
        Ringan,
        Sedang,
        Berat
    }

    public enum StatusLaporan
    {
        Baru,
        Diverifikasi,
        Ditolak
    }

    public enum StatusLangganan
    {
        Aktif,
        Dijeda
    }

    public enum HasilNotifikasi
    {
        Terkirim,
        Dilewati,
        Gagal
    }

    public enum PeranOperator
    {
        SuperAdmin,
        Admin,
        Operator
    }

    public enum StatusAkun
    {
        Menunggu,
        Aktif,
        Ditolak,
        Nonaktif
    }
}
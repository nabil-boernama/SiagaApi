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
}
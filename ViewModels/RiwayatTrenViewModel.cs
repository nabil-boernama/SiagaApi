using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SiagaApi.Backend.Services;

namespace SiagaApi.ViewModels;

public class RiwayatTrenViewModel : ViewModelBase, INavigationViewModel
{
    private const double TinggiBatangMaks = 300;

    // Lebar penuh batang di tabel nasional (lebar panel dikurangi tepi).
    public const double LebarBatangNasional = 352;

    // Rentang grafik: 14 atau 30 hari terakhir.
    private int _jumlahHari = 14;

    public bool Rentang14 => _jumlahHari == 14;
    public bool Rentang30 => _jumlahHari == 30;

    public void AturRentang(int hari)
    {
        if (hari == _jumlahHari || (hari != 14 && hari != 30))
            return;

        _jumlahHari = hari;
        OnPropertyChanged(nameof(Rentang14));
        OnPropertyChanged(nameof(Rentang30));
        OnPropertyChanged(nameof(UkuranAngkaBatang));
        _ = MuatSeriAsync();
    }

    // Angka di atas batang diperkecil saat 30 batang berjejer.
    public double UkuranAngkaBatang => _jumlahHari == 30 ? 10 : 12.5;

    private string _labelTotal = "hotspot dalam 14 hari";
    public string LabelTotal { get => _labelTotal; private set => SetProperty(ref _labelTotal, value); }

    private string _rentangTanggal = "";
    public string RentangTanggal { get => _rentangTanggal; private set => SetProperty(ref _rentangTanggal, value); }

    // Hari tanpa hotspot dinyatakan sebagai kabar baik.
    private string _kabarBaik = "";
    public string KabarBaik { get => _kabarBaik; private set => SetProperty(ref _kabarBaik, value); }
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;
    private readonly LayananAplikasi _layanan;

    // Pilihan di dropdown -> id kabupaten (null = seluruh provinsi).
    private readonly Dictionary<string, int?> _wilayahIdPerLabel = new();
    private string _provinsi = "";
    private bool _sedangMemuatDaftar;

    public string ActivePage => "Riwayat & Tren";

    public ObservableCollection<string> WilayahList { get; } = new();
    public ObservableCollection<HotspotHarianViewModel> HotspotHarian { get; } = new();

    // "Mode lihat nasional": satu baris per provinsi. Hanya tampil kalau
    // diizinkan di Pengaturan.
    public ObservableCollection<RingkasanProvinsiViewModel> Nasional { get; } = new();

    private bool _tampilkanNasional;
    public bool TampilkanNasional { get => _tampilkanNasional; private set => SetProperty(ref _tampilkanNasional, value); }

    private string? _selectedWilayah;
    public string? SelectedWilayah
    {
        get => _selectedWilayah;
        set
        {
            if (SetProperty(ref _selectedWilayah, value) && value is not null && !_sedangMemuatDaftar)
                _ = MuatSeriAsync();
        }
    }

    private string _totalHotspot = "–";
    public string TotalHotspot { get => _totalHotspot; private set => SetProperty(ref _totalHotspot, value); }

    private string _puncakHarian = "–";
    public string PuncakHarian { get => _puncakHarian; private set => SetProperty(ref _puncakHarian, value); }

    private string _rataRataHarian = "–";
    public string RataRataHarian { get => _rataRataHarian; private set => SetProperty(ref _rataRataHarian, value); }

    private string _tanggalPuncak = "";
    public string TanggalPuncak { get => _tanggalPuncak; private set => SetProperty(ref _tanggalPuncak, value); }

    private string _perbandinganRataRata = "";
    public string PerbandinganRataRata { get => _perbandinganRataRata; private set => SetProperty(ref _perbandinganRataRata, value); }

    private string _subjudul = "Jumlah hotspot per hari, 14 hari terakhir";
    public string Subjudul { get => _subjudul; private set => SetProperty(ref _subjudul, value); }

    private string _judulGrafik = "HOTSPOT PER HARI";
    public string JudulGrafik { get => _judulGrafik; private set => SetProperty(ref _judulGrafik, value); }

    private string _message = "";
    public string Message { get => _message; private set => SetProperty(ref _message, value); }

    public RiwayatTrenViewModel(Action<string> navigateToPage, Action logout, LayananAplikasi layanan)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;
        _layanan = layanan;

        _ = MuatAsync();
    }

    private async Task MuatAsync()
    {
        try
        {
            var posko = await _layanan.MuatPoskoAsync();
            _provinsi = posko.Provinsi;
            var kabupaten = await _layanan.Dasbor.AmbilKabupatenAsync(_provinsi);

            _sedangMemuatDaftar = true;
            WilayahList.Clear();
            _wilayahIdPerLabel.Clear();

            string semua = $"Seluruh Provinsi {_provinsi}";
            _wilayahIdPerLabel[semua] = null;
            WilayahList.Add(semua);
            foreach (var w in kabupaten)
            {
                _wilayahIdPerLabel[w.Nama] = w.Id;
                WilayahList.Add(w.Nama);
            }
            SelectedWilayah = semua;
            _sedangMemuatDaftar = false;

            await MuatSeriAsync();

            Nasional.Clear();
            if (posko.ModeNasional)
            {
                var nasional = await _layanan.Dasbor.MuatRingkasanNasionalAsync();
                int terbanyak = Math.Max(1, nasional.Count == 0 ? 1 : nasional.Max(n => n.Ringkasan.JumlahHotspot));
                foreach (var (provinsi, ringkasan, jumlahKabupaten) in nasional)
                {
                    Nasional.Add(new RingkasanProvinsiViewModel(provinsi.Nama, ringkasan.JumlahHotspot, ringkasan.StatusDominan, jumlahKabupaten, provinsi.Nama == _provinsi, terbanyak));
                }
            }
            TampilkanNasional = posko.ModeNasional;
        }
        catch (Exception ex)
        {
            _sedangMemuatDaftar = false;
            Message = $"Gagal memuat data dari database: {ex.Message}";
        }
    }

    // Hitung ulang grafik dan tiga kartu ringkasan untuk wilayah terpilih.
    private async Task MuatSeriAsync()
    {
        if (SelectedWilayah is null || !_wilayahIdPerLabel.TryGetValue(SelectedWilayah, out int? wilayahId))
            return;

        try
        {
            var seri = await _layanan.Dasbor.MuatRiwayatAsync(_provinsi, wilayahId, _jumlahHari);

            HotspotHarian.Clear();
            foreach (var (tanggal, jumlah) in seri.Hari)
            {
                // Batang hari tanpa hotspot tetap digambar tipis supaya sumbu tanggal terbaca.
                double tinggi = seri.Puncak == 0 ? 3 : Math.Max(3, jumlah * TinggiBatangMaks / seri.Puncak);
                HotspotHarian.Add(new HotspotHarianViewModel(
                    tanggal.Day.ToString(), jumlah, tinggi, seri.Puncak > 0 && tanggal == seri.TanggalPuncak, tanggal == DasborService.HariIniWib));
            }

            TotalHotspot = seri.Total.ToString("N0", Id);
            PuncakHarian = seri.Puncak.ToString("N0", Id);
            RataRataHarian = seri.RataRata.ToString("0.#", Id);

            TanggalPuncak = seri.Puncak == 0
                ? "tidak ada hotspot"
                : seri.TanggalPuncak.ToString("d MMMM", Id) + (seri.TanggalPuncak == DasborService.HariIniWib ? ", hari ini" : "");

            string pekanLalu = seri.RataRataPekanLalu.ToString("0.#", Id);
            PerbandinganRataRata = seri.RataRata > seri.RataRataPekanLalu
                ? $"naik dari {pekanLalu} pekan lalu"
                : seri.RataRata < seri.RataRataPekanLalu
                    ? $"turun dari {pekanLalu} pekan lalu"
                    : $"sama dengan pekan lalu ({pekanLalu})";

            int hariKosong = seri.Hari.Count(h => h.Jumlah == 0);
            LabelTotal = $"hotspot dalam {_jumlahHari} hari";
            KabarBaik = hariKosong == 0
                ? "Ada hotspot setiap hari dalam rentang ini. Tutupan awan dapat menyembunyikan deteksi."
                : $"{hariKosong} dari {_jumlahHari} hari tanpa hotspot. Tutupan awan dapat menyembunyikan deteksi.";

            var awal = seri.Hari[0].Tanggal;
            var akhir = seri.Hari[^1].Tanggal;
            JudulGrafik = $"HOTSPOT PER HARI · {awal.ToString("d MMM", Id)} – {akhir.ToString("d MMM yyyy", Id)}".ToUpperInvariant();
            Subjudul = $"Jumlah hotspot per hari, {awal.ToString("d MMMM", Id)} – {akhir.ToString("d MMMM yyyy", Id)} (tanggal WIB)";
            RentangTanggal = $"{awal.ToString("d MMM", Id)} sampai {akhir.ToString("d MMMM", Id)}";
            Message = "";
        }
        catch (Exception ex)
        {
            Message = $"Gagal memuat riwayat: {ex.Message}";
        }
    }

    public void NavigateToPage(string page) => _navigateToPage(page);

    public void Logout() => _logout();
}

public class HotspotHarianViewModel
{
    public string Tanggal { get; }
    public string Jumlah { get; }
    public double TinggiBatang { get; }
    public bool IsPeak { get; }
    public bool HariIni { get; }
    public bool Kosong { get; }

    public Avalonia.Media.IBrush Warna { get; }
    public Avalonia.Media.IBrush WarnaAngka { get; }
    public Avalonia.Media.IBrush WarnaTanggal { get; }

    public HotspotHarianViewModel(string tanggal, int jumlah, double tinggiBatang, bool isPeak, bool hariIni)
    {
        Tanggal = tanggal;
        Jumlah = jumlah.ToString();
        TinggiBatang = tinggiBatang;
        IsPeak = isPeak;
        HariIni = hariIni;
        Kosong = jumlah == 0;

        // Puncak memakai warna aksen, hari lain cokelat redup, hari kosong hanya garis tipis.
        Warna = Kuas(isPeak ? "#F15A24" : jumlah > 0 ? "#8A4A2E" : "#26313A");
        WarnaAngka = Kuas(jumlah > 0 ? "#E8EDF1" : "#66737E");
        WarnaTanggal = Kuas(hariIni ? "#E8EDF1" : "#66737E");
    }

    private static Avalonia.Media.IBrush Kuas(string warna) =>
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(warna));
}

// Satu baris di tabel ringkasan nasional.
public class RingkasanProvinsiViewModel
{
    public string Provinsi { get; }
    public string Hotspot { get; }
    public string Status { get; }
    public string Kabupaten { get; }
    public bool Posko { get; }
    public double Lebar { get; }
    public Avalonia.Media.IBrush StatusForeground { get; }
    public Avalonia.Media.IBrush WarnaBatang { get; }

    public RingkasanProvinsiViewModel(string provinsi, int hotspot, Backend.Models.StatusSiaga status, int jumlahKabupaten, bool provinsiPosko, int terbanyak)
    {
        // Nama dipakai apa adanya; tanda "posko ini" ditampilkan terpisah lewat Posko.
        Provinsi = provinsi;
        Hotspot = hotspot.ToString();
        Status = $"status dominan {status}";
        Kabupaten = $"{jumlahKabupaten} kabupaten dipantau";
        Posko = provinsiPosko;
        Lebar = hotspot == 0 ? 0 : Math.Max(4, hotspot / (double)terbanyak * RiwayatTrenViewModel.LebarBatangNasional);
        StatusForeground = Kuas(status switch
        {
            Backend.Models.StatusSiaga.Merah => "#F0452E",
            Backend.Models.StatusSiaga.Kuning => "#F2A83B",
            _ => "#5CC98A"
        });
        WarnaBatang = Kuas(provinsiPosko ? "#F15A24" : "#9AA7B2");
    }

    private static Avalonia.Media.IBrush Kuas(string warna) =>
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(warna));
}

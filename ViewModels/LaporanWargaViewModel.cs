using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Models;
using SiagaApi.Backend.Services;

namespace SiagaApi.ViewModels;

public class LaporanWargaViewModel : ViewModelBase, INavigationViewModel
{
    private static readonly IBrush WarnaSukses = new SolidColorBrush(Color.Parse("#5CC98A"));
    private static readonly IBrush WarnaGagal = new SolidColorBrush(Color.Parse("#F0452E"));

    // Label di UI dipetakan ke enum Keparahan di backend.
    private static readonly Dictionary<string, Keparahan> LabelKeparahan = new()
    {
        ["Asap saja"] = Keparahan.Ringan,
        ["Api terlihat"] = Keparahan.Sedang,
        ["Api besar"] = Keparahan.Berat
    };

    public const string SaringSemuaLabel = "Semua";
    public const string SaringMenungguLabel = "Menunggu";
    public const string SaringTerverifikasiLabel = "Terverifikasi";

    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;
    private readonly KoneksiDatabase _db;
    private readonly LaporanWargaService _service;

    // Nama wilayah di dropdown -> id wilayah di database.
    private readonly Dictionary<string, int> _wilayahIdPerLabel = new();

    // Semua laporan dari database; Reports adalah hasil saringan dan pencarian.
    private List<LaporanItemViewModel> _semua = new();
    private bool _sedangMenyimpan;

    public string ActivePage => "Laporan Warga";

    public ObservableCollection<string> WilayahList { get; } = new();

    public ObservableCollection<LaporanItemViewModel> Reports { get; } = new();

    // ---------- Saringan dan pencarian ----------

    private string _saringan = SaringSemuaLabel;
    public string Saringan
    {
        get => _saringan;
        private set
        {
            if (SetProperty(ref _saringan, value))
            {
                OnPropertyChanged(nameof(SaringSemua));
                OnPropertyChanged(nameof(SaringMenunggu));
                OnPropertyChanged(nameof(SaringTerverifikasi));
                TerapkanSaringan();
            }
        }
    }

    public bool SaringSemua => Saringan == SaringSemuaLabel;
    public bool SaringMenunggu => Saringan == SaringMenungguLabel;
    public bool SaringTerverifikasi => Saringan == SaringTerverifikasiLabel;

    public void AturSaringan(string saringan) => Saringan = saringan;

    private string _cari = "";
    public string Cari
    {
        get => _cari;
        set
        {
            if (SetProperty(ref _cari, value))
                TerapkanSaringan();
        }
    }

    private string _jumlahSemua = "0";
    public string JumlahSemua { get => _jumlahSemua; private set => SetProperty(ref _jumlahSemua, value); }

    private string _jumlahMenunggu = "0";
    public string JumlahMenunggu { get => _jumlahMenunggu; private set => SetProperty(ref _jumlahMenunggu, value); }

    private string _jumlahTerverifikasi = "0";
    public string JumlahTerverifikasi { get => _jumlahTerverifikasi; private set => SetProperty(ref _jumlahTerverifikasi, value); }

    private string _keterangan = "Memuat laporan...";
    public string Keterangan { get => _keterangan; private set => SetProperty(ref _keterangan, value); }

    // ---------- Panel "Laporan baru" ----------

    private bool _isPanelOpen;
    public bool IsPanelOpen
    {
        get => _isPanelOpen;
        private set => SetProperty(ref _isPanelOpen, value);
    }

    public void BukaPanel()
    {
        Message = "";
        IsPanelOpen = true;
    }

    public void TutupPanel() => IsPanelOpen = false;

    // Begitu operator mulai memperbaiki isian, pesan kesalahan lama dihapus.
    private void HapusPesanSaatMengisi()
    {
        if (IsPanelOpen && Message.Length > 0)
            Message = "";
    }

    private string _namaPelapor = "";
    public string NamaPelapor
    {
        get => _namaPelapor;
        set
        {
            if (SetProperty(ref _namaPelapor, value))
                HapusPesanSaatMengisi();
        }
    }

    private string? _selectedWilayah;
    public string? SelectedWilayah
    {
        get => _selectedWilayah;
        set => SetProperty(ref _selectedWilayah, value);
    }

    private string _selectedKeparahan = "Api terlihat";
    public string SelectedKeparahan
    {
        get => _selectedKeparahan;
        set
        {
            if (SetProperty(ref _selectedKeparahan, value))
            {
                OnPropertyChanged(nameof(KeparahanAsap));
                OnPropertyChanged(nameof(KeparahanApi));
                OnPropertyChanged(nameof(KeparahanBesar));
            }
        }
    }

    public bool KeparahanAsap => SelectedKeparahan == "Asap saja";
    public bool KeparahanApi => SelectedKeparahan == "Api terlihat";
    public bool KeparahanBesar => SelectedKeparahan == "Api besar";

    private string _deskripsi = "";
    public string Deskripsi
    {
        get => _deskripsi;
        set
        {
            if (SetProperty(ref _deskripsi, value))
                HapusPesanSaatMengisi();
        }
    }

    private string _message = "";
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private IBrush _messageForeground = WarnaSukses;
    public IBrush MessageForeground
    {
        get => _messageForeground;
        set => SetProperty(ref _messageForeground, value);
    }

    public LaporanWargaViewModel(
        Action<string> navigateToPage,
        Action logout)
        : this(navigateToPage, logout, new KoneksiDatabase())
    {
    }

    public LaporanWargaViewModel(
        Action<string> navigateToPage,
        Action logout,
        KoneksiDatabase db)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;
        _db = db;
        _service = new LaporanWargaService(db);

        _ = MuatDataAsync();
    }

    // Dipanggil sekali saat halaman dibuka: isi dropdown wilayah dan tabel laporan dari database.
    private async Task MuatDataAsync()
    {
        try
        {
            var kabupaten = await _service.AmbilKabupatenAsync();
            WilayahList.Clear();
            _wilayahIdPerLabel.Clear();
            foreach (var w in kabupaten)
            {
                string label = $"{w.Nama}, {w.Provinsi}";
                _wilayahIdPerLabel[label] = w.Id;
                WilayahList.Add(label);
            }
            SelectedWilayah = WilayahList.FirstOrDefault();

            await MuatLaporanAsync();
        }
        catch (Exception ex)
        {
            Keterangan = "Laporan tidak bisa dimuat.";
            TampilkanGagal($"Gagal memuat data dari database ({_db.SumberKonfigurasi}): {ex.Message}");
        }
    }

    private async Task MuatLaporanAsync()
    {
        var daftar = await _service.AmbilDaftarAsync();
        _semua = daftar.Select(LaporanItemViewModel.Dari).ToList();

        JumlahSemua = _semua.Count.ToString();
        JumlahMenunggu = _semua.Count(r => !r.Terverifikasi).ToString();
        JumlahTerverifikasi = _semua.Count(r => r.Terverifikasi).ToString();
        TerapkanSaringan();
    }

    private void TerapkanSaringan()
    {
        string cari = Cari.Trim();
        var hasil = _semua.Where(r =>
            (Saringan == SaringSemuaLabel || (Saringan == SaringTerverifikasiLabel) == r.Terverifikasi) &&
            (cari.Length == 0 ||
             r.Pelapor.Contains(cari, StringComparison.OrdinalIgnoreCase) ||
             r.Wilayah.Contains(cari, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Reports.Clear();
        foreach (var r in hasil)
        {
            Reports.Add(r);
        }

        Keterangan = _semua.Count == 0
            ? "Belum ada laporan warga. Tekan Laporan baru untuk mencatat yang pertama."
            : hasil.Count == _semua.Count
                ? $"{hasil.Count} laporan · terbaru di atas · laporan yang ditolak tidak ditampilkan"
                : $"{hasil.Count} dari {_semua.Count} laporan cocok dengan saringan";
    }

    public async Task AddReportAsync()
    {
        if (_sedangMenyimpan)
            return;

        if (string.IsNullOrWhiteSpace(NamaPelapor))
        {
            TampilkanGagal("Nama pelapor wajib diisi.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Deskripsi))
        {
            TampilkanGagal("Deskripsi kejadian wajib diisi.");
            return;
        }

        if (SelectedWilayah is null || !_wilayahIdPerLabel.TryGetValue(SelectedWilayah, out int wilayahId))
        {
            TampilkanGagal("Pilih wilayah terlebih dahulu.");
            return;
        }

        _sedangMenyimpan = true;
        try
        {
            var hasil = await _service.KirimAsync(
                NamaPelapor.Trim(), wilayahId, LabelKeparahan[SelectedKeparahan], Deskripsi.Trim());

            // Laporan baru harus terlihat: saringan dan pencarian dikembalikan ke semula.
            _saringan = SaringSemuaLabel;
            OnPropertyChanged(nameof(Saringan));
            OnPropertyChanged(nameof(SaringSemua));
            OnPropertyChanged(nameof(SaringMenunggu));
            OnPropertyChanged(nameof(SaringTerverifikasi));
            _cari = "";
            OnPropertyChanged(nameof(Cari));
            await MuatLaporanAsync();

            NamaPelapor = "";
            Deskripsi = "";
            IsPanelOpen = false;

            TampilkanSukses(hasil.Status == StatusLaporan.Diverifikasi
                ? $"Laporan tersimpan dan terverifikasi otomatis ({hasil.JumlahHotspotDekat} titik panas dalam 5 km)."
                : "Laporan tersimpan. Belum ada titik panas dalam 5 km, status menunggu pemeriksaan.");
        }
        catch (Exception ex)
        {
            TampilkanGagal($"Gagal menyimpan laporan: {ex.Message}");
        }
        finally
        {
            _sedangMenyimpan = false;
        }
    }

    private void TampilkanSukses(string pesan)
    {
        MessageForeground = WarnaSukses;
        Message = pesan;
    }

    private void TampilkanGagal(string pesan)
    {
        MessageForeground = WarnaGagal;
        Message = pesan;
    }

    public void NavigateToPage(string page)
    {
        _navigateToPage(page);
    }

    public void Logout()
    {
        _logout();
    }
}

public class LaporanItemViewModel
{
    public string Waktu { get; }
    public string Pelapor { get; }
    public string Wilayah { get; }
    public string Keparahan { get; }
    public string CocokHotspot { get; }
    public string Status { get; }
    public bool Terverifikasi { get; }

    public IBrush StatusBackground { get; }
    public IBrush StatusForeground { get; }

    // Ubah satu baris dari database jadi baris tabel UI.
    public static LaporanItemViewModel Dari(RingkasanLaporan r)
    {
        // Database menyimpan UTC; tampilkan dalam jam lokal komputer (WIB di Indonesia barat).
        DateTime lokal = DateTime.SpecifyKind(r.DibuatPadaUtc, DateTimeKind.Utc).ToLocalTime();
        string waktu = lokal.Date == DateTime.Today
            ? lokal.ToString("HH.mm")
            : lokal.Date == DateTime.Today.AddDays(-1)
                ? "Kemarin"
                : lokal.ToString("dd MMM");

        string keparahan = r.Tingkat switch
        {
            Backend.Models.Keparahan.Ringan => "Asap saja",
            Backend.Models.Keparahan.Sedang => "Api terlihat",
            _ => "Api besar"
        };

        string cocok = r.JumlahHotspotDekat > 0
            ? $"{r.JumlahHotspotDekat} titik dalam 5 km"
            : "Tidak ada titik";

        bool terverifikasi = r.Status == StatusLaporan.Diverifikasi;

        return new LaporanItemViewModel(
            waktu,
            r.Pelapor,
            r.NamaWilayah,
            keparahan,
            cocok,
            terverifikasi ? "Terverifikasi" : "Menunggu",
            terverifikasi);
    }

    public LaporanItemViewModel(
        string waktu,
        string pelapor,
        string wilayah,
        string keparahan,
        string cocokHotspot,
        string status,
        bool verified)
    {
        Waktu = waktu;
        Pelapor = pelapor;
        Wilayah = wilayah;
        Keparahan = keparahan;
        CocokHotspot = cocokHotspot;
        Status = status;
        Terverifikasi = verified;

        // Hijau = terverifikasi, kuning = menunggu pemeriksaan (latar transparan 13%).
        string warna = verified ? "5CC98A" : "F2A83B";
        StatusBackground = new SolidColorBrush(Color.Parse("#22" + warna));
        StatusForeground = new SolidColorBrush(Color.Parse("#" + warna));
    }
}

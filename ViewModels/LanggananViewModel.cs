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

public class LanggananViewModel : ViewModelBase, INavigationViewModel
{
    private static readonly IBrush WarnaSukses = new SolidColorBrush(Color.Parse("#5CC98A"));
    private static readonly IBrush WarnaGagal = new SolidColorBrush(Color.Parse("#F0452E"));

    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;
    private readonly LayananAplikasi _layanan;

    // Nama kabupaten di dropdown -> id wilayah di database.
    private readonly Dictionary<string, int> _wilayahIdPerNama = new();
    private bool _sibuk;

    // Semua langganan dari database; Subscriptions adalah hasil saringan dan pencarian.
    private List<LanggananItemViewModel> _semua = new();

    // ---------- Saringan dan pencarian ----------

    private string _saringan = "Semua";
    public string Saringan
    {
        get => _saringan;
        private set
        {
            if (SetProperty(ref _saringan, value))
            {
                OnPropertyChanged(nameof(SaringSemua));
                OnPropertyChanged(nameof(SaringAktif));
                OnPropertyChanged(nameof(SaringDijeda));
                TerapkanSaringan();
            }
        }
    }

    public bool SaringSemua => Saringan == "Semua";
    public bool SaringAktif => Saringan == "Aktif";
    public bool SaringDijeda => Saringan == "Dijeda";

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

    private string _jumlahAktif = "0";
    public string JumlahAktif { get => _jumlahAktif; private set => SetProperty(ref _jumlahAktif, value); }

    private string _jumlahDijeda = "0";
    public string JumlahDijeda { get => _jumlahDijeda; private set => SetProperty(ref _jumlahDijeda, value); }

    private string _keterangan = "Memuat langganan...";
    public string Keterangan { get => _keterangan; private set => SetProperty(ref _keterangan, value); }

    private void TerapkanSaringan()
    {
        string cari = Cari.Trim();
        var hasil = _semua.Where(l =>
            (Saringan == "Semua" || (Saringan == "Aktif") == l.Aktif) &&
            (cari.Length == 0 ||
             l.Email.Contains(cari, StringComparison.OrdinalIgnoreCase) ||
             l.Wilayah.Contains(cari, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Subscriptions.Clear();
        foreach (var l in hasil)
        {
            Subscriptions.Add(l);
        }

        Keterangan = _semua.Count == 0
            ? "Belum ada warga yang berlangganan. Tambahkan lewat formulir di kanan."
            : hasil.Count == _semua.Count
                ? $"{hasil.Count} langganan · satu warga boleh memantau lebih dari satu wilayah"
                : $"{hasil.Count} dari {_semua.Count} langganan cocok dengan saringan";
    }

    public string ActivePage => "Langganan";

    public ObservableCollection<string> WilayahList { get; } = new();
    public ObservableCollection<LanggananItemViewModel> Subscriptions { get; } = new();
    public ObservableCollection<NotifikasiItemViewModel> Notifications { get; } = new();

    private string _email = "";
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string? _selectedWilayah;
    public string? SelectedWilayah
    {
        get => _selectedWilayah;
        set => SetProperty(ref _selectedWilayah, value);
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

    private string _activeCount = "0 AKTIF";
    public string ActiveCount
    {
        get => _activeCount;
        set => SetProperty(ref _activeCount, value);
    }

    private bool _tidakAdaNotifikasi = true;
    public bool TidakAdaNotifikasi
    {
        get => _tidakAdaNotifikasi;
        set => SetProperty(ref _tidakAdaNotifikasi, value);
    }

    public LanggananViewModel(Action<string> navigateToPage, Action logout, LayananAplikasi layanan)
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
            var kabupaten = await _layanan.Langganan.AmbilKabupatenAsync(posko.Provinsi);

            WilayahList.Clear();
            _wilayahIdPerNama.Clear();
            foreach (var w in kabupaten)
            {
                _wilayahIdPerNama[w.Nama] = w.Id;
                WilayahList.Add(w.Nama);
            }
            SelectedWilayah = WilayahList.FirstOrDefault();

            await MuatDaftarAsync();
        }
        catch (Exception ex)
        {
            Tampilkan($"Gagal memuat data dari database: {ex.Message}", gagal: true);
        }
    }

    private async Task MuatDaftarAsync()
    {
        var langganan = await _layanan.Langganan.AmbilLanggananAsync();
        _semua = langganan.Select(l => new LanggananItemViewModel(l)).ToList();
        JumlahSemua = _semua.Count.ToString();
        JumlahAktif = _semua.Count(l => l.Aktif).ToString();
        JumlahDijeda = _semua.Count(l => !l.Aktif).ToString();
        ActiveCount = $"{_semua.Count(l => l.Aktif)} AKTIF";
        TerapkanSaringan();

        var notifikasi = await _layanan.Langganan.AmbilNotifikasiAsync(12);
        Notifications.Clear();
        foreach (var n in notifikasi)
        {
            Notifications.Add(new NotifikasiItemViewModel(n));
        }
        TidakAdaNotifikasi = notifikasi.Count == 0;
    }

    public Task AddSubscriptionAsync()
    {
        return JalankanAsync(async () =>
        {
            if (SelectedWilayah is null || !_wilayahIdPerNama.TryGetValue(SelectedWilayah, out int wilayahId))
                return new HasilLangganan(false, "Pilih wilayah terlebih dahulu.");

            var hasil = await _layanan.Langganan.TambahAsync(Email, wilayahId);
            if (hasil.Berhasil)
                Email = "";
            return hasil;
        });
    }

    public Task JedaAtauLanjutkanAsync(LanggananItemViewModel item)
    {
        return JalankanAsync(async () =>
        {
            bool berubah = item.Aktif
                ? await _layanan.Langganan.JedaAsync(item.Id)
                : await _layanan.Langganan.LanjutkanAsync(item.Id);

            return new HasilLangganan(berubah, !berubah
                ? "Langganan ini sudah diubah operator lain. Daftar diperbarui."
                : item.Aktif
                    ? $"Langganan {item.Email} dijeda, tidak menerima peringatan."
                    : $"Langganan {item.Email} dilanjutkan.");
        });
    }

    public Task HapusAsync(LanggananItemViewModel item)
    {
        return JalankanAsync(async () =>
        {
            bool terhapus = await _layanan.Langganan.HapusAsync(item.Id);
            return new HasilLangganan(terhapus, terhapus
                ? $"Langganan {item.Email} di {item.Wilayah} dihapus."
                : "Langganan ini sudah dihapus operator lain. Daftar diperbarui.");
        });
    }

    // Satu aksi: cegah klik ganda, muat ulang daftar, ubah kegagalan jadi pesan.
    private async Task JalankanAsync(Func<Task<HasilLangganan>> aksi)
    {
        if (_sibuk)
            return;

        _sibuk = true;
        try
        {
            var hasil = await aksi();
            await MuatDaftarAsync();
            Tampilkan(hasil.Pesan, gagal: !hasil.Berhasil);
        }
        catch (Exception ex)
        {
            Tampilkan($"Gagal: {ex.Message}", gagal: true);
        }
        finally
        {
            _sibuk = false;
        }
    }

    private void Tampilkan(string pesan, bool gagal)
    {
        MessageForeground = gagal ? WarnaGagal : WarnaSukses;
        Message = pesan;
    }

    public void NavigateToPage(string page) => _navigateToPage(page);

    public void Logout() => _logout();
}

public class LanggananItemViewModel
{
    public int Id { get; }
    public string Email { get; }
    public string Wilayah { get; }
    public string Sejak { get; }
    public string Status { get; }
    public bool Aktif { get; }
    public string LabelJeda { get; }

    public IBrush StatusBackground { get; }
    public IBrush StatusForeground { get; }

    public LanggananItemViewModel(LanggananItem item)
    {
        Id = item.Id;
        Email = item.Email;
        Wilayah = item.NamaWilayah;
        Sejak = (item.TerdaftarUtc + DasborService.Wib).ToString("dd MMM");
        Aktif = item.Status == StatusLangganan.Aktif;
        Status = Aktif ? "Aktif" : "Dijeda";
        LabelJeda = Aktif ? "Jeda" : "Lanjutkan";

        // Hijau = aktif menerima peringatan, abu = dijeda (latar transparan 13%).
        string warna = Aktif ? "5CC98A" : "9AA7B2";
        StatusBackground = new SolidColorBrush(Color.Parse("#22" + warna));
        StatusForeground = new SolidColorBrush(Color.Parse("#" + warna));
    }
}

public class NotifikasiItemViewModel
{
    public string Waktu { get; }
    public string Email { get; }
    public string Pemicu { get; }
    public string Hasil { get; }

    public IBrush HasilBackground { get; }
    public IBrush HasilForeground { get; }

    public NotifikasiItemViewModel(CatatanNotifikasi catatan)
    {
        var lokal = catatan.WaktuUtc + DasborService.Wib;
        Waktu = DateOnly.FromDateTime(lokal) == DasborService.HariIniWib ? lokal.ToString("HH.mm") : lokal.ToString("dd MMM");
        Email = catatan.Email;

        // Alasan dilewati / gagal ikut ditampilkan supaya operator tahu harus berbuat apa.
        string pemicu = $"{catatan.NamaWilayah} · confidence {Confidence.Label(catatan.Confidence).ToLowerInvariant()}";
        Pemicu = string.IsNullOrWhiteSpace(catatan.Keterangan) ? pemicu : $"{pemicu} · {catatan.Keterangan}";
        Hasil = catatan.Hasil.ToString();

        (string latar, string teks) = catatan.Hasil switch
        {
            HasilNotifikasi.Terkirim => ("#225CC98A", "#5CC98A"),
            HasilNotifikasi.Gagal => ("#22F0452E", "#F0452E"),
            _ => ("#229AA7B2", "#9AA7B2")
        };
        HasilBackground = new SolidColorBrush(Color.Parse(latar));
        HasilForeground = new SolidColorBrush(Color.Parse(teks));
    }
}

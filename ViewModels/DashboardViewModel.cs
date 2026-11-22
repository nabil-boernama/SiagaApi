using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using SiagaApi.Backend.Models;
using SiagaApi.Backend.Services;
using SiagaApi.ViewModels.Components;

namespace SiagaApi.ViewModels;

public class DashboardViewModel : ViewModelBase, INavigationViewModel
{
    private static readonly IBrush WarnaInfo = new SolidColorBrush(Color.Parse("#9AA7B2"));
    private static readonly IBrush WarnaGagal = new SolidColorBrush(Color.Parse("#F0452E"));
    private static readonly IBrush WarnaTeks = new SolidColorBrush(Color.Parse("#E8EDF1"));
    private static readonly IBrush WarnaBaik = new SolidColorBrush(Color.Parse("#5CC98A"));
    private static readonly IBrush WarnaNominal = new SolidColorBrush(Color.Parse("#F2A83B"));

    // Lebar penuh batang indeks risiko di panel kanan (lebar panel dikurangi tepi).
    private const double LebarBatangRisiko = 324;

    private static IBrush WarnaStatus(StatusSiaga status) => status switch
    {
        StatusSiaga.Merah => WarnaGagal,
        StatusSiaga.Kuning => WarnaNominal,
        _ => WarnaBaik
    };

    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;
    private readonly LayananAplikasi _layanan;
    private readonly Action<int> _bukaSimulasi;

    public string ActivePage => "Dashboard";

    public ObservableCollection<StatCardViewModel> Statistics { get; } = new()
    {
        new StatCardViewModel { Label = "hotspot 24 jam", Value = "–", Description = "memuat..." },
        new StatCardViewModel { Label = "confidence tinggi", Value = "–", Description = "memuat..." },
        new StatCardViewModel { Label = "risiko tertinggi", Value = "–", Description = "memuat..." },
        new StatCardViewModel { Label = "laporan warga hari ini", Value = "–", Description = "memuat..." }
    };

    public ObservableCollection<HotspotItemViewModel> HotspotItems { get; } = new();

    // Dipanggil setiap kali peta perlu digambar ulang (data, mode, zoom, pilihan berubah).
    public event Action? PetaBerubah;

    public DataDasbor? Data { get; private set; }
    public bool ModeTitik { get; private set; }
    public string PetaDasar { get; private set; }
    public int? TerpilihId { get; private set; }

    public KabupatenDasbor? Terpilih => Data?.Kabupaten.FirstOrDefault(k => k.Wilayah.Id == TerpilihId);

    private string _title = "Dashboard";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _lastUpdated = "memuat...";
    public string LastUpdated
    {
        get => _lastUpdated;
        set => SetProperty(ref _lastUpdated, value);
    }

    private string _message = "";
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private IBrush _messageForeground = WarnaInfo;
    public IBrush MessageForeground
    {
        get => _messageForeground;
        set => SetProperty(ref _messageForeground, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _detailTitle = "DETAIL KABUPATEN";
    public string DetailTitle
    {
        get => _detailTitle;
        set => SetProperty(ref _detailTitle, value);
    }

    private string _detailInfo = "";
    public string DetailInfo
    {
        get => _detailInfo;
        set => SetProperty(ref _detailInfo, value);
    }

    private bool _detailKosong = true;
    public bool DetailKosong
    {
        get => _detailKosong;
        set => SetProperty(ref _detailKosong, value);
    }

    // ---------- Panel kabupaten terpilih ----------

    private string _namaTerpilih = "";
    public string NamaTerpilih { get => _namaTerpilih; set => SetProperty(ref _namaTerpilih, value); }

    private string _ringkasTerpilih = "";
    public string RingkasTerpilih { get => _ringkasTerpilih; set => SetProperty(ref _ringkasTerpilih, value); }

    private string _risikoNilai = "–";
    public string RisikoNilai { get => _risikoNilai; set => SetProperty(ref _risikoNilai, value); }

    private string _risikoKeterangan = "belum dihitung";
    public string RisikoKeterangan { get => _risikoKeterangan; set => SetProperty(ref _risikoKeterangan, value); }

    private IBrush _risikoBrush = WarnaInfo;
    public IBrush RisikoBrush { get => _risikoBrush; set => SetProperty(ref _risikoBrush, value); }

    private double _risikoLebar;
    public double RisikoLebar { get => _risikoLebar; set => SetProperty(ref _risikoLebar, value); }

    private string _cuacaSuhu = "–";
    public string CuacaSuhu { get => _cuacaSuhu; set => SetProperty(ref _cuacaSuhu, value); }

    private string _cuacaKelembapan = "–";
    public string CuacaKelembapan { get => _cuacaKelembapan; set => SetProperty(ref _cuacaKelembapan, value); }

    private string _cuacaAngin = "–";
    public string CuacaAngin { get => _cuacaAngin; set => SetProperty(ref _cuacaAngin, value); }

    private string _cuacaArah = "angin";
    public string CuacaArah { get => _cuacaArah; set => SetProperty(ref _cuacaArah, value); }

    // Provinsi tanpa hotspot 24 jam: dinyatakan sebagai kabar baik, bukan layar kosong.
    private bool _provinsiAman;
    public bool ProvinsiAman { get => _provinsiAman; set => SetProperty(ref _provinsiAman, value); }

    private bool _bisaSimulasi;
    public bool BisaSimulasi
    {
        get => _bisaSimulasi;
        set => SetProperty(ref _bisaSimulasi, value);
    }

    public DashboardViewModel(
        Action<string> navigateToPage,
        Action logout,
        LayananAplikasi layanan,
        Action<int> bukaSimulasi)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;
        _layanan = layanan;
        _bukaSimulasi = bukaSimulasi;
        PetaDasar = layanan.Posko.PetaDasar;

        _ = MuatAsync();
    }

    // Membaca ulang semua angka dan peta dari database.
    public async Task MuatAsync()
    {
        try
        {
            var posko = await _layanan.MuatPoskoAsync();
            Data = await _layanan.Dasbor.MuatAsync(posko.Provinsi);
            StatusSumber.Perbarui(Data.Sinkron, databaseTerhubung: true);

            Title = $"Dashboard Provinsi {Data.Provinsi}";
            ProvinsiAman = Data.Hotspot24Jam == 0;
            LastUpdated = Data.Sinkron.TryGetValue("FIRMS", out var firms) && firms.Berhasil
                ? $"{firms.WaktuUtc + DasborService.Wib:dd MMM HH.mm} WIB"
                : "belum pernah diambil";

            IsiStatistik(Data);

            // Pertahankan pilihan operator; kalau belum ada, pilih kabupaten dengan hotspot terbanyak.
            if (Terpilih is null)
                TerpilihId = Data.Kabupaten.OrderByDescending(k => k.Jumlah).ThenBy(k => k.Wilayah.Nama).FirstOrDefault()?.Wilayah.Id;

            IsiDetail();
            PetaBerubah?.Invoke();
        }
        catch (Exception ex)
        {
            StatusSumber.Perbarui(new System.Collections.Generic.Dictionary<string, Backend.Data.CatatanSinkron>(), databaseTerhubung: false);
            TampilkanPesan($"Gagal memuat data dari database: {ex.Message}", gagal: true);
        }
    }

    // Tombol "Perbarui data": ambil hotspot FIRMS dan cuaca terbaru, simpan ke
    // database, lalu muat ulang tampilan.
    public async Task RefreshDataAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        TampilkanPesan("Mengambil hotspot FIRMS dan cuaca terbaru...", gagal: false);
        try
        {
            // Pengambilan pertama mengisi 7 hari sekaligus supaya Riwayat & Tren punya data.
            bool pertamaKali = Data is null || !Data.Sinkron.ContainsKey("FIRMS");
            var hasil = await _layanan.Sinkron.JalankanAsync(riwayat7Hari: pertamaKali);
            await MuatAsync();
            TampilkanPesan("Selesai: " + hasil.Ringkasan(), gagal: hasil.Kesalahan.Count > 0);
        }
        catch (Exception ex)
        {
            TampilkanPesan($"Gagal memperbarui data: {ex.Message}", gagal: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void IsiStatistik(DataDasbor d)
    {
        int selisih = d.Hotspot24Jam - d.Hotspot24JamSebelumnya;
        Statistics[0].Value = d.Hotspot24Jam.ToString();
        Statistics[0].Description = selisih switch
        {
            > 0 => $"+{selisih} dari 24 jam sebelumnya",
            < 0 => $"{selisih} dari 24 jam sebelumnya",
            _ => "sama dengan 24 jam sebelumnya"
        };

        Statistics[1].Value = d.ConfidenceTinggi.ToString();
        Statistics[1].Description = "belum terverifikasi";
        Statistics[1].Warna = d.ConfidenceTinggi > 0 ? WarnaGagal : WarnaTeks;

        if (d.RisikoTertinggi?.Risiko is { } risiko)
        {
            Statistics[2].Value = $"{risiko.Skor}/100";
            Statistics[2].Description = $"{d.RisikoTertinggi.Wilayah.Nama} · {risiko.Status}";
            Statistics[2].Warna = WarnaStatus(risiko.Status);
        }
        else
        {
            Statistics[2].Value = "–";
            Statistics[2].Description = "belum ada data cuaca";
            Statistics[2].Warna = WarnaTeks;
        }

        Statistics[3].Value = d.LaporanHariIni.ToString();
        Statistics[3].Description = $"{d.LaporanCocok} cocok dengan hotspot";
    }

    private void IsiDetail()
    {
        HotspotItems.Clear();
        var k = Terpilih;
        if (k is null)
        {
            DetailTitle = "DETAIL KABUPATEN";
            NamaTerpilih = "Belum ada kabupaten";
            RingkasTerpilih = "Provinsi ini belum punya kabupaten di database.";
            DetailInfo = "";
            DetailKosong = true;
            BisaSimulasi = false;
            return;
        }

        var id = CultureInfo.GetCultureInfo("id-ID");
        DetailTitle = $"DETAIL · {k.Wilayah.Nama.ToUpperInvariant()} · {k.Jumlah} HOTSPOT";
        NamaTerpilih = k.Wilayah.Nama;
        RingkasTerpilih = k.Jumlah == 0
            ? "Tidak ada hotspot dalam 24 jam terakhir"
            : $"{k.Jumlah} hotspot dalam 24 jam terakhir";

        if (k.Risiko is { } risiko)
        {
            RisikoNilai = risiko.Skor.ToString();
            RisikoKeterangan = $"/100 · {risiko.Status}";
            RisikoBrush = WarnaStatus(risiko.Status);
            // Batang minimal 6 piksel supaya skor kecil tetap terlihat.
            RisikoLebar = Math.Max(6, risiko.Skor / 100.0 * LebarBatangRisiko);
        }
        else
        {
            RisikoNilai = "–";
            RisikoKeterangan = "belum dihitung";
            RisikoBrush = WarnaInfo;
            RisikoLebar = 0;
        }

        if (k.Cuaca is { } cuaca)
        {
            CuacaSuhu = string.Create(id, $"{cuaca.Suhu:0.#}°C");
            CuacaKelembapan = $"{cuaca.Kelembapan:0}%";
            CuacaAngin = string.Create(id, $"{cuaca.KecepatanAngin:0.#} km/j");
            CuacaArah = $"dari {Geo.NamaArah(cuaca.ArahAngin).ToLowerInvariant()}";
        }
        else
        {
            CuacaSuhu = CuacaKelembapan = CuacaAngin = "–";
            CuacaArah = "angin";
        }

        DetailInfo = k.Cuaca is null ? "Belum ada data cuaca. Tekan Perbarui data." : "";

        foreach (var h in k.Hotspot.Take(7))
        {
            HotspotItems.Add(new HotspotItemViewModel(h));
        }

        DetailKosong = k.Jumlah == 0;
        BisaSimulasi = true;
    }

    public void PilihKabupaten(int wilayahId)
    {
        if (TerpilihId == wilayahId)
            return;

        TerpilihId = wilayahId;
        IsiDetail();
        PetaBerubah?.Invoke();
    }

    public void AturMode(bool modeTitik)
    {
        if (ModeTitik == modeTitik)
            return;

        ModeTitik = modeTitik;
        PetaBerubah?.Invoke();
    }

    public void AturPetaDasar(string petaDasar)
    {
        if (PetaDasar == petaDasar)
            return;

        PetaDasar = petaDasar;
        PetaBerubah?.Invoke();
    }

    // Tombol "Simulasikan klaster ini": buka Peta & Simulasi untuk kabupaten terpilih.
    public void SimulasikanTerpilih()
    {
        if (TerpilihId is { } id)
            _bukaSimulasi(id);
    }

    private void TampilkanPesan(string pesan, bool gagal)
    {
        MessageForeground = gagal ? WarnaGagal : WarnaInfo;
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

// Satu baris di daftar hotspot kabupaten terpilih.
public class HotspotItemViewModel
{
    private static readonly IBrush WarnaDiperkuat = new SolidColorBrush(Color.Parse("#5CC98A"));
    private static readonly IBrush WarnaIndikasi = new SolidColorBrush(Color.Parse("#66737E"));

    public string Koordinat { get; }
    public string Confidence { get; }
    public string Status { get; }
    public string Keterangan { get; }
    public string Waktu { get; }
    public IBrush StatusForeground { get; }
    public IBrush TitikBrush { get; }

    public HotspotItemViewModel(HotspotDasbor h)
    {
        Koordinat = string.Create(CultureInfo.InvariantCulture, $"{h.Titik.Lat:0.000}, {h.Titik.Lon:0.000}");
        Confidence = h.LabelConfidence;
        // Diperkuat = ada laporan warga terverifikasi dalam 5 km dari hotspot ini.
        Status = h.Diperkuat ? "Diperkuat" : "Indikasi";
        Keterangan = h.Diperkuat ? $"{h.LabelConfidence} · Diperkuat laporan warga" : $"{h.LabelConfidence} · Indikasi";
        Waktu = $"{h.Titik.WaktuDeteksi + DasborService.Wib:HH.mm} WIB";
        StatusForeground = h.Diperkuat ? WarnaDiperkuat : WarnaIndikasi;
        TitikBrush = new SolidColorBrush(Color.Parse(h.Tingkat switch
        {
            TingkatConfidence.Tinggi => "#F0452E",
            TingkatConfidence.Nominal => "#F2A83B",
            _ => "#5CC98A"
        }));
    }
}

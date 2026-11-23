using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using SiagaApi.Backend.Services;

namespace SiagaApi.ViewModels;

public class PetaSimulasiViewModel : ViewModelBase, INavigationViewModel
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");
    private static readonly IBrush WarnaInfo = new SolidColorBrush(Color.Parse("#9AA7B2"));
    private static readonly IBrush WarnaGagal = new SolidColorBrush(Color.Parse("#F0452E"));

    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;
    private readonly LayananAplikasi _layanan;

    // Kabupaten yang diminta dari tombol "Simulasikan klaster ini" di Dashboard.
    private readonly int? _wilayahAwalId;
    private bool _sedangMemuatDaftar;

    public string ActivePage => "Peta & Simulasi";

    public ObservableCollection<PilihanWilayahViewModel> WilayahList { get; } = new();

    // Dipanggil setiap kali grid perlu digambar ulang (hasil baru atau jam digeser).
    public event Action? GridBerubah;

    public HasilMonteCarlo? Hasil { get; private set; }

    private PilihanWilayahViewModel? _selectedWilayah;
    public PilihanWilayahViewModel? SelectedWilayah
    {
        get => _selectedWilayah;
        set
        {
            if (SetProperty(ref _selectedWilayah, value) && value is not null && !_sedangMemuatDaftar)
                _ = RunSimulationAsync();
        }
    }

    private double _simulationHour = 3;
    public double SimulationHour
    {
        get => _simulationHour;
        set
        {
            if (SetProperty(ref _simulationHour, value))
                PerbaruiTeksHasil();
        }
    }

    public int Jam => (int)Math.Round(SimulationHour);

    // ---------- Garis waktu: pilih jam, atau putar otomatis t+0 sampai t+6 ----------

    private readonly Avalonia.Threading.DispatcherTimer _pemutar = new() { Interval = TimeSpan.FromMilliseconds(850) };

    private bool _sedangDiputar;
    public bool SedangDiputar
    {
        get => _sedangDiputar;
        private set => SetProperty(ref _sedangDiputar, value);
    }

    public void AturJam(int jam)
    {
        SimulationHour = Math.Clamp(jam, 0, SimulasiService.JamSimulasi);
    }

    // Tombol putar: maju satu jam tiap langkah, berhenti di jam terakhir.
    public void PutarAtauJeda()
    {
        if (SedangDiputar)
        {
            HentikanPutar();
            return;
        }

        if (Hasil is null)
            return;

        if (Jam >= SimulasiService.JamSimulasi)
            AturJam(0);

        SedangDiputar = true;
        _pemutar.Start();
    }

    public void HentikanPutar()
    {
        _pemutar.Stop();
        SedangDiputar = false;
    }

    private void LangkahPutar(object? sender, EventArgs e)
    {
        if (Jam >= SimulasiService.JamSimulasi)
        {
            HentikanPutar();
            return;
        }
        AturJam(Jam + 1);
    }

    // ---------- Angka rincian di panel kanan ----------

    private string _luasMungkin = "–";
    public string LuasMungkin { get => _luasMungkin; set => SetProperty(ref _luasMungkin, value); }

    private string _luasJangkauan = "–";
    public string LuasJangkauan { get => _luasJangkauan; set => SetProperty(ref _luasJangkauan, value); }

    private string _anginRingkas = "";
    public string AnginRingkas { get => _anginRingkas; set => SetProperty(ref _anginRingkas, value); }

    // Arah DORONGAN angin dalam derajat kompas, untuk memutar panah di peta.
    private double _arahDorong;
    public double ArahDorong { get => _arahDorong; set => SetProperty(ref _arahDorong, value); }

    private bool _adaHasil;
    public bool AdaHasil { get => _adaHasil; set => SetProperty(ref _adaHasil, value); }

    public string SimulationHourStart => "t+0";
    public string SimulationHourEnd => $"t+{SimulasiService.JamSimulasi}";

    private string _headerTitle = "Simulasi penyebaran";
    public string HeaderTitle { get => _headerTitle; set => SetProperty(ref _headerTitle, value); }

    private string _headerSubtitle = "Memuat data hotspot dan cuaca...";
    public string HeaderSubtitle { get => _headerSubtitle; set => SetProperty(ref _headerSubtitle, value); }

    private string _simulationTimeText = "WAKTU SIMULASI";
    public string SimulationTimeText { get => _simulationTimeText; set => SetProperty(ref _simulationTimeText, value); }

    private string _simulationResultLabel = "HASIL SKENARIO";
    public string SimulationResultLabel { get => _simulationResultLabel; set => SetProperty(ref _simulationResultLabel, value); }

    private string _simulationArea = "–";
    public string SimulationArea { get => _simulationArea; set => SetProperty(ref _simulationArea, value); }

    private string _simulationAreaNote = "";
    public string SimulationAreaNote { get => _simulationAreaNote; set => SetProperty(ref _simulationAreaNote, value); }

    private string _simulationDescription = "";
    public string SimulationDescription { get => _simulationDescription; set => SetProperty(ref _simulationDescription, value); }

    private string _windSpeedText = "–";
    public string WindSpeedText { get => _windSpeedText; set => SetProperty(ref _windSpeedText, value); }

    private string _windDirectionText = "–";
    public string WindDirectionText { get => _windDirectionText; set => SetProperty(ref _windDirectionText, value); }

    private string _humidityText = "–";
    public string HumidityText { get => _humidityText; set => SetProperty(ref _humidityText, value); }

    private string _temperatureText = "–";
    public string TemperatureText { get => _temperatureText; set => SetProperty(ref _temperatureText, value); }

    private string _weatherSourceText = "";
    public string WeatherSourceText { get => _weatherSourceText; set => SetProperty(ref _weatherSourceText, value); }

    private string _message = "";
    public string Message { get => _message; set => SetProperty(ref _message, value); }

    private IBrush _messageForeground = WarnaInfo;
    public IBrush MessageForeground { get => _messageForeground; set => SetProperty(ref _messageForeground, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    public PetaSimulasiViewModel(
        Action<string> navigateToPage,
        Action logout,
        LayananAplikasi layanan,
        int? wilayahAwalId = null)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;
        _layanan = layanan;
        _wilayahAwalId = wilayahAwalId;
        _pemutar.Tick += LangkahPutar;

        _ = MuatAsync();
    }

    // Isi pilihan kabupaten, lalu jalankan simulasi pertama.
    private async Task MuatAsync()
    {
        try
        {
            var posko = await _layanan.MuatPoskoAsync();
            var berhotspot = await _layanan.Simulasi.AmbilWilayahBerhotspotAsync(posko.Provinsi);

            _sedangMemuatDaftar = true;
            WilayahList.Clear();
            foreach (var (wilayah, jam24, hari7) in berhotspot)
            {
                WilayahList.Add(new PilihanWilayahViewModel(wilayah.Id, wilayah.Nama, jam24, hari7));
            }

            // Kabupaten dari Dashboard tetap bisa dipilih walau tidak punya hotspot,
            // supaya operator mendapat penjelasan, bukan halaman kosong.
            if (_wilayahAwalId is { } awal && WilayahList.All(w => w.Id != awal))
            {
                var kabupaten = await _layanan.Dasbor.AmbilKabupatenAsync(posko.Provinsi);
                var w = kabupaten.FirstOrDefault(k => k.Id == awal);
                if (w is not null)
                    WilayahList.Add(new PilihanWilayahViewModel(w.Id, w.Nama, 0, 0));
            }

            SelectedWilayah = WilayahList.FirstOrDefault(w => w.Id == _wilayahAwalId) ?? WilayahList.FirstOrDefault();
            _sedangMemuatDaftar = false;

            if (SelectedWilayah is null)
            {
                HeaderSubtitle = $"Tidak ada hotspot di Provinsi {posko.Provinsi} dalam 7 hari terakhir.";
                TampilkanPesan("Belum ada klaster untuk disimulasikan. Tekan \"Perbarui data\" di Dashboard untuk mengambil hotspot terbaru.", gagal: false);
                return;
            }

            await RunSimulationAsync();
        }
        catch (Exception ex)
        {
            _sedangMemuatDaftar = false;
            TampilkanPesan($"Gagal memuat data dari database: {ex.Message}", gagal: true);
        }
    }

    // Tombol "Jalankan ulang simulasi" dan pergantian kabupaten.
    public async Task RunSimulationAsync()
    {
        if (IsBusy || SelectedWilayah is null)
            return;

        HentikanPutar();
        IsBusy = true;
        TampilkanPesan("Menjalankan simulasi...", gagal: false);
        try
        {
            Hasil = await _layanan.Simulasi.JalankanAsync(SelectedWilayah.Id);

            var c = Hasil.Cuaca;
            HeaderTitle = $"Simulasi penyebaran: Klaster {Hasil.Wilayah.Nama}";
            HeaderSubtitle =
                $"{Hasil.Klaster.Count} dari {Hasil.JumlahHotspotWilayah} hotspot masuk radius 10 km dari titik pemicu, {(Hasil.MemakaiRentang7Hari ? "7 hari terakhir (tidak ada yang 24 jam)" : "24 jam terakhir")}" +
                $" · {Hasil.JumlahSimulasi} kali simulasi · sel sekitar {Hasil.UkuranSel * 111320:0} m · skenario hipotetis";
            AnginRingkas = string.Create(Id,
                $"Angin dari {Geo.NamaArah(c.ArahAngin).ToLowerInvariant()} ke {Geo.NamaArah(c.ArahAngin + 180).ToLowerInvariant()} · {c.KecepatanAngin:0.#} km/j");
            ArahDorong = (c.ArahAngin + 180) % 360;

            WindSpeedText = string.Create(Id, $"{c.KecepatanAngin:0.#} km/jam");
            WindDirectionText = $"{Geo.NamaArah(c.ArahAngin)} {c.ArahAngin:0}°";
            HumidityText = $"{c.Kelembapan:0}%";
            TemperatureText = string.Create(Id, $"{c.Suhu:0.#}°C");
            WeatherSourceText = $"{c.Sumber} · {c.Waktu + DasborService.Wib:dd MMM HH.mm} WIB";

            TampilkanPesan("", gagal: false);
            PerbaruiTeksHasil();
        }
        catch (InvalidOperationException ex)
        {
            // Kondisi yang wajar: belum ada hotspot atau belum ada data cuaca.
            Hasil = null;
            HeaderTitle = $"Simulasi penyebaran: {SelectedWilayah.Nama}";
            HeaderSubtitle = "Simulasi belum bisa dijalankan.";
            TampilkanPesan(ex.Message, gagal: false);
            PerbaruiTeksHasil();
        }
        catch (Exception ex)
        {
            TampilkanPesan($"Gagal menjalankan simulasi: {ex.Message}", gagal: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PerbaruiTeksHasil()
    {
        SimulationTimeText = $"WAKTU SIMULASI · T + {Jam} JAM";
        SimulationResultLabel = $"PERKIRAAN PADA T+{Jam} JAM";
        AdaHasil = Hasil is not null;

        if (Hasil is null)
        {
            SimulationArea = "–";
            SimulationAreaNote = "";
            SimulationDescription = "";
            LuasMungkin = "–";
            LuasJangkauan = "–";
            AnginRingkas = "";
            GridBerubah?.Invoke();
            return;
        }

        double luasAwal = Hasil.LuasHa(0, 0.10);
        double tambahan = Math.Max(0, Hasil.LuasHa(Jam, 0.10) - luasAwal);
        SimulationArea = string.Create(Id, $"{tambahan:N0} ha");
        SimulationAreaNote = "tambahan area dengan peluang terjangkau ≥ 10%";
        LuasMungkin = string.Create(Id, $"≈ {luasAwal:N0} ha");
        LuasJangkauan = string.Create(Id, $"≈ {Hasil.LuasHa(Jam, 0.10):N0} ha");

        SimulationDescription = Jam == 0
            ? "Kondisi awal. Bulatan adalah deteksi satelit; luas petak awal berasal dari ukuran grid."
            : tambahan == 0
                ? $"Model tidak memperkirakan perluasan di luar petak awal hingga t+{Jam} pada ambang 10%."
                : $"Hingga t+{Jam}, model memperkirakan perluasan jika klaster ini merupakan kebakaran aktif.";

        GridBerubah?.Invoke();
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

// Satu pilihan kabupaten di dropdown halaman simulasi.
public class PilihanWilayahViewModel
{
    public int Id { get; }
    public string Nama { get; }
    public string Label { get; }

    public PilihanWilayahViewModel(int id, string nama, int hotspot24Jam, int hotspot7Hari)
    {
        Id = id;
        Nama = nama;
        // Ringkas supaya muat di dropdown: "Pelalawan · 2 hari ini · 5 sepekan".
        Label = hotspot7Hari == 0
            ? $"{nama} · tidak ada hotspot"
            : $"{nama} · {hotspot24Jam} hari ini · {hotspot7Hari} sepekan";
    }

    public override string ToString() => Label;
}

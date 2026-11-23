using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using SiagaApi.Backend.Models;
using SiagaApi.Backend.Services;

namespace SiagaApi.ViewModels;

public class PengaturanViewModel : ViewModelBase, INavigationViewModel
{
    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;
    private readonly LayananAplikasi _layanan;

    public string ActivePage => "Pengaturan";

    // ---------- Tab ----------
    public const string TabPoskoNama = "Posko";
    public const string TabSumberNama = "Sumber data";
    public const string TabEmailNama = "Email";
    public const string TabAkunNama = "Akun operator";
    public const string TabKeamananNama = "Keamanan";

    private string _tab = TabPoskoNama;
    public string Tab
    {
        get => _tab;
        private set
        {
            if (SetProperty(ref _tab, value))
            {
                OnPropertyChanged(nameof(TabPosko));
                OnPropertyChanged(nameof(TabSumber));
                OnPropertyChanged(nameof(TabEmail));
                OnPropertyChanged(nameof(TabAkun));
                OnPropertyChanged(nameof(TabKeamanan));
                OnPropertyChanged(nameof(TabBisaSimpan));
            }
        }
    }

    public bool TabPosko => Tab == TabPoskoNama;
    public bool TabSumber => Tab == TabSumberNama;
    public bool TabEmail => Tab == TabEmailNama;
    public bool TabAkun => Tab == TabAkunNama;
    public bool TabKeamanan => Tab == TabKeamananNama;

    // Tiga tab pertama berbagi satu tombol "Simpan pengaturan".
    public bool TabBisaSimpan => TabPosko || TabSumber || TabEmail;

    public void AturTab(string tab)
    {
        // Tab Akun operator hanya untuk Admin dan Super Admin.
        if (tab == TabAkunNama && !IsAdmin)
            return;
        Tab = tab;
    }

    // Operator biasa boleh melihat setelan posko tapi tidak mengubahnya.
    public bool BukanAdmin => !IsAdmin;

    // Teks di dropdown <-> nilai yang disimpan.
    private static readonly (string Label, string Nilai)[] PilihanPeta =
    {
        ("Tanpa peta dasar (jalan offline)", PengaturanPosko.TanpaPetaDasar),
        ("OpenStreetMap (peta jalan)", PengaturanPosko.PetaJalan),
        ("Peta satelit", PengaturanPosko.PetaSatelit)
    };

    private static readonly (string Label, int Menit)[] PilihanInterval =
    {
        ("Setiap 15 menit", 15),
        ("Setiap 30 menit", 30),
        ("Setiap 1 jam", 60),
        ("Setiap 3 jam", 180),
        ("Setiap 6 jam", 360)
    };

    // Provinsi yang kabupatennya ada di database.
    public ObservableCollection<string> ProvinsiList { get; } = new();

    public ObservableCollection<string> PetaDasarList { get; } = new(PilihanPeta.Select(p => p.Label));

    public ObservableCollection<string> IntervalList { get; } = new(PilihanInterval.Select(p => p.Label));

    // ---------- Pengaturan posko (database, dipakai bersama) ----------

    private string? _selectedProvinsi;
    public string? SelectedProvinsi
    {
        get => _selectedProvinsi;
        set => SetProperty(ref _selectedProvinsi, value);
    }

    private bool _allowNationalMode = true;
    public bool AllowNationalMode
    {
        get => _allowNationalMode;
        set => SetProperty(ref _allowNationalMode, value);
    }

    private string _selectedMap = PilihanPeta[0].Label;
    public string SelectedMap
    {
        get => _selectedMap;
        set => SetProperty(ref _selectedMap, value);
    }

    private string _selectedInterval = PilihanInterval[1].Label;
    public string SelectedInterval
    {
        get => _selectedInterval;
        set => SetProperty(ref _selectedInterval, value);
    }

    // ---------- Pengaturan komputer ini (kunci API dan SMTP, tidak masuk database) ----------

    private string _firmsApiKey = "";
    public string FirmsApiKey
    {
        get => _firmsApiKey;
        set => SetProperty(ref _firmsApiKey, value);
    }

    private string _weatherApiKey = "";
    public string WeatherApiKey
    {
        get => _weatherApiKey;
        set => SetProperty(ref _weatherApiKey, value);
    }

    private string _smtpServer = "";
    public string SmtpServer
    {
        get => _smtpServer;
        set => SetProperty(ref _smtpServer, value);
    }

    private string _smtpPort = "587";
    public string SmtpPort
    {
        get => _smtpPort;
        set => SetProperty(ref _smtpPort, value);
    }

    private string _senderEmail = "";
    public string SenderEmail
    {
        get => _senderEmail;
        set => SetProperty(ref _senderEmail, value);
    }

    private string _smtpUsername = "";
    public string SmtpUsername
    {
        get => _smtpUsername;
        set => SetProperty(ref _smtpUsername, value);
    }

    private string _smtpPassword = "";
    public string SmtpPassword
    {
        get => _smtpPassword;
        set => SetProperty(ref _smtpPassword, value);
    }

    private string _message = "";
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private IBrush _messageForeground = new SolidColorBrush(Color.Parse("#5CC98A"));
    public IBrush MessageForeground
    {
        get => _messageForeground;
        set => SetProperty(ref _messageForeground, value);
    }

    // ---------- Ganti kata sandi sendiri ----------

    private string _sandiMessage = "";
    public string SandiMessage
    {
        get => _sandiMessage;
        set => SetProperty(ref _sandiMessage, value);
    }

    private IBrush _sandiMessageForeground = new SolidColorBrush(Color.Parse("#5CC98A"));
    public IBrush SandiMessageForeground
    {
        get => _sandiMessageForeground;
        set => SetProperty(ref _sandiMessageForeground, value);
    }

    public string AkunSaya => SesiAplikasi.Operator is { } akun
        ? $"Masuk sebagai {akun.NamaLengkap} ({akun.Username}) · {akun.LabelPeran}"
        : "";

    private bool _sedangMenyimpan;

    private async Task MuatPengaturanAsync()
    {
        try
        {
            var lokal = _layanan.Pengaturan.MuatLokal();
            FirmsApiKey = lokal.FirmsMapKey;
            WeatherApiKey = lokal.AccuWeatherKey;
            SmtpServer = lokal.SmtpHost;
            SmtpPort = lokal.SmtpPort.ToString();
            SenderEmail = lokal.SmtpEmailPengirim;
            SmtpUsername = lokal.SmtpUsername;
            SmtpPassword = lokal.SmtpPassword;

            var provinsi = await _layanan.Dasbor.AmbilProvinsiAsync();
            ProvinsiList.Clear();
            foreach (string p in provinsi)
            {
                ProvinsiList.Add(p);
            }

            var posko = await _layanan.MuatPoskoAsync();
            SelectedProvinsi = ProvinsiList.Contains(posko.Provinsi) ? posko.Provinsi : ProvinsiList.FirstOrDefault();
            AllowNationalMode = posko.ModeNasional;
            SelectedMap = PilihanPeta.FirstOrDefault(p => p.Nilai == posko.PetaDasar, PilihanPeta[0]).Label;
            SelectedInterval = PilihanInterval.FirstOrDefault(p => p.Menit == posko.IntervalMenit, PilihanInterval[1]).Label;
        }
        catch (Exception ex)
        {
            TampilkanPesan($"Gagal memuat pengaturan dari database: {ex.Message}", gagal: true);
        }
    }

    // Tombol "Simpan pengaturan".
    //  - Kunci API dan SMTP disimpan di komputer ini.
    //  - Provinsi, peta dasar, mode nasional, interval disimpan di database
    //    untuk semua operator, dan hanya boleh diubah Admin / Super Admin.
    public async Task SaveSettingsAsync()
    {
        if (_sedangMenyimpan || SesiAplikasi.Operator is not { } pelaku)
            return;

        // SMTP boleh dikosongkan seluruhnya; kalau diisi, harus lengkap dan benar.
        bool smtpDiisi = new[] { SmtpServer, SenderEmail, SmtpUsername, SmtpPassword }.Any(x => !string.IsNullOrWhiteSpace(x));
        int port = 587;
        if (smtpDiisi)
        {
            if (string.IsNullOrWhiteSpace(SmtpServer))
            {
                TampilkanPesan("Server SMTP wajib diisi.", gagal: true);
                return;
            }
            if (!int.TryParse(SmtpPort, out port) || port is < 1 or > 65535)
            {
                TampilkanPesan("Port SMTP harus angka 1-65535 (biasanya 587).", gagal: true);
                return;
            }
            if (string.IsNullOrWhiteSpace(SenderEmail) || !SenderEmail.Contains('@') || !SenderEmail.Contains('.'))
            {
                TampilkanPesan("Alamat email pengirim tidak valid.", gagal: true);
                return;
            }
            if (string.IsNullOrWhiteSpace(SmtpUsername) || string.IsNullOrWhiteSpace(SmtpPassword))
            {
                TampilkanPesan("Nama pengguna dan kata sandi SMTP wajib diisi.", gagal: true);
                return;
            }
        }

        _sedangMenyimpan = true;
        try
        {
            _layanan.Pengaturan.SimpanLokal(new PengaturanLokal
            {
                FirmsMapKey = FirmsApiKey.Trim(),
                AccuWeatherKey = WeatherApiKey.Trim(),
                SmtpHost = SmtpServer.Trim(),
                SmtpPort = port,
                SmtpEmailPengirim = SenderEmail.Trim(),
                SmtpUsername = SmtpUsername.Trim(),
                SmtpPassword = SmtpPassword
            });

            if (!pelaku.Pengelola)
            {
                TampilkanPesan("Kunci API dan SMTP tersimpan di komputer ini. Provinsi, peta dasar, dan interval hanya bisa diubah admin posko.", gagal: false);
                return;
            }

            await _layanan.Pengaturan.SimpanPoskoAsync(new PengaturanPosko
            {
                Provinsi = SelectedProvinsi ?? _layanan.Posko.Provinsi,
                ModeNasional = AllowNationalMode,
                PetaDasar = PilihanPeta.First(p => p.Label == SelectedMap).Nilai,
                IntervalMenit = PilihanInterval.First(p => p.Label == SelectedInterval).Menit
            }, pelaku.Id);
            await _layanan.MuatPoskoAsync();

            TampilkanPesan("Pengaturan tersimpan. Pengaturan posko berlaku untuk semua operator; kunci API dan SMTP hanya di komputer ini.", gagal: false);
        }
        catch (Exception ex)
        {
            TampilkanPesan($"Gagal menyimpan pengaturan: {ex.Message}", gagal: true);
        }
        finally
        {
            _sedangMenyimpan = false;
        }
    }

    // Mengembalikan true kalau kata sandi berhasil diganti.
    public async Task<bool> GantiKataSandiAsync(string sandiLama, string sandiBaru, string konfirmasi)
    {
        if (_sedangMenyimpan || SesiAplikasi.Operator is not { } pelaku)
            return false;

        _sedangMenyimpan = true;
        try
        {
            var hasil = await _layanan.Auth.GantiKataSandiAsync(pelaku.Id, sandiLama, sandiBaru, konfirmasi);
            if (hasil.Berhasil)
            {
                // Semua sesi "Ingat saya" sudah dicabut di database, jadi token di komputer ini dibuang juga.
                _layanan.SesiLokal.Hapus();
            }

            SandiMessageForeground = hasil.Berhasil ? WarnaSukses : WarnaGagal;
            SandiMessage = hasil.Berhasil
                ? "Kata sandi berhasil diganti. Fitur \"Ingat saya\" dimatikan di semua komputer; masuk lagi dengan kata sandi baru."
                : hasil.Pesan;
            return hasil.Berhasil;
        }
        catch (Exception ex)
        {
            SandiMessageForeground = WarnaGagal;
            SandiMessage = $"Gagal mengganti kata sandi: {ex.Message}";
            return false;
        }
        finally
        {
            _sedangMenyimpan = false;
        }
    }

    private void TampilkanPesan(string pesan, bool gagal)
    {
        MessageForeground = gagal ? WarnaGagal : WarnaSukses;
        Message = pesan;
    }

    // ---------- Kelola akun operator (hanya tampil untuk Admin dan Super Admin) ----------

    private static readonly IBrush WarnaSukses = new SolidColorBrush(Color.Parse("#5CC98A"));
    private static readonly IBrush WarnaGagal = new SolidColorBrush(Color.Parse("#F0452E"));

    private AutentikasiService _auth => _layanan.Auth;
    private bool _adminSibuk;

    public bool IsAdmin => SesiAplikasi.Operator?.Pengelola == true;

    public ObservableCollection<AkunItemViewModel> Accounts { get; } = new();

    private string _invitationCode = "";
    public string InvitationCode
    {
        get => _invitationCode;
        set => SetProperty(ref _invitationCode, value);
    }

    // Lencana di tab: "2 menunggu".
    private string _menungguLabel = "";
    public string MenungguLabel
    {
        get => _menungguLabel;
        set => SetProperty(ref _menungguLabel, value);
    }

    private bool _adaMenunggu;
    public bool AdaMenunggu
    {
        get => _adaMenunggu;
        set => SetProperty(ref _adaMenunggu, value);
    }

    private string _accountsTitle = "AKUN OPERATOR";
    public string AccountsTitle
    {
        get => _accountsTitle;
        set => SetProperty(ref _accountsTitle, value);
    }

    private string _adminMessage = "";
    public string AdminMessage
    {
        get => _adminMessage;
        set => SetProperty(ref _adminMessage, value);
    }

    private IBrush _adminMessageForeground = WarnaSukses;
    public IBrush AdminMessageForeground
    {
        get => _adminMessageForeground;
        set => SetProperty(ref _adminMessageForeground, value);
    }

    public PengaturanViewModel(Action<string> navigateToPage, Action logout, LayananAplikasi layanan)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;
        _layanan = layanan;

        _ = MuatPengaturanAsync();
        if (IsAdmin)
            _ = MuatAdminAsync();
    }

    private Task MuatAdminAsync()
    {
        return JalankanAdminAsync(async pelaku =>
        {
            InvitationCode = await _auth.AmbilKodeUndanganAsync(pelaku.Id) ?? "Belum ada kode";
            await MuatAkunAsync(pelaku);
            return null;
        });
    }

    // Semua akun, yang menunggu persetujuan ditaruh paling atas.
    private async Task MuatAkunAsync(AkunOperator pelaku)
    {
        var daftar = await _auth.AmbilSemuaAkunAsync(pelaku.Id);
        Accounts.Clear();
        foreach (var akun in daftar
                     .OrderBy(a => a.Status == StatusAkun.Menunggu ? 0 : 1)
                     .ThenBy(a => a.Peran)
                     .ThenBy(a => a.NamaLengkap))
        {
            Accounts.Add(new AkunItemViewModel(akun, pelaku));
        }

        int menunggu = daftar.Count(a => a.Status == StatusAkun.Menunggu);
        AdaMenunggu = menunggu > 0;
        MenungguLabel = $"{menunggu} menunggu";
        AccountsTitle = menunggu > 0
            ? $"AKUN OPERATOR ({daftar.Count}) · {menunggu} MENUNGGU PERSETUJUAN"
            : $"AKUN OPERATOR ({daftar.Count})";
    }

    public Task BuatKodeUndanganAsync()
    {
        return JalankanAdminAsync(async pelaku =>
        {
            InvitationCode = await _auth.BuatKodeUndanganAsync(pelaku.Id);
            return "Kode undangan baru dibuat. Kode lama sudah tidak berlaku.";
        });
    }

    public Task SetujuiAsync(AkunItemViewModel item) => UbahAkunAsync(item,
        id => _auth.SetujuiAsync(id, item.Id), $"Akun {item.Username} disetujui dan sudah bisa masuk.");

    public Task TolakAsync(AkunItemViewModel item) => UbahAkunAsync(item,
        id => _auth.TolakAsync(id, item.Id), $"Pendaftaran {item.Username} ditolak.");

    public Task NonaktifkanAsync(AkunItemViewModel item) => UbahAkunAsync(item,
        id => _auth.NonaktifkanAsync(id, item.Id), $"Akun {item.Username} dinonaktifkan dan tidak bisa masuk lagi.");

    public Task AktifkanAsync(AkunItemViewModel item) => UbahAkunAsync(item,
        id => _auth.AktifkanAsync(id, item.Id), $"Akun {item.Username} diaktifkan.");

    public Task JadikanAdminAsync(AkunItemViewModel item) => UbahAkunAsync(item,
        id => _auth.UbahPeranAsync(id, item.Id, PeranOperator.Admin), $"Akun {item.Username} sekarang Admin.");

    public Task JadikanOperatorAsync(AkunItemViewModel item) => UbahAkunAsync(item,
        id => _auth.UbahPeranAsync(id, item.Id, PeranOperator.Operator), $"Akun {item.Username} sekarang Operator.");

    public Task ResetKataSandiAsync(AkunItemViewModel item)
    {
        return JalankanAdminAsync(async pelaku =>
        {
            string sementara = await _auth.ResetKataSandiAsync(pelaku.Id, item.Id);
            return $"Kata sandi sementara untuk {item.Username}: {sementara}  Catat sekarang, tidak akan ditampilkan lagi.";
        });
    }

    private Task UbahAkunAsync(AkunItemViewModel item, Func<int, Task<bool>> ubah, string pesanBerhasil)
    {
        return JalankanAdminAsync(async pelaku =>
        {
            bool berubah = await ubah(pelaku.Id);
            await MuatAkunAsync(pelaku);
            return berubah ? pesanBerhasil : $"Akun {item.Username} sudah diubah admin lain. Daftar diperbarui.";
        });
    }

    // Menjalankan satu aksi admin: mencegah klik ganda, dan mengubah
    // kegagalan (database mati, tidak berwenang) jadi pesan di layar.
    private async Task JalankanAdminAsync(Func<AkunOperator, Task<string?>> aksi)
    {
        if (_adminSibuk || SesiAplikasi.Operator is not { } pelaku)
            return;

        _adminSibuk = true;
        try
        {
            string? pesan = await aksi(pelaku);
            if (pesan is not null)
            {
                AdminMessageForeground = WarnaSukses;
                AdminMessage = pesan;
            }
        }
        catch (Exception ex)
        {
            AdminMessageForeground = WarnaGagal;
            AdminMessage = $"Gagal: {ex.Message}";
        }
        finally
        {
            _adminSibuk = false;
        }
    }

    public void NavigateToPage(string page) => _navigateToPage(page);

    public void Logout() => _logout();
}

// Satu baris akun di panel "Akun operator". Tombol yang tampil ditentukan
// oleh aturan wewenang di AkunOperator.BolehMengelola; backend tetap
// mengecek ulang aturan yang sama saat tombolnya ditekan.
public class AkunItemViewModel
{
    public int Id { get; }
    public string NamaLengkap { get; }
    public string Username { get; }
    public string Rincian { get; }
    public string Lencana { get; }

    // Untuk tabel akun.
    public string Inisial { get; }
    public string NamaPengguna { get; }
    public string Peran { get; }
    public string StatusLabel { get; }
    public string Terdaftar { get; }
    public bool Menunggu { get; }
    public IBrush StatusBackground { get; }
    public IBrush StatusForeground { get; }
    public IBrush LatarBaris { get; }

    public bool BisaSetujui { get; }
    public bool BisaNonaktifkan { get; }
    public bool BisaAktifkan { get; }
    public bool BisaJadikanAdmin { get; }
    public bool BisaJadikanOperator { get; }
    public bool BisaResetSandi { get; }

    public AkunItemViewModel(AkunOperator akun, AkunOperator pelaku)
    {
        Id = akun.Id;
        NamaLengkap = akun.Id == pelaku.Id ? $"{akun.NamaLengkap} (Anda)" : akun.NamaLengkap;
        Username = akun.Username;
        Lencana = $"{akun.LabelPeran} · {akun.Status}";

        // Database menyimpan UTC; tampilkan dalam jam lokal komputer.
        DateTime lokal = DateTime.SpecifyKind(akun.DibuatPada, DateTimeKind.Utc).ToLocalTime();
        Rincian = $"{akun.Username} · {akun.Email} · terdaftar {lokal:dd MMM yyyy HH.mm}";

        Inisial = akun.NamaLengkap.Length > 0 ? akun.NamaLengkap[..1].ToUpperInvariant() : "?";
        NamaPengguna = $"@{akun.Username} · {akun.Email}";
        Peran = akun.LabelPeran;
        StatusLabel = akun.Status.ToString();
        Terdaftar = lokal.ToString("dd MMM HH.mm");
        Menunggu = akun.Status == StatusAkun.Menunggu;

        // Hijau = aktif, kuning = menunggu persetujuan, abu = ditolak / nonaktif.
        string warna = akun.Status switch
        {
            StatusAkun.Aktif => "5CC98A",
            StatusAkun.Menunggu => "F2A83B",
            _ => "9AA7B2"
        };
        StatusBackground = new SolidColorBrush(Color.Parse("#22" + warna));
        StatusForeground = new SolidColorBrush(Color.Parse("#" + warna));
        // Baris yang menunggu keputusan diberi latar kuning tipis supaya langsung terlihat.
        LatarBaris = new SolidColorBrush(Color.Parse(Menunggu ? "#0DF2A83B" : "#00000000"));

        bool boleh = pelaku.BolehMengelola(akun);
        BisaSetujui = boleh && akun.Status == StatusAkun.Menunggu;
        BisaNonaktifkan = boleh && akun.Status == StatusAkun.Aktif;
        BisaAktifkan = boleh && akun.Status is StatusAkun.Nonaktif or StatusAkun.Ditolak;
        BisaResetSandi = boleh && akun.Status == StatusAkun.Aktif;
        BisaJadikanAdmin = boleh && pelaku.SuperAdmin && akun.Peran == PeranOperator.Operator && akun.Status == StatusAkun.Aktif;
        BisaJadikanOperator = boleh && pelaku.SuperAdmin && akun.Peran == PeranOperator.Admin;
    }
}

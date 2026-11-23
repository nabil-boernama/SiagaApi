using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Services;

namespace SiagaApi.ViewModels;

// Semua layanan backend yang dipakai halaman-halaman aplikasi, dibuat sekali
// di MainViewModel lalu dibagikan, supaya semua halaman memakai koneksi dan
// pengaturan yang sama.
public class LayananAplikasi
{
    public KoneksiDatabase Db { get; }
    public AutentikasiService Auth { get; }
    public PenyimpanSesiLokal SesiLokal { get; }
    public PengaturanService Pengaturan { get; }
    public SinkronisasiService Sinkron { get; }
    public DasborService Dasbor { get; }
    public SimulasiService Simulasi { get; }
    public LanggananService Langganan { get; }

    // Pengaturan posko terakhir yang dibaca dari database.
    public PengaturanPosko Posko { get; private set; } = new();

    public LayananAplikasi()
    {
        Db = new KoneksiDatabase();
        Auth = new AutentikasiService(Db);
        SesiLokal = new PenyimpanSesiLokal();
        Pengaturan = new PengaturanService(Db);
        Sinkron = new SinkronisasiService(Db, Pengaturan);
        Dasbor = new DasborService(Db);
        Simulasi = new SimulasiService(Db);
        Langganan = new LanggananService(Db);
    }

    public async Task<PengaturanPosko> MuatPoskoAsync()
    {
        Posko = await Pengaturan.MuatPoskoAsync();
        StatusSumber.AturProvinsi(Posko.Provinsi);
        return Posko;
    }

    // Membaca catatan sinkronisasi terakhir dan memperbarui status di sidebar.
    public async Task PerbaruiStatusAsync()
    {
        try
        {
            StatusSumber.Perbarui(await Dasbor.AmbilSinkronTerakhirAsync(), databaseTerhubung: true);
        }
        catch (Exception)
        {
            StatusSumber.Perbarui(new Dictionary<string, CatatanSinkron>(), databaseTerhubung: false);
        }
    }
}

// Status tiga sumber data yang tampil di bilah atas tiap halaman
// (FIRMS, Cuaca, Database). Diisi dari tabel log_sinkronisasi.
public record StatusSatuSumber(string Nama, string Nilai, bool Baik);

public static class StatusSumber
{
    public static IReadOnlyList<StatusSatuSumber> Daftar { get; private set; } = new[]
    {
        new StatusSatuSumber("FIRMS", "memeriksa", true),
        new StatusSatuSumber("Cuaca", "memeriksa", true),
        new StatusSatuSumber("Database", "memeriksa", true)
    };

    // Provinsi posko, ditampilkan di samping judul halaman.
    public static string Provinsi { get; private set; } = "";

    public static bool Baik => Daftar.All(s => s.Baik);

    public static event Action? Berubah;

    public static void AturProvinsi(string provinsi)
    {
        if (Provinsi == provinsi)
            return;

        Provinsi = provinsi;
        Berubah?.Invoke();
    }

    public static void Perbarui(IReadOnlyDictionary<string, CatatanSinkron> sinkron, bool databaseTerhubung)
    {
        // Jam pengambilan terakhir (WIB), atau alasan kenapa datanya tidak bisa dipercaya.
        StatusSatuSumber Sumber(string nama)
        {
            if (!databaseTerhubung)
                return new StatusSatuSumber(nama, "tidak diketahui", false);
            if (!sinkron.TryGetValue(nama, out var catatan))
                return new StatusSatuSumber(nama, "belum diambil", false);
            if (!catatan.Berhasil)
                return new StatusSatuSumber(nama, "gagal", false);

            var wib = catatan.WaktuUtc + DasborService.Wib;
            string waktu = DateOnly.FromDateTime(wib) == DasborService.HariIniWib ? wib.ToString("HH.mm") : wib.ToString("dd MMM HH.mm");
            return new StatusSatuSumber(nama, waktu, true);
        }

        Daftar = new[]
        {
            Sumber("FIRMS"),
            Sumber("Cuaca"),
            new StatusSatuSumber("Database", databaseTerhubung ? "terhubung" : "terputus", databaseTerhubung)
        };

        Berubah?.Invoke();
    }
}

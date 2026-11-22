using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SiagaApi.Backend.Services;

namespace SiagaApi.Views.Components;

// Peta hotspot di Dashboard. Semua posisi dihitung dari koordinat asli
// (proyeksi Web Mercator), jadi batas kabupaten, titik hotspot, dan ubin
// peta dasar selalu sejajar.
//   - Mode Choropleth : kabupaten diwarnai menurut jumlah hotspot 24 jam.
//   - Mode Titik      : tiap hotspot digambar di lokasinya, warna = confidence.
//   - Peta dasar      : tanpa, peta jalan (OpenStreetMap), atau satelit (Esri).
// Peta bisa digeser dengan menyeret mouse dan diperbesar dengan roda mouse
// (zoom berpusat di posisi kursor) atau tombol + / −.
public class PetaHotspot : Canvas
{
    private const double Tepi = 26;
    // Satu "klik" roda mouse = 0,35 tingkat zoom. Trackpad mengirim banyak
    // gerakan kecil, jadi zoom-nya mengikuti besar gerakan, bukan per kejadian.
    private const double LangkahRoda = 0.35;
    private const double LangkahTrackpad = 0.10;
    private const double LangkahCubit = 2.2;
    private const double JarakSeretMin = 4;

    private static readonly PetaDasarService LayananUbin = new();

    // Ubin yang sudah pernah ditampilkan disimpan di memori, supaya saat peta
    // digeser gambarnya langsung muncul tanpa berkedip.
    private static readonly Dictionary<string, Bitmap> UbinDiMemori = new();
    private const int BatasUbinDiMemori = 400;

    // Nama pendek supaya label muat di dalam batas kabupaten.
    private static readonly Dictionary<string, string> NamaPendek = new()
    {
        ["Rokan Hulu"] = "R. Hulu",
        ["Rokan Hilir"] = "R. Hilir",
        ["Kuantan Singingi"] = "Kuansing",
        ["Indragiri Hulu"] = "Inhu",
        ["Indragiri Hilir"] = "Inhil",
        ["Kepulauan Meranti"] = "Meranti",
        ["Ogan Komering Ilir"] = "OKI"
    };

    // Setiap penggambaran ulang menaikkan angka ini; ubin yang selesai diunduh
    // untuk gambar lama diabaikan.
    private int _generasi;

    // Yang sedang ditampilkan.
    private DataDasbor? _data;
    private bool _modeTitik;
    private string _petaDasar = PengaturanPosko.TanpaPetaDasar;
    private int? _terpilihId;

    // Pandangan: titik tengah dan zoom. Null = belum diatur, pakai tampilan
    // yang memuat seluruh provinsi.
    private (double Lat, double Lon)? _pusat;
    private double? _zoom;
    private string? _provinsiPandangan;

    // Seret mouse.
    private Point? _titikTekan;
    private (double X, double Y) _pusatSaatTekan;
    private bool _sedangMenyeret;

    public event Action<int>? KabupatenDiklik;

    public PetaHotspot()
    {
        ClipToBounds = true;
        // Latar transparan supaya area kosong pun menerima seretan dan roda mouse.
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);

        SizeChanged += (_, _) => GambarUlang();
        PointerPressed += SaatDitekan;
        PointerMoved += SaatDigerakkan;
        PointerReleased += SaatDilepas;
        PointerWheelChanged += SaatRodaDiputar;
        // Cubit dua jari di trackpad (macOS).
        PointerTouchPadGestureMagnify += SaatDicubit;
    }

    public static string WarnaJumlah(int jumlah)
    {
        return jumlah switch
        {
            0 => "#283037",
            <= 4 => "#4D3A30",
            <= 9 => "#6E4630",
            <= 17 => "#914D2D",
            <= 30 => "#C6532A",
            _ => "#F15A24"
        };
    }

    public static string WarnaConfidence(TingkatConfidence tingkat)
    {
        return tingkat switch
        {
            TingkatConfidence.Tinggi => "#E7462D",
            TingkatConfidence.Nominal => "#E5A72B",
            _ => "#72C58B"
        };
    }

    public void Gambar(DataDasbor? data, bool modeTitik, string petaDasar, int? terpilihId)
    {
        _data = data;
        _modeTitik = modeTitik;
        _petaDasar = petaDasar;
        _terpilihId = terpilihId;

        // Ganti provinsi: kembali ke tampilan yang memuat seluruh provinsi.
        if (data?.Provinsi != _provinsiPandangan)
        {
            _provinsiPandangan = data?.Provinsi;
            _pusat = null;
            _zoom = null;
        }

        GambarUlang();
    }

    // Tombol + / − : perbesar atau perkecil di tengah peta.
    public void Perbesar(double langkah)
    {
        if (_data is null || Bounds.Width <= 0)
            return;

        UbahZoom(langkah, new Point(Bounds.Width / 2, Bounds.Height / 2));
    }

    // Kotak yang memuat seluruh provinsi: dari batas kabupaten dan titik tengahnya.
    private (double LatMin, double LatMaks, double LonMin, double LonMaks) KotakProvinsi()
    {
        var semuaTitik = _data!.Batas.Values.SelectMany(p => p).SelectMany(p => p).ToList();
        semuaTitik.AddRange(_data.Kabupaten.Select(k => (k.Wilayah.Lat, k.Wilayah.Lon)));
        double latMin = semuaTitik.Min(t => t.Lat), latMaks = semuaTitik.Max(t => t.Lat);
        double lonMin = semuaTitik.Min(t => t.Lon), lonMaks = semuaTitik.Max(t => t.Lon);
        if (latMaks - latMin < 0.2) { latMin -= 0.3; latMaks += 0.3; }
        if (lonMaks - lonMin < 0.2) { lonMin -= 0.3; lonMaks += 0.3; }
        return (latMin, latMaks, lonMin, lonMaks);
    }

    private double ZoomPasProvinsi()
    {
        var k = KotakProvinsi();
        return PetaDasarService.ZoomPas(k.LatMin, k.LatMaks, k.LonMin, k.LonMaks,
            Math.Max(Bounds.Width - 2 * Tepi, 50), Math.Max(Bounds.Height - 2 * Tepi, 50));
    }

    // Pandangan yang sedang berlaku (zoom, titik tengah dalam piksel dunia).
    private (double Zoom, double PusatX, double PusatY) Pandangan()
    {
        var k = KotakProvinsi();
        double zoom = _zoom ?? ZoomPasProvinsi();
        var pusat = _pusat ?? ((k.LatMin + k.LatMaks) / 2, (k.LonMin + k.LonMaks) / 2);
        var (x, y) = PetaDasarService.KePiksel(pusat.Item1, pusat.Item2, zoom);
        return (zoom, x, y);
    }

    // Zoom dengan menjaga titik di bawah kursor tetap di tempatnya.
    private void UbahZoom(double langkah, Point jangkar)
    {
        var (zoom, pusatX, pusatY) = Pandangan();
        // Boleh diperkecil sedikit melewati tampilan penuh, dan diperbesar sampai batas ubin.
        double baru = Math.Clamp(zoom + langkah, ZoomPasProvinsi() - 1.0, PetaDasarService.ZoomMaks);
        if (Math.Abs(baru - zoom) < 1e-6)
            return;

        double dx = jangkar.X - Bounds.Width / 2, dy = jangkar.Y - Bounds.Height / 2;
        var (latJangkar, lonJangkar) = PetaDasarService.DariPiksel(pusatX + dx, pusatY + dy, zoom);
        var (xBaru, yBaru) = PetaDasarService.KePiksel(latJangkar, lonJangkar, baru);

        _zoom = baru;
        _pusat = PetaDasarService.DariPiksel(xBaru - dx, yBaru - dy, baru);
        GambarUlang();
    }

    private void SaatRodaDiputar(object? sender, PointerWheelEventArgs e)
    {
        if (_data is null || e.Delta.Y == 0)
            return;

        // Roda mouse memberi langkah bulat (1, 2, ...); geseran dua jari di trackpad
        // memberi pecahan kecil berkali-kali.
        bool rodaMouse = Math.Abs(e.Delta.Y) >= 1 && Math.Abs(e.Delta.Y - Math.Round(e.Delta.Y)) < 1e-6;
        double langkah = rodaMouse
            ? Math.Sign(e.Delta.Y) * LangkahRoda
            : Math.Clamp(e.Delta.Y * LangkahTrackpad, -0.25, 0.25);

        UbahZoom(langkah, e.GetPosition(this));
        e.Handled = true;
    }

    // Delta.X = perubahan skala cubitan (positif = jari merenggang = perbesar).
    private void SaatDicubit(object? sender, PointerDeltaEventArgs e)
    {
        if (_data is null || e.Delta.X == 0)
            return;

        UbahZoom(Math.Clamp(e.Delta.X * LangkahCubit, -0.4, 0.4), e.GetPosition(this));
        e.Handled = true;
    }

    // Tombol "kembali ke tampilan awal": seluruh provinsi di tengah peta.
    public void KembaliKeAwal()
    {
        _pusat = null;
        _zoom = null;
        GambarUlang();
    }

    private void SaatDitekan(object? sender, PointerPressedEventArgs e)
    {
        if (_data is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var (_, pusatX, pusatY) = Pandangan();
        _titikTekan = e.GetPosition(this);
        _pusatSaatTekan = (pusatX, pusatY);
        _sedangMenyeret = false;
        e.Pointer.Capture(this);
    }

    private void SaatDigerakkan(object? sender, PointerEventArgs e)
    {
        if (_titikTekan is not { } awal || _data is null)
            return;

        var kini = e.GetPosition(this);
        double dx = kini.X - awal.X, dy = kini.Y - awal.Y;
        if (!_sedangMenyeret && Math.Abs(dx) < JarakSeretMin && Math.Abs(dy) < JarakSeretMin)
            return;

        _sedangMenyeret = true;
        Cursor = new Cursor(StandardCursorType.SizeAll);

        // Peta mengikuti kursor: titik tengah bergeser berlawanan arah seretan.
        var (zoom, _, _) = Pandangan();
        _zoom = zoom;
        _pusat = PetaDasarService.DariPiksel(_pusatSaatTekan.X - dx, _pusatSaatTekan.Y - dy, zoom);
        GambarUlang();
    }

    private void SaatDilepas(object? sender, PointerReleasedEventArgs e)
    {
        if (_titikTekan is null)
            return;

        bool diseret = _sedangMenyeret;
        var posisi = e.GetPosition(this);
        _titikTekan = null;
        _sedangMenyeret = false;
        e.Pointer.Capture(null);
        Cursor = new Cursor(StandardCursorType.Hand);

        // Dilepas tanpa menyeret = klik: pilih kabupaten di bawah kursor.
        if (!diseret && KabupatenDi(posisi) is { } id)
            KabupatenDiklik?.Invoke(id);
    }

    // Kabupaten di posisi layar tertentu, dicari dari koordinat aslinya.
    private int? KabupatenDi(Point posisi)
    {
        if (_data is null)
            return null;

        var (zoom, pusatX, pusatY) = Pandangan();
        var (lat, lon) = PetaDasarService.DariPiksel(
            pusatX + posisi.X - Bounds.Width / 2, pusatY + posisi.Y - Bounds.Height / 2, zoom);

        foreach (var k in _data.Kabupaten)
        {
            if (_data.Batas.TryGetValue(k.Wilayah.Id, out var poligon))
            {
                if (poligon.Any(p => Geo.DiDalam(p, lat, lon)))
                    return k.Wilayah.Id;
            }
            else
            {
                // Kabupaten tanpa garis batas digambar sebagai lingkaran beradius 17 piksel.
                var (x, y) = PetaDasarService.KePiksel(k.Wilayah.Lat, k.Wilayah.Lon, zoom);
                double jx = x - pusatX + Bounds.Width / 2 - posisi.X, jy = y - pusatY + Bounds.Height / 2 - posisi.Y;
                if (jx * jx + jy * jy <= 17 * 17)
                    return k.Wilayah.Id;
            }
        }
        return null;
    }

    private void GambarUlang()
    {
        int generasi = ++_generasi;
        Children.Clear();

        var data = _data;
        double lebar = Bounds.Width, tinggi = Bounds.Height;
        if (data is null || data.Kabupaten.Count == 0 || lebar <= 0 || tinggi <= 0)
            return;

        bool modeTitik = _modeTitik;
        string petaDasar = _petaDasar;
        int? terpilihId = _terpilihId;
        bool pakaiUbin = petaDasar != PengaturanPosko.TanpaPetaDasar;

        var (zoom, pusatX, pusatY) = Pandangan();
        double asalX = pusatX - lebar / 2, asalY = pusatY - tinggi / 2;

        Point Ke(double lat, double lon)
        {
            var (x, y) = PetaDasarService.KePiksel(lat, lon, zoom);
            return new Point(x - asalX, y - asalY);
        }

        if (pakaiUbin)
            GambarUbin(petaDasar, zoom, asalX, asalY, lebar, tinggi, generasi);

        // --- Batas kabupaten ---
        foreach (var k in data.Kabupaten)
        {
            bool dipilih = k.Wilayah.Id == terpilihId;
            string isi = modeTitik ? "#283037" : WarnaJumlah(k.Jumlah);
            double tembus = !pakaiUbin ? 1.0 : modeTitik ? 0.18 : k.Jumlah == 0 ? 0.25 : 0.62;
            string info = $"{k.Wilayah.Nama}: {k.Jumlah} hotspot dalam 24 jam" +
                          (k.Risiko is null ? "" : $" · indeks risiko {k.Risiko.Skor}/100");

            if (data.Batas.TryGetValue(k.Wilayah.Id, out var poligon))
            {
                foreach (var cincin in poligon)
                {
                    var bentuk = new Polygon
                    {
                        Points = cincin.Select(t => Ke(t.Lat, t.Lon)).ToList(),
                        Fill = new SolidColorBrush(Color.Parse(isi), tembus),
                        Stroke = new SolidColorBrush(Color.Parse(dipilih ? "#F15A24" : pakaiUbin ? "#E5E9ED" : "#14181B")),
                        StrokeThickness = dipilih ? 2.5 : pakaiUbin ? 1 : 1.5,
                        ZIndex = dipilih ? 2 : 1
                    };
                    PasangKlik(bentuk, k.Wilayah.Id, info);
                    Children.Add(bentuk);
                }
            }
            else
            {
                // Kabupaten yang belum punya garis batas: lingkaran di titik tengahnya.
                var p = Ke(k.Wilayah.Lat, k.Wilayah.Lon);
                var lingkaran = new Ellipse
                {
                    Width = 34,
                    Height = 34,
                    Fill = new SolidColorBrush(Color.Parse(isi), Math.Max(tembus, 0.6)),
                    Stroke = new SolidColorBrush(Color.Parse(dipilih ? "#F15A24" : "#95A2AD")),
                    StrokeThickness = dipilih ? 2.5 : 1,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    ZIndex = 2
                };
                SetLeft(lingkaran, p.X - 17);
                SetTop(lingkaran, p.Y - 17);
                PasangKlik(lingkaran, k.Wilayah.Id, info);
                Children.Add(lingkaran);
            }
        }

        // --- Titik hotspot (mode Titik) ---
        if (modeTitik)
        {
            foreach (var k in data.Kabupaten)
            {
                foreach (var h in k.Hotspot)
                {
                    var p = Ke(h.Titik.Lat, h.Titik.Lon);
                    var titik = new Ellipse
                    {
                        Width = 11,
                        Height = 11,
                        Fill = new SolidColorBrush(Color.Parse(WarnaConfidence(h.Tingkat))),
                        Stroke = new SolidColorBrush(Color.Parse("#14181B")),
                        StrokeThickness = 1.5,
                        ZIndex = 4
                    };
                    SetLeft(titik, p.X - 5.5);
                    SetTop(titik, p.Y - 5.5);
                    PasangKlik(titik, k.Wilayah.Id, string.Create(CultureInfo.InvariantCulture,
                        $"{k.Wilayah.Nama} · {h.Titik.Lat:0.000}, {h.Titik.Lon:0.000} · confidence {h.LabelConfidence} · {h.Titik.WaktuDeteksi + DasborService.Wib:dd MMM HH.mm} WIB"));
                    Children.Add(titik);
                }
            }
        }

        // --- Label kabupaten ---
        foreach (var k in data.Kabupaten)
        {
            var p = Ke(k.Wilayah.Lat, k.Wilayah.Lon);
            if (p.X < -40 || p.X > lebar + 40 || p.Y < -20 || p.Y > tinggi + 20)
                continue;

            string nama = NamaPendek.GetValueOrDefault(k.Wilayah.Nama, k.Wilayah.Nama);
            var label = new TextBlock
            {
                Text = modeTitik ? nama : $"{nama}\n{k.Jumlah}",
                Width = 120,
                TextAlignment = TextAlignment.Center,
                FontSize = modeTitik ? 11 : 12.5,
                FontWeight = !modeTitik && k.Jumlah > 0 ? FontWeight.SemiBold : FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse(modeTitik ? "#95A2AD" : k.Jumlah > 0 ? "#FFFFFF" : "#C3CBD2")),
                IsHitTestVisible = false,
                ZIndex = 5
            };
            if (pakaiUbin)
            {
                // Di atas foto satelit/peta jalan, teks diberi alas gelap supaya terbaca.
                label.Background = new SolidColorBrush(Color.Parse("#14181B"), 0.55);
                label.Width = double.NaN;
                label.Padding = new Thickness(5, 2);
                label.Foreground = Brushes.White;
                SetLeft(label, p.X - 28);
            }
            else
            {
                SetLeft(label, p.X - 60);
            }
            SetTop(label, p.Y - (modeTitik ? 8 : 16));
            Children.Add(label);
        }

        if (pakaiUbin)
        {
            var atribusi = new TextBlock
            {
                Text = PetaDasarService.Atribusi(petaDasar),
                FontSize = 10,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.Parse("#14181B"), 0.7),
                Padding = new Thickness(5, 2),
                IsHitTestVisible = false,
                ZIndex = 6
            };
            SetLeft(atribusi, 6);
            SetTop(atribusi, tinggi - 22);
            Children.Add(atribusi);
        }
    }

    // Klik ditangani kanvas (lihat SaatDilepas), supaya menyeret peta di atas
    // sebuah kabupaten tidak dianggap memilihnya. Bentuk hanya membawa keterangan.
    private static void PasangKlik(Control kontrol, int wilayahId, string info)
    {
        ToolTip.SetTip(kontrol, info);
    }

    // Ubin hanya tersedia pada zoom bulat. Untuk zoom pecahan dipakai ubin zoom
    // di bawahnya lalu diperbesar, supaya zoom dengan roda mouse terasa halus.
    private void GambarUbin(string jenis, double zoom, double asalX, double asalY, double lebar, double tinggi, int generasi)
    {
        int zoomUbin = (int)Math.Clamp(Math.Floor(zoom), PetaDasarService.ZoomMin, PetaDasarService.ZoomMaks);
        double u = PetaDasarService.UkuranUbin * Math.Pow(2, zoom - zoomUbin);

        for (int tx = (int)Math.Floor(asalX / u); tx * u < asalX + lebar; tx++)
        {
            for (int ty = (int)Math.Floor(asalY / u); ty * u < asalY + tinggi; ty++)
            {
                // +0,6 piksel menutup celah tipis antar ubin akibat pembulatan.
                var gambar = new Image { Width = u + 0.6, Height = u + 0.6, Stretch = Stretch.Fill, IsHitTestVisible = false, ZIndex = 0 };
                SetLeft(gambar, tx * u - asalX);
                SetTop(gambar, ty * u - asalY);
                Children.Add(gambar);

                string kunci = $"{jenis}/{zoomUbin}/{tx}/{ty}";
                if (UbinDiMemori.TryGetValue(kunci, out var tersimpan))
                    gambar.Source = tersimpan;
                else
                    _ = MuatUbinAsync(gambar, kunci, jenis, zoomUbin, tx, ty, generasi);
            }
        }
    }

    private async Task MuatUbinAsync(Image gambar, string kunci, string jenis, int zoom, int tx, int ty, int generasi)
    {
        try
        {
            byte[]? isi = await LayananUbin.AmbilUbinAsync(jenis, zoom, tx, ty);
            // Ubin tidak tersedia (internet putus): kotaknya dibiarkan kosong, peta tetap bisa dipakai.
            if (isi is null)
                return;

            if (!UbinDiMemori.TryGetValue(kunci, out var bitmap))
            {
                using var aliran = new MemoryStream(isi);
                bitmap = new Bitmap(aliran);
                if (UbinDiMemori.Count >= BatasUbinDiMemori)
                    UbinDiMemori.Clear();
                UbinDiMemori[kunci] = bitmap;
            }

            // Peta sudah digambar ulang selagi menunggu: gambar barunya yang memakai ubin ini.
            if (generasi == _generasi)
                gambar.Source = bitmap;
            else
                GambarUlang();
        }
        catch (Exception)
        {
            // Berkas ubin rusak tidak boleh menjatuhkan aplikasi; kotaknya tetap kosong.
        }
    }
}

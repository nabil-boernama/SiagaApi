using System;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using SiagaApi.Backend.Services;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class PetaSimulasiView : UserControl
{
    private PetaSimulasiViewModel? _viewModel;

    public PetaSimulasiView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) => PasangViewModel(DataContext as PetaSimulasiViewModel);
        DetachedFromVisualTree += (_, _) => PasangViewModel(null);
    }

    private void PasangViewModel(PetaSimulasiViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
            return;

        if (_viewModel is not null)
        {
            _viewModel.GridBerubah -= GambarGrid;
            _viewModel.PropertyChanged -= SaatViewModelBerubah;
            // Pindah halaman selagi diputar: hentikan pewaktunya.
            _viewModel.HentikanPutar();
        }

        _viewModel = viewModel;
        if (_viewModel is null)
            return;

        _viewModel.GridBerubah += GambarGrid;
        _viewModel.PropertyChanged += SaatViewModelBerubah;
        BuatTombolJam();
        GambarGrid();
    }

    private void SaatViewModelBerubah(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PetaSimulasiViewModel.SedangDiputar) && _viewModel is not null)
            IkonPutar.Nama = _viewModel.SedangDiputar ? "pause" : "play";
    }

    // Tujuh tombol jam (t+0 sampai t+6) di garis waktu.
    private void BuatTombolJam()
    {
        Langkah.Children.Clear();
        for (int jam = 0; jam <= SimulasiService.JamSimulasi; jam++)
        {
            int j = jam;
            var tombol = new Button
            {
                Theme = (Avalonia.Styling.ControlTheme)this.FindResource("TombolSenyap")!,
                Tag = j,
                Padding = new Thickness(6, 4),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new Ellipse { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                        new TextBlock { Text = $"t+{j}", FontSize = 11.5, FontFamily = (FontFamily)this.FindResource("HurufMono")!, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                    }
                }
            };
            tombol.Click += (_, _) => { _viewModel?.HentikanPutar(); _viewModel?.AturJam(j); };
            Langkah.Children.Add(tombol);
        }
    }

    // Jam yang sedang tampil diberi titik aksen; yang sudah lewat terang, yang belum redup.
    private void TandaiJam()
    {
        if (_viewModel is null)
            return;

        foreach (var tombol in Langkah.Children.OfType<Button>())
        {
            int jam = (int)tombol.Tag!;
            var isi = (StackPanel)tombol.Content!;
            var titik = (Ellipse)isi.Children[0];
            var teks = (TextBlock)isi.Children[1];
            bool aktif = jam == _viewModel.Jam;

            titik.Width = titik.Height = aktif ? 14 : 8;
            titik.Margin = new Thickness(0, aktif ? 0 : 3, 0, aktif ? 0 : 3);
            titik.Fill = (IBrush)this.FindResource(aktif ? "Aksen" : jam < _viewModel.Jam ? "Teks2" : "Garis")!;
            teks.Foreground = (IBrush)this.FindResource(aktif ? "Teks" : "Teks3")!;
            teks.FontWeight = aktif ? FontWeight.SemiBold : FontWeight.Normal;
        }
    }

    // Menggambar peta peluang pada jam yang dipilih di garis waktu.
    // Baris 0 grid ada di selatan, jadi digambar dari bawah: atas kanvas = utara.
    private void GambarGrid()
    {
        GridKanvas.Children.Clear();
        TandaiJam();
        var hasil = _viewModel?.Hasil;
        if (hasil is null || _viewModel is null)
        {
            Tulis("Belum ada hasil simulasi untuk ditampilkan.", 300, 300, 14, "#9AA7B2");
            return;
        }

        // Panah di keterangan angin menunjuk ke arah api terdorong.
        PanahAngin.RenderTransform = new RotateTransform(_viewModel.ArahDorong);

        const double tepi = 46;
        double lebar = GridKanvas.Width, tinggi = GridKanvas.Height;
        double sel = Math.Min(Math.Min((lebar - 2 * tepi) / hasil.Kolom, (tinggi - 2 * tepi) / hasil.Baris), 32);
        double asalX = (lebar - sel * hasil.Kolom) / 2;
        double asalY = (tinggi - sel * hasil.Baris) / 2;

        double[] sekarang = hasil.Peluang[Math.Clamp(_viewModel.Jam, 0, hasil.Jam)];
        double[] akhir = hasil.Peluang[hasil.Jam];

        for (int b = 0; b < hasil.Baris; b++)
        {
            for (int k = 0; k < hasil.Kolom; k++)
            {
                int i = b * hasil.Kolom + k;
                double p = sekarang[i];

                var kotak = new Rectangle
                {
                    Width = sel,
                    Height = sel,
                    Stroke = new SolidColorBrush(Colors.White, 0.05),
                    StrokeThickness = 1
                };

                if (p >= 0.5)
                    kotak.Fill = new SolidColorBrush(Color.Parse("#F15A24"), 0.40 + 0.55 * p);
                else if (p >= 0.1)
                    kotak.Fill = new SolidColorBrush(Color.Parse("#F2A83B"), 0.30 + 0.9 * p);
                else if (p > 0)
                    kotak.Fill = new SolidColorBrush(Color.Parse("#F2A83B"), 0.12);
                else if (akhir[i] >= 0.1)
                {
                    // Belum terjangkau pada jam ini, tapi masuk jangkauan t+6.
                    kotak.Stroke = new SolidColorBrush(Colors.White, 0.32);
                    kotak.StrokeDashArray = new AvaloniaList<double> { 3, 3 };
                }

                if (p > 0)
                    ToolTip.SetTip(kotak, $"Peluang terbakar pada t+{_viewModel.Jam}: {p:P0}");

                Canvas.SetLeft(kotak, asalX + k * sel);
                Canvas.SetTop(kotak, asalY + (hasil.Baris - 1 - b) * sel);
                GridKanvas.Children.Add(kotak);
            }
        }

        // Hotspot asli di posisi sebenarnya di dalam grid.
        foreach (var t in hasil.Klaster)
        {
            double kolom = (t.Lon - hasil.LonAsal) / hasil.UkuranSel;
            double baris = (t.Lat - hasil.LatAsal) / hasil.UkuranSel;
            var titik = new Ellipse
            {
                Width = 13,
                Height = 13,
                Fill = new SolidColorBrush(Color.Parse("#F0452E")),
                Stroke = Brushes.White,
                StrokeThickness = 1.5
            };
            ToolTip.SetTip(titik, FormattableString.Invariant(
                $"Hotspot {t.Lat:0.000}, {t.Lon:0.000} · confidence {Confidence.Label(t.Confidence)} · {t.WaktuDeteksi + DasborService.Wib:dd MMM HH.mm} WIB"));
            Canvas.SetLeft(titik, asalX + kolom * sel - 6.5);
            Canvas.SetTop(titik, asalY + (hasil.Baris - baris) * sel - 6.5);
            GridKanvas.Children.Add(titik);
        }

        // Skala: panjang 2 km di grid ini.
        double pikselPerKm = sel / (hasil.UkuranSel * 111.32);
        Garis(new Point(asalX, tinggi - 14), new Point(asalX + 2 * pikselPerKm, tinggi - 14), "#9AA7B2", 3);
        Tulis("2 km", asalX, tinggi - 36, 12, "#9AA7B2");
    }

    private void Garis(Point dari, Point ke, string warna, double tebal)
    {
        GridKanvas.Children.Add(new Line
        {
            StartPoint = dari,
            EndPoint = ke,
            Stroke = new SolidColorBrush(Color.Parse(warna)),
            StrokeThickness = tebal,
            StrokeLineCap = PenLineCap.Round
        });
    }

    private void Tulis(string teks, double x, double y, double ukuran, string warna)
    {
        var blok = new TextBlock
        {
            Text = teks,
            FontSize = ukuran,
            Foreground = new SolidColorBrush(Color.Parse(warna))
        };
        Canvas.SetLeft(blok, x);
        Canvas.SetTop(blok, y);
        GridKanvas.Children.Add(blok);
    }

    // Kegagalan ditangani di dalam ViewModel dan tampil lewat Message.
    private void Putar_Click(object? sender, RoutedEventArgs e) => _viewModel?.PutarAtauJeda();

    private async void RunSimulation_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is PetaSimulasiViewModel viewModel)
        {
            await viewModel.RunSimulationAsync();
        }
    }
}

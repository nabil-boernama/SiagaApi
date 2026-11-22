using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using SiagaApi.ViewModels;

namespace SiagaApi.Views.Components;

public partial class BilahAtasView : UserControl
{
    public static readonly StyledProperty<string> JudulProperty =
        AvaloniaProperty.Register<BilahAtasView, string>(nameof(Judul), "");

    public static readonly StyledProperty<object?> AksiProperty =
        AvaloniaProperty.Register<BilahAtasView, object?>(nameof(Aksi));

    public string Judul
    {
        get => GetValue(JudulProperty);
        set => SetValue(JudulProperty, value);
    }

    // Tombol aksi utama halaman, ditulis di XAML halaman yang memakainya.
    public object? Aksi
    {
        get => GetValue(AksiProperty);
        set => SetValue(AksiProperty, value);
    }

    public BilahAtasView()
    {
        InitializeComponent();
    }

    private void BilahAtas_Loaded(object? sender, RoutedEventArgs e)
    {
        // Status diisi sekarang, lalu ikut berubah setiap ada sinkronisasi.
        StatusSumber.Berubah -= TampilkanStatus;
        StatusSumber.Berubah += TampilkanStatus;
        TampilkanStatus();
    }

    private void BilahAtas_Unloaded(object? sender, RoutedEventArgs e)
    {
        StatusSumber.Berubah -= TampilkanStatus;
    }

    // Titik hijau = data sumber itu baik; kuning = belum diambil atau gagal.
    private void TampilkanStatus()
    {
        ProvinsiText.Text = StatusSumber.Provinsi.Length > 0 ? $"Provinsi {StatusSumber.Provinsi}" : "";
        KotakProvinsi.IsVisible = StatusSumber.Provinsi.Length > 0;

        var teks = (IBrush)this.FindResource("Teks")!;
        var teks2 = (IBrush)this.FindResource("Teks2")!;
        var baik = (IBrush)this.FindResource("Baik")!;
        var nominal = (IBrush)this.FindResource("Nominal")!;
        var mono = (FontFamily)this.FindResource("HurufMono")!;

        StatusPanel.Children.Clear();
        foreach (var sumber in StatusSumber.Daftar)
        {
            StatusPanel.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 7,
                Tag = sumber.Nama,
                Children =
                {
                    new Ellipse { Width = 7, Height = 7, Fill = sumber.Baik ? baik : nominal, VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = sumber.Nama, FontSize = 12.5, Foreground = teks2, VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = sumber.Nilai, FontSize = 12.5, FontFamily = mono, Foreground = sumber.Baik ? teks : nominal, VerticalAlignment = VerticalAlignment.Center }
                }
            });
        }
    }
}

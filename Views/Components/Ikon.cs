using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace SiagaApi.Views.Components;

// Ikon garis dari IkonData.Lucide. Pemakaian di XAML:
//   <components:Ikon Nama="flame" Ukuran="18" Warna="{StaticResource Teks2}"/>
// Ikon selalu digambar di kotak 24 x 24 lalu diskalakan, jadi semua ikon
// seukuran dan tebal garisnya ikut menyesuaikan.
public class Ikon : Viewbox
{
    public static readonly StyledProperty<string> NamaProperty =
        AvaloniaProperty.Register<Ikon, string>(nameof(Nama), "");

    public static readonly StyledProperty<double> UkuranProperty =
        AvaloniaProperty.Register<Ikon, double>(nameof(Ukuran), 18);

    public static readonly StyledProperty<IBrush?> WarnaProperty =
        AvaloniaProperty.Register<Ikon, IBrush?>(nameof(Warna), Brushes.White);

    private readonly Path _garis = new()
    {
        StrokeThickness = 2,
        StrokeLineCap = PenLineCap.Round,
        StrokeJoin = PenLineJoin.Round
    };

    public string Nama
    {
        get => GetValue(NamaProperty);
        set => SetValue(NamaProperty, value);
    }

    public double Ukuran
    {
        get => GetValue(UkuranProperty);
        set => SetValue(UkuranProperty, value);
    }

    public IBrush? Warna
    {
        get => GetValue(WarnaProperty);
        set => SetValue(WarnaProperty, value);
    }

    public Ikon()
    {
        IsHitTestVisible = false;
        Stretch = Stretch.Uniform;
        Child = new Canvas { Width = 24, Height = 24, Children = { _garis } };
        Terapkan();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NamaProperty || change.Property == UkuranProperty || change.Property == WarnaProperty)
            Terapkan();
    }

    private void Terapkan()
    {
        Width = Ukuran;
        Height = Ukuran;
        _garis.Stroke = Warna;
        // Nama ikon yang tidak dikenal menghasilkan kotak kosong, bukan error.
        _garis.Data = IkonData.Lucide.TryGetValue(Nama, out var data) ? Geometry.Parse(data) : null;
    }
}

// Tanda logo SiagaApi. Pemakaian: <components:LogoTanda Tinggi="28"/>
public class LogoTanda : Viewbox
{
    public static readonly StyledProperty<double> TinggiProperty =
        AvaloniaProperty.Register<LogoTanda, double>(nameof(Tinggi), 28);

    public double Tinggi
    {
        get => GetValue(TinggiProperty);
        set => SetValue(TinggiProperty, value);
    }

    public LogoTanda()
    {
        IsHitTestVisible = false;
        Stretch = Stretch.Uniform;

        var kanvas = new Canvas { Width = IkonData.LogoLebar, Height = IkonData.LogoTinggi };
        foreach (var (data, warna, x, y) in IkonData.Logo)
        {
            var bagian = new Path { Data = Geometry.Parse(data), Fill = new SolidColorBrush(Color.Parse(warna)) };
            Canvas.SetLeft(bagian, x);
            Canvas.SetTop(bagian, y);
            kanvas.Children.Add(bagian);
        }
        Child = kanvas;
        Terapkan();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TinggiProperty)
            Terapkan();
    }

    private void Terapkan()
    {
        Height = Tinggi;
        Width = Tinggi * IkonData.LogoLebar / IkonData.LogoTinggi;
    }
}

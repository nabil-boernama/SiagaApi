using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using SiagaApi.ViewModels;

namespace SiagaApi.Views.Components;

public partial class SidebarView : UserControl
{
    // Ikon, label di rel, dan nama halaman yang dikenal MainViewModel.NavigateToPage.
    private static readonly (string Ikon, string Label, string Halaman)[] Halaman =
    {
        ("layout-dashboard", "Dashboard", "Dashboard"),
        ("map", "Simulasi", "Peta & Simulasi"),
        ("megaphone", "Laporan", "Laporan Warga"),
        ("mail", "Langganan", "Langganan"),
        ("chart-column", "Riwayat", "Riwayat & Tren"),
        ("settings", "Atur", "Pengaturan")
    };

    public SidebarView()
    {
        InitializeComponent();
    }

    private void SidebarView_Loaded(
        object? sender,
        RoutedEventArgs e)
    {
        // Inisial dan nama operator yang sedang masuk.
        if (SesiAplikasi.Operator is { } akun)
        {
            OperatorNameText.Text = akun.NamaLengkap.Length > 0 ? akun.NamaLengkap[..1].ToUpperInvariant() : "?";
            ToolTip.SetTip(Avatar, $"{akun.LabelPeran} {akun.NamaLengkap} ({akun.Username})");
        }

        string aktif = (DataContext as INavigationViewModel)?.ActivePage ?? "";
        var teks = (IBrush)this.FindResource("Teks")!;
        var teks2 = (IBrush)this.FindResource("Teks2")!;
        var aksen = (IBrush)this.FindResource("Aksen")!;
        var permukaan2 = (IBrush)this.FindResource("Permukaan2")!;

        Menu.Children.Clear();
        foreach (var (ikon, label, halaman) in Halaman)
        {
            bool dipilih = halaman == aktif;
            var tombol = new Button
            {
                Theme = (Avalonia.Styling.ControlTheme)this.FindResource("TombolSenyap")!,
                Tag = halaman,
                Width = 64,
                Padding = new Thickness(0, 9, 0, 8),
                CornerRadius = new CornerRadius(8),
                Background = dipilih ? permukaan2 : Brushes.Transparent,
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new Ikon { Nama = ikon, Ukuran = 20, Warna = dipilih ? aksen : teks2, HorizontalAlignment = HorizontalAlignment.Center },
                        new TextBlock
                        {
                            Text = label,
                            FontSize = 10.5,
                            FontWeight = dipilih ? FontWeight.SemiBold : FontWeight.Medium,
                            Foreground = dipilih ? teks : teks2,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };
            ToolTip.SetTip(tombol, halaman);
            tombol.Click += NavigationButton_Click;
            Menu.Children.Add(tombol);
        }
    }

    private void NavigationButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is Button { Tag: string page } &&
            DataContext is INavigationViewModel navigation)
        {
            navigation.NavigateToPage(page);
        }
    }

    private void LogoutButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is INavigationViewModel navigation)
        {
            navigation.Logout();
        }
    }
}

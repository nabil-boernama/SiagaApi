using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class DashboardView : UserControl
{
    private DashboardViewModel? _viewModel;

    public DashboardView()
    {
        InitializeComponent();

        Peta.KabupatenDiklik += id => _viewModel?.PilihKabupaten(id);
        DataContextChanged += (_, _) => PasangViewModel(DataContext as DashboardViewModel);
        DetachedFromVisualTree += (_, _) => PasangViewModel(null);
    }

    // Peta digambar ulang setiap kali ViewModel memberi tahu ada yang berubah.
    private void PasangViewModel(DashboardViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
            return;

        if (_viewModel is not null)
            _viewModel.PetaBerubah -= GambarUlang;

        _viewModel = viewModel;
        if (_viewModel is null)
            return;

        _viewModel.PetaBerubah += GambarUlang;
        GambarUlang();
    }

    private void GambarUlang()
    {
        if (_viewModel is null)
            return;

        Peta.Gambar(_viewModel.Data, _viewModel.ModeTitik, _viewModel.PetaDasar, _viewModel.TerpilihId);

        // Pilihan yang aktif di tiap kelompok diberi kelas "aktif" (lihat tema TombolSegmen).
        TombolChoropleth.Classes.Set("aktif", !_viewModel.ModeTitik);
        TombolTitik.Classes.Set("aktif", _viewModel.ModeTitik);
        foreach (var tombol in PilihanPetaDasar.Children.OfType<Button>())
        {
            tombol.Classes.Set("aktif", (tombol.Tag as string) == _viewModel.PetaDasar);
        }

        LegendaJumlah.IsVisible = !_viewModel.ModeTitik;
        LegendaConfidence.IsVisible = _viewModel.ModeTitik;
    }

    // Kegagalan jaringan/database ditangani di dalam ViewModel dan tampil lewat
    // Message, jadi tidak ada exception yang lolos dari handler ini.
    private async void RefreshData_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.RefreshDataAsync();
    }

    private void ModeChoropleth_Click(object? sender, RoutedEventArgs e) => _viewModel?.AturMode(modeTitik: false);

    private void ModeTitik_Click(object? sender, RoutedEventArgs e) => _viewModel?.AturMode(modeTitik: true);

    // Tag tiap tombol berisi nilai PengaturanPosko (Tanpa peta dasar / Peta jalan / Satelit).
    private void PetaDasar_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string petaDasar })
            _viewModel?.AturPetaDasar(petaDasar);
    }

    // Zoom dan geser adalah urusan tampilan peta, jadi langsung ke komponennya.
    private void ZoomIn_Click(object? sender, RoutedEventArgs e) => Peta.Perbesar(+0.7);

    private void ZoomOut_Click(object? sender, RoutedEventArgs e) => Peta.Perbesar(-0.7);

    private void ResetPeta_Click(object? sender, RoutedEventArgs e) => Peta.KembaliKeAwal();

    private void Simulasikan_Click(object? sender, RoutedEventArgs e) => _viewModel?.SimulasikanTerpilih();
}

namespace SiagaApi.ViewModels.Components;

// Satu kartu angka di Dashboard. Nilainya berubah setiap data dimuat ulang,
// jadi tiap properti memberi tahu tampilan saat diganti.
public class StatCardViewModel : ViewModelBase
{
    private string _label = string.Empty;
    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    private string _value = string.Empty;
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    // Warna angka: netral, atau warna status kalau angkanya membawa arti (bahaya, aman).
    private Avalonia.Media.IBrush _warna = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E8EDF1"));
    public Avalonia.Media.IBrush Warna
    {
        get => _warna;
        set => SetProperty(ref _warna, value);
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
}

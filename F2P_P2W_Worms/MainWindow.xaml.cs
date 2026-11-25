using System.Windows;
using F2P_P2W_Worms.Presentation.ViewModels;

namespace F2P_P2W_Worms;

public partial class MainWindow : Window
{
    private readonly TerrainViewModel terrainViewModel = new();

    // Подключает модель представления и начальную загрузку карты
    public MainWindow()
    {
        InitializeComponent();
        DataContext = terrainViewModel;
        Loaded += OnLoaded;
    }

    // Генерирует карту после открытия окна и сообщает об ошибках конфигурации
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            await terrainViewModel.GenerateAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось загрузить карту из terrain.settings.json.\n{ex.Message}",
                "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    // Освобождает ресурсы модели представления при закрытии окна
    protected override void OnClosed(EventArgs e)
    {
        terrainViewModel.Dispose();
        base.OnClosed(e);
    }
}

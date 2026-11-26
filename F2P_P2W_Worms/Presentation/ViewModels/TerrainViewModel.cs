using System.IO;
using System.Diagnostics;
using System.Text.Json;
using F2P_P2W_Worms.Terrain;
using R3;

namespace F2P_P2W_Worms.Presentation.ViewModels;

// Загружает настройки и передаёт готовую карту представлению через R3
public sealed class TerrainViewModel : IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private bool disposed;

    public BindableReactiveProperty<TerrainFrame?> Frame { get; } = new(null);

    // Читает конфигурацию и вычисляет карту вне потока интерфейса
    public async Task GenerateAsync()
    {
        var token = lifetime.Token;
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "terrain.settings.json");
            string json = await File.ReadAllTextAsync(path, token);
            var settings = JsonSerializer.Deserialize<Settings>(json)
                ?? throw new InvalidDataException("Конфигурация карты пуста.");
            var result = await Task.Run(() =>
            {
                var terrain = new TerrainGenerator().Generate(
                    settings.Seed, settings.Width, settings.Height, settings.AnchorCount,
                    settings.Growth ?? throw new InvalidDataException("Не заданы параметры Growth."),
                    settings.Contours ?? throw new InvalidDataException("Не заданы параметры Contours."));
                var timer = Stopwatch.StartNew();
                byte[] pixels = TerrainPixelBuffer.Create(terrain, settings.ShowDebugLayers);
                timer.Stop();
                return new TerrainFrame(terrain, pixels, settings.ShowDebugLayers, timer.Elapsed);
            }, token);
            if (!disposed) Frame.Value = result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    // Отменяет загрузку и освобождает реактивное свойство при закрытии окна
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        Frame.Dispose();
        lifetime.Dispose();
    }

    // Описывает поля файла terrain.settings.json
    private sealed class Settings
    {
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required int Seed { get; init; }
        public required int AnchorCount { get; init; }
        public required TerrainGrowthSettings Growth { get; init; }
        public required TerrainContourSettings Contours { get; init; }
        public bool ShowDebugLayers { get; init; } = true;
    }
}

using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using F2P_P2W_Worms.Terrain;

namespace F2P_P2W_Worms.Presentation.Views;

// Показывает большую маску земли с отладочными слоями каркаса и сетки
public sealed class TerrainDebugSurface : FrameworkElement
{
    private static readonly Brush BackgroundBrush = FrozenBrush("#595959");
    private static readonly Pen GridPen = FrozenPen(FrozenBrush("#333333"), 0.5);
    private static readonly Pen EdgePen = FrozenPen(Brushes.Black, 1);
    private BitmapSource? terrainImage;

    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame), typeof(TerrainFrame), typeof(TerrainDebugSurface),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnFrameChanged));

    public TerrainFrame? Frame
    {
        get => (TerrainFrame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    // Ограничивает рисунок областью элемента и сохраняет чёткость растровой маски
    public TerrainDebugSurface()
    {
        ClipToBounds = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    // Обновляет размеры отладочных клеток при изменении области окна
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateVisual();
    }

    // Рисует готовую большую маску и необязательные линии отладочной сетки
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(BackgroundBrush, null, new Rect(RenderSize));
        if (Frame is not { } frame || terrainImage is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        dc.DrawImage(terrainImage, new Rect(RenderSize));
        if (!frame.ShowDebugLayers) return;

        var terrain = frame.Terrain;
        double cellWidth = ActualWidth / terrain.Width;
        double cellHeight = ActualHeight / terrain.Height;
        for (int x = 0; x <= terrain.Width; x++)
            dc.DrawLine(GridPen, new Point(x * cellWidth, 0), new Point(x * cellWidth, ActualHeight));
        for (int y = 0; y <= terrain.Height; y++)
            dc.DrawLine(GridPen, new Point(0, y * cellHeight), new Point(ActualWidth, y * cellHeight));

        // Переводит координаты опоры в центр экранной клетки
        Point Center(GridPoint point) => new((point.X + 0.5) * cellWidth, (point.Y + 0.5) * cellHeight);
        foreach (var edge in terrain.Edges)
            dc.DrawLine(EdgePen, Center(terrain.Anchors[edge.StartIndex]), Center(terrain.Anchors[edge.EndIndex]));
    }

    // Создаёт WPF-изображение в UI-потоке из подготовленного в фоне массива
    private static void OnFrameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var surface = (TerrainDebugSurface)sender;
        surface.Dispatcher.VerifyAccess();
        surface.terrainImage = null;
        if (args.NewValue is not TerrainFrame frame) return;
        var filled = frame.Terrain.FilledMap!;
        var timer = Stopwatch.StartNew();
        var image = BitmapSource.Create(filled.Width, filled.Height, 96, 96,
            PixelFormats.Bgra32, null, frame.Pixels, checked(filled.Width * 4));
        image.Freeze();
        surface.terrainImage = image;
        timer.Stop();
        Trace.WriteLine($"Terrain: fill {filled.Duration.TotalMilliseconds:F3} ms; buffer {frame.BufferDuration.TotalMilliseconds:F3} ms; WPF image {timer.Elapsed.TotalMilliseconds:F3} ms");
    }

    // Создаёт неизменяемую кисть для повторного использования при отрисовке
    private static Brush FrozenBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    // Создаёт неизменяемое перо для повторной отрисовки
    private static Pen FrozenPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        pen.Freeze();
        return pen;
    }
}

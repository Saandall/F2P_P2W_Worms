using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using F2P_P2W_Worms.Terrain;

namespace F2P_P2W_Worms.Presentation.Views;

// Показывает землю, исходный каркас и контуры в обозначениях статьи
public sealed class TerrainDebugSurface : FrameworkElement
{
    private static readonly Brush BackgroundBrush = FrozenBrush("#595959");
    private static readonly Brush LandBrush = FrozenBrush("#4E9D3C");
    private static readonly Brush SkeletonBrush = FrozenBrush("#00A1B5");
    private static readonly Brush AnchorBrush = FrozenBrush("#087FA3");
    private static readonly Pen GridPen = FrozenPen(FrozenBrush("#333333"), 0.5);
    private static readonly Pen EdgePen = FrozenPen(Brushes.Black, 1);
    private BitmapSource? contourImage;
    private bool[,]? skeleton;

    public static readonly DependencyProperty TerrainProperty = DependencyProperty.Register(
        nameof(Terrain), typeof(TerrainModel), typeof(TerrainDebugSurface),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnTerrainChanged));

    public TerrainModel? Terrain
    {
        get => (TerrainModel?)GetValue(TerrainProperty);
        set => SetValue(TerrainProperty, value);
    }

    // Ограничивает рисунок областью элемента и сохраняет чёткость растрового контура
    public TerrainDebugSurface()
    {
        ClipToBounds = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    // Обновляет размеры клеток при изменении области окна
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateVisual();
    }

    // Рисует слои малой сетки и накладывает большой контур с прозрачным фоном
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(BackgroundBrush, null, new Rect(RenderSize));
        if (Terrain is not { } terrain || ActualWidth <= 0 || ActualHeight <= 0) return;

        double cellWidth = ActualWidth / terrain.Width;
        double cellHeight = ActualHeight / terrain.Height;
        for (int y = 0; y < terrain.Height; y++)
            for (int x = 0; x < terrain.Width; x++)
            {
                if (!terrain.IsSolid(x, y)) continue;
                Brush fill = skeleton is not null && skeleton[x, y] ? SkeletonBrush : LandBrush;
                dc.DrawRectangle(fill, null, new Rect(x * cellWidth, y * cellHeight, cellWidth, cellHeight));
            }

        foreach (var point in terrain.Anchors)
            dc.DrawRectangle(AnchorBrush, null,
                new Rect(point.X * cellWidth, point.Y * cellHeight, cellWidth, cellHeight));

        for (int x = 0; x <= terrain.Width; x++)
            dc.DrawLine(GridPen, new Point(x * cellWidth, 0), new Point(x * cellWidth, ActualHeight));
        for (int y = 0; y <= terrain.Height; y++)
            dc.DrawLine(GridPen, new Point(0, y * cellHeight), new Point(ActualWidth, y * cellHeight));

        // Переводит координаты опоры в центр экранной клетки
        Point Center(GridPoint p) => new((p.X + 0.5) * cellWidth, (p.Y + 0.5) * cellHeight);
        foreach (var edge in terrain.Edges)
            dc.DrawLine(EdgePen, Center(terrain.Anchors[edge.StartIndex]), Center(terrain.Anchors[edge.EndIndex]));
        if (contourImage is not null) dc.DrawImage(contourImage, new Rect(RenderSize));
    }

    // Подготавливает неизменяемые отладочные слои при получении готовой карты
    private static void OnTerrainChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var surface = (TerrainDebugSurface)sender;
        surface.skeleton = null;
        surface.contourImage = null;
        if (args.NewValue is not TerrainModel terrain) return;

        // Восстанавливает изображение исходного каркаса из сохранённых опор и рёбер
        var mask = new bool[terrain.Width, terrain.Height];
        foreach (var point in terrain.Anchors) mask[point.X, point.Y] = true;
        foreach (var edge in terrain.Edges)
            TerrainGenerator.RasterizeLine(mask, terrain.Anchors[edge.StartIndex], terrain.Anchors[edge.EndIndex]);
        surface.skeleton = mask;
        if (terrain.ContourMap is { } map) surface.contourImage = CreateContourImage(map);
    }

    // Создаёт белые линии контура на прозрачном фоне без заполнения внутренностей
    private static BitmapSource CreateContourImage(TerrainContourMap map)
    {
        int stride = checked(map.Width * 4);
        var pixels = new byte[checked(stride * map.Height)];
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (!map.IsOutline(x, y)) continue;
                int index = y * stride + x * 4;
                pixels[index] = pixels[index + 1] = pixels[index + 2] = pixels[index + 3] = 255;
            }
        var image = BitmapSource.Create(map.Width, map.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        image.Freeze();
        return image;
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

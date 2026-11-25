namespace F2P_P2W_Worms.Terrain;

// Создаёт общие углы, сглаживает контуры и растеризует их в большую маску
public static class TerrainContourBuilder
{
    // Выполняет пункты 9-12 со смещением из пункта 13 при расчёте углов
    public static TerrainContourMap Build(bool[,] mask, TerrainContourSettings settings, Random random)
    {
        ArgumentNullException.ThrowIfNull(mask);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(random);
        settings.Validate();
        int width = mask.GetLength(0), height = mask.GetLength(1);
        ValidateBorder(mask);
        var outline = new bool[checked(width * settings.UpscaleFactor), checked(height * settings.UpscaleFactor)];
        GridPoint[,] corners = CreateSharedCorners(width, height, settings, random);
        var contours = new List<TerrainContour>();

        foreach (var loop in PavlidisContourTracer.Trace(mask))
        {
            GridPoint[] source = loop.Select(p => corners[p.X, p.Y]).ToArray();
            ContourPoint[] curve = ChaikinSmoother.Smooth(source, settings.SmoothingIterations, settings.CutRatio);
            GridPoint[] points = RoundToPixels(curve);
            contours.Add(new TerrainContour(points, SignedArea(loop) < 0));
            for (int i = 0; i < points.Length; i++)
                TerrainGenerator.RasterizeLine(outline, points[i], points[(i + 1) % points.Length]);
        }
        return new TerrainContourMap(corners, contours.ToArray(), outline);
    }

    // Округляет только готовую кривую и удаляет соседние повторения пикселей
    private static GridPoint[] RoundToPixels(IReadOnlyList<ContourPoint> curve)
    {
        var points = new List<GridPoint>(curve.Count);
        foreach (var point in curve)
        {
            var rounded = new GridPoint((int)Math.Round(point.X, MidpointRounding.AwayFromZero),
                (int)Math.Round(point.Y, MidpointRounding.AwayFromZero));
            if (points.Count == 0 || points[^1] != rounded) points.Add(rounded);
        }
        if (points.Count > 1 && points[^1] == points[0]) points.RemoveAt(points.Count - 1);
        return points.ToArray();
    }

    // Вычисляет каждую общую вершину один раз с безопасным целочисленным сдвигом
    private static GridPoint[,] CreateSharedCorners(int width, int height, TerrainContourSettings settings, Random random)
    {
        var corners = new GridPoint[width + 1, height + 1];
        int scale = settings.UpscaleFactor;
        int offset = (int)Math.Floor((scale - 1) * settings.CornerOffset / 4);
        for (int y = 0; y <= height; y++)
            for (int x = 0; x <= width; x++)
            {
                int dx = offset == 0 ? 0 : random.Next(-offset, offset + 1);
                int dy = offset == 0 ? 0 : random.Next(-offset, offset + 1);
                corners[x, y] = new GridPoint(checked(x * scale + dx), checked(y * scale + dy));
            }
        return corners;
    }

    // Определяет ориентацию контура, отрицательная площадь соответствует отверстию
    private static long SignedArea(IReadOnlyList<GridPoint> points)
    {
        long area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            area += (long)a.X * b.Y - (long)b.X * a.Y;
        }
        return area;
    }

    // Проверяет пустую рамку, необходимую для размещения увеличенных контуров
    private static void ValidateBorder(bool[,] mask)
    {
        int width = mask.GetLength(0), height = mask.GetLength(1);
        if (width < 3 || height < 3) throw new ArgumentException("Сетка должна быть не меньше 3 на 3.", nameof(mask));
        for (int x = 0; x < width; x++)
            if (mask[x, 0] || mask[x, height - 1]) throw new ArgumentException("Внешняя рамка должна быть пустой.", nameof(mask));
        for (int y = 0; y < height; y++)
            if (mask[0, y] || mask[width - 1, y]) throw new ArgumentException("Внешняя рамка должна быть пустой.", nameof(mask));
    }
}

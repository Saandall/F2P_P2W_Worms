using System.Diagnostics;

namespace F2P_P2W_Worms.Terrain;

// Большая маска земли и результаты последовательной заливки
public sealed class TerrainFillResult
{
    private readonly bool[,] solid;
    public int Width => solid.GetLength(0);
    public int Height => solid.GetLength(1);
    public int RegionCount { get; }
    public int SolidPixelCount { get; }
    public TimeSpan Duration { get; }

    // Сохраняет готовую маску, число классифицированных областей и время заливки
    internal TerrainFillResult(bool[,] solid, int regions, int solidPixels, TimeSpan duration)
    {
        this.solid = solid;
        RegionCount = regions;
        SolidPixelCount = solidPixels;
        Duration = duration;
    }

    // Проверяет пиксель большой маски, за её пределами возвращает пустоту
    public bool IsSolid(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height && solid[x, y];
}

// Классифицирует области лучом и заполняет их последовательным BFS
public static class TerrainFill
{
    // Использует все растровые контуры как общий непроходимый барьер
    public static TerrainFillResult Fill(TerrainContourMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var timer = Stopwatch.StartNew();
        int width = map.Width, height = map.Height;
        _ = checked(width * height);
        var solid = new bool[width, height];
        var visited = new bool[width, height];
        var queue = new Queue<int>();
        int regions = 0, solidPixels = 0;

        // Пиксели всех границ считаются землёй до обхода областей
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (map.IsOutline(x, y))
                {
                    solid[x, y] = true;
                    solidPixels++;
                }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (visited[x, y] || map.IsOutline(x, y)) continue;
                // Геометрия проверяется только для первой клетки новой области
                bool material = IsInside(map.Contours, x, y);
                regions++;
                Enqueue(x, y, material);
                while (queue.TryDequeue(out int index))
                {
                    int px = index % width, py = index / width;
                    Enqueue(px - 1, py, material);
                    Enqueue(px + 1, py, material);
                    Enqueue(px, py - 1, material);
                    Enqueue(px, py + 1, material);
                }
            }
        timer.Stop();
        return new TerrainFillResult(solid, regions, solidPixels, timer.Elapsed);

        // Помечает пиксель при постановке в очередь и назначает материал области
        void Enqueue(int x, int y, bool material)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || map.IsOutline(x, y)) return;
            visited[x, y] = true;
            solid[x, y] = material;
            if (material) solidPixels++;
            queue.Enqueue(y * width + x);
        }
    }

    // Определяет чётность пересечений горизонтального луча со всеми контурами вместе
    public static bool IsInside(IReadOnlyList<TerrainContour> contours, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(contours);
        int crossings = 0;
        foreach (var contour in contours)
        {
            var points = contour.Points;
            for (int i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];
                // Полуоткрытый диапазон Y исключает горизонтали и двойной учёт вершины
                if ((a.Y > y) == (b.Y > y)) continue;
                double intersectionX = a.X + (double)(y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                if (intersectionX > x) crossings++;
            }
        }
        return (crossings & 1) != 0;
    }
}

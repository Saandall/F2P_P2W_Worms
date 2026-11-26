namespace F2P_P2W_Worms.Terrain;

// Генерирует каркас и наращивает землю на малой сетке
public sealed class TerrainGenerator
{
    // Создаёт карту по seed, без параметров роста возвращает только каркас
    public TerrainModel Generate(int seed, int width = 40, int height = 20, int anchorCount = 16,
        TerrainGrowthSettings? growth = null, TerrainContourSettings? contours = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 3);
        int capacity = checked((width - 2) * (height - 2));
        if (anchorCount < 1 || anchorCount > capacity)
            throw new ArgumentOutOfRangeException(nameof(anchorCount));
        growth?.Validate(width);
        contours?.Validate();

        var anchors = new List<GridPoint>(anchorCount);
        // Две нижние опоры и случайные точки задают начальный каркас
        var fixedPoints = new[]
        {
            new GridPoint(Math.Clamp(width / 4, 1, width - 2), Math.Clamp(height * 2 / 3, 1, height - 2)),
            new GridPoint(Math.Clamp(width * 3 / 4, 1, width - 2), Math.Clamp(height * 2 / 3, 1, height - 2))
        };
        foreach (var point in fixedPoints)
            if (anchors.Count < anchorCount && !anchors.Contains(point)) anchors.Add(point);

        var available = new List<GridPoint>(capacity - anchors.Count);
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                var point = new GridPoint(x, y);
                if (!anchors.Contains(point)) available.Add(point);
            }
        var random = new Random(seed);
        // Выбор без возвращения, без повторных попыток при почти полной сетке
        while (anchors.Count < anchorCount)
        {
            int index = random.Next(available.Count);
            anchors.Add(available[index]);
            available[index] = available[^1];
            available.RemoveAt(available.Count - 1);
        }

        GridPoint[] points = anchors.ToArray();
        TerrainEdge[] edges = BuildMinimumSpanningTree(points);
        var mask = new bool[width, height];
        foreach (var point in points) mask[point.X, point.Y] = true;
        foreach (var edge in edges) RasterizeLine(mask, points[edge.StartIndex], points[edge.EndIndex]);
        if (growth is not null) TerrainGrowth.Apply(mask, growth, random);
        var contourMap = contours is null ? null : TerrainContourBuilder.Build(mask, contours, random);
        var filledMap = contourMap is null ? null : TerrainFill.Fill(contourMap);
        return new TerrainModel(seed, mask, points, edges, contourMap, filledMap);
    }

    // Строит минимальное остовное дерево Прима по расстояниям между опорами
    public static TerrainEdge[] BuildMinimumSpanningTree(IReadOnlyList<GridPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Distinct().Count() != points.Count)
            throw new ArgumentException("Опорные точки должны быть уникальными.", nameof(points));
        if (points.Count < 2) return [];
        int count = points.Count;
        var visited = new bool[count];
        var distances = Enumerable.Repeat(double.PositiveInfinity, count).ToArray();
        var parents = new int[count];
        var edges = new List<TerrainEdge>(count - 1);
        distances[0] = 0;
        for (int step = 0; step < count; step++)
        {
            int current = -1;
            for (int i = 0; i < count; i++)
                if (!visited[i] && (current < 0 || distances[i] < distances[current])) current = i;
            visited[current] = true;
            if (current != 0) edges.Add(new TerrainEdge(parents[current], current));
            for (int i = 0; i < count; i++)
            {
                double dx = (double)points[current].X - points[i].X;
                double dy = (double)points[current].Y - points[i].Y;
                double squaredDistance = dx * dx + dy * dy;
                // Квадрат расстояния сохраняет порядок евклидовых весов
                // При равных весах сохраняет первого родителя и выбирает вершины по индексу
                if (!visited[i] && squaredDistance < distances[i])
                {
                    distances[i] = squaredDistance;
                    parents[i] = current;
                }
            }
        }
        return edges.ToArray();
    }

    // Рисует одноклеточную линию Брезенхема с обоими концами во всех направлениях
    public static void RasterizeLine(bool[,] mask, GridPoint start, GridPoint end)
    {
        ArgumentNullException.ThrowIfNull(mask);
        ValidateEndpoint(start, mask);
        ValidateEndpoint(end, mask);
        // Одинаковое разрешение ничьих при перестановке концов отрезка
        if (start.X > end.X || (start.X == end.X && start.Y > end.Y)) (start, end) = (end, start);
        int x = start.X, y = start.Y;
        long dx = Math.Abs((long)end.X - x), dy = -Math.Abs((long)end.Y - y);
        int sx = x < end.X ? 1 : -1, sy = y < end.Y ? 1 : -1;
        long error = dx + dy;
        while (true)
        {
            mask[x, y] = true;
            if (x == end.X && y == end.Y) break;
            long twiceError = 2 * error;
            if (twiceError >= dy) { error += dy; x += sx; }
            if (twiceError <= dx) { error += dx; y += sy; }
        }
    }

    // Проверяет, что конец отрезка находится внутри маски
    private static void ValidateEndpoint(GridPoint point, bool[,] mask)
    {
        if (point.X < 0 || point.X >= mask.GetLength(0) || point.Y < 0 || point.Y >= mask.GetLength(1))
            throw new ArgumentOutOfRangeException(nameof(point), "Конец линии находится вне сетки.");
    }
}

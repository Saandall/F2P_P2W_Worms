namespace F2P_P2W_Worms.Terrain;

// Обходит границы Павлидисом и переводит граничные пиксели в углы клеток
public static class PavlidisContourTracer
{
    // Четыре отсчёта на клетку отделяют середины сторон от поворотов границы
    private const int Subdivision = 4;
    private static readonly GridPoint[] Directions = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];
    private readonly record struct State(int X, int Y, int Direction);

    // Находит внешние и внутренние контуры всех заполненных областей
    public static IReadOnlyList<GridPoint[]> Trace(bool[,] mask)
    {
        ArgumentNullException.ThrowIfNull(mask);
        int width = mask.GetLength(0), height = mask.GetLength(1);
        var visited = new HashSet<State>();
        var result = new List<GridPoint[]>();

        // Проверяет клетку исходной маски с пустотой за её границами
        bool Solid(int x, int y) => x >= 0 && x < width && y >= 0 && y < height && mask[x, y];

        // Читает вспомогательный растр без выделения увеличенного массива
        bool Sample(int x, int y) => x >= 0 && y >= 0 &&
            x < (long)width * Subdivision && y < (long)height * Subdivision &&
            Solid(x / Subdivision, y / Subdivision);

        for (int y = height - 1; y >= 0; y--)
            for (int x = 0; x < width; x++)
            {
                if (!mask[x, y]) continue;
                for (int side = 0; side < 4; side++)
                {
                    var outward = Directions[side];
                    if (Solid(x + outward.X, y + outward.Y)) continue;
                    State start = SideStart(x, y, side);
                    if (visited.Contains(start)) continue;
                    SplitAtSharedCorners(TraceLoop(start, Sample, visited), result);
                }
            }
        return result;
    }

    // Выбирает середину открытой стороны, оставляя пустоту слева от направления обхода
    private static State SideStart(int x, int y, int side)
    {
        int left = checked(x * Subdivision), top = checked(y * Subdivision);
        return side switch
        {
            0 => new State(left + 2, top, 1),
            1 => new State(left + 3, top + 2, 2),
            2 => new State(left + 2, top + 3, 3),
            _ => new State(left, top + 2, 0)
        };
    }

    // Выполняет проверки P1, P2, P3 и повороты до возврата в исходное состояние
    private static List<GridPoint> TraceLoop(State start, Func<int, int, bool> solid, HashSet<State> visited)
    {
        var points = new List<GridPoint>();
        var local = new HashSet<State>();
        State current = start;
        while (local.Add(current))
        {
            visited.Add(current);
            // Округляет центр граничного отсчёта до угла исходной клетки
            var corner = new GridPoint((2 * current.X + 1 + Subdivision) / (2 * Subdivision),
                (2 * current.Y + 1 + Subdivision) / (2 * Subdivision));
            if (points.Count == 0 || points[^1] != corner) points.Add(corner);

            var forward = Directions[current.Direction];
            int leftDirection = (current.Direction + 3) % 4;
            var left = Directions[leftDirection];
            int frontX = current.X + forward.X, frontY = current.Y + forward.Y;
            if (solid(frontX + left.X, frontY + left.Y))
                current = new State(frontX + left.X, frontY + left.Y, leftDirection);
            else if (solid(frontX, frontY))
                current = new State(frontX, frontY, current.Direction);
            else if (solid(frontX - left.X, frontY - left.Y))
                current = new State(frontX - left.X, frontY - left.Y, current.Direction);
            else
                current = current with { Direction = (current.Direction + 1) % 4 };
        }
        // Направление включено в условие замыкания, поэтому поворот на месте не завершает обход
        if (current != start) throw new InvalidOperationException("Обход Павлидиса не вернулся к начальной стороне.");
        if (points.Count > 1 && points[^1] == points[0]) points.RemoveAt(points.Count - 1);
        return points;
    }

    // Разделяет касающиеся в одной вершине границы на отдельные замкнутые петли
    private static void SplitAtSharedCorners(List<GridPoint> points, List<GridPoint[]> result)
    {
        var path = new List<GridPoint>();
        var positions = new Dictionary<GridPoint, int>();
        foreach (var point in points.Append(points[0]))
        {
            if (!positions.TryGetValue(point, out int index))
            {
                positions.Add(point, path.Count);
                path.Add(point);
                continue;
            }
            GridPoint[] loop = path.Skip(index).ToArray();
            if (loop.Length < 4) throw new InvalidOperationException("Контур содержит вырожденную петлю.");
            result.Add(loop);
            for (int i = path.Count - 1; i > index; i--) positions.Remove(path[i]);
            path.RemoveRange(index + 1, path.Count - index - 1);
        }
    }
}

namespace F2P_P2W_Worms.Terrain;

// Дробные координаты вершины при последовательном сглаживании
public readonly record struct ContourPoint(double X, double Y);

// Срезает углы замкнутого контура алгоритмом Чайкина
public static class ChaikinSmoother
{
    // Удваивает число вершин на каждой итерации без промежуточного округления
    public static ContourPoint[] Smooth(IReadOnlyList<GridPoint> points, int iterations, double cutRatio)
    {
        ArgumentNullException.ThrowIfNull(points);
        ValidateParameters(iterations, cutRatio);
        if (points.Count < 3) throw new ArgumentException("Для замкнутого контура нужны хотя бы три вершины.", nameof(points));

        ContourPoint[] current = points.Select(p => new ContourPoint(p.X, p.Y)).ToArray();
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            var next = new ContourPoint[checked(current.Length * 2)];
            for (int i = 0; i < current.Length; i++)
            {
                var a = current[i];
                var b = current[(i + 1) % current.Length];
                // Две точки на каждом ребре сохраняют порядок обхода и замыкание
                next[i * 2] = Interpolate(a, b, cutRatio);
                next[i * 2 + 1] = Interpolate(b, a, cutRatio);
            }
            current = next;
        }
        return current;
    }

    // Ограничивает число итераций и запрещает пересечение точек за серединой ребра
    internal static void ValidateParameters(int iterations, double cutRatio)
    {
        if (iterations < 0 || iterations > 8) throw new ArgumentOutOfRangeException(nameof(iterations));
        if (!double.IsFinite(cutRatio) || cutRatio <= 0 || cutRatio >= 0.5)
            throw new ArgumentOutOfRangeException(nameof(cutRatio), "Доля среза должна быть больше 0 и меньше 0.5.");
    }

    // Находит точку на заданной доле отрезка между двумя вершинами
    private static ContourPoint Interpolate(ContourPoint a, ContourPoint b, double ratio) =>
        new(a.X + (b.X - a.X) * ratio, a.Y + (b.Y - a.Y) * ratio);
}

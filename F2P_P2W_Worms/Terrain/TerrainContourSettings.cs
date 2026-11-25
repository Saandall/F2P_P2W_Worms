namespace F2P_P2W_Worms.Terrain;

// Параметры увеличения, смещения углов и сглаживания контуров
public sealed class TerrainContourSettings
{
    // Число пикселей большой маски на одну клетку малой сетки
    public int UpscaleFactor { get; init; } = 30;

    // Сила смещения от 0 до 1, максимальный сдвиг меньше четверти клетки
    public double CornerOffset { get; init; } = 0.6;

    // Количество итераций Чайкина, ноль сохраняет исходный контур
    public int SmoothingIterations { get; init; } = 3;

    // Доля среза каждого ребра с обеих сторон, стандартное значение равно четверти
    public double CutRatio { get; init; } = 0.25;

    // Проверяет масштаб, смещение и параметры сглаживания
    internal void Validate()
    {
        if (UpscaleFactor < 1) throw new ArgumentOutOfRangeException(nameof(UpscaleFactor));
        if (!double.IsFinite(CornerOffset) || CornerOffset < 0 || CornerOffset > 1)
            throw new ArgumentOutOfRangeException(nameof(CornerOffset));
        ChaikinSmoother.ValidateParameters(SmoothingIterations, CutRatio);
    }
}

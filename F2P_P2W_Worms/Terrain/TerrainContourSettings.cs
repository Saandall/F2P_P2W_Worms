namespace F2P_P2W_Worms.Terrain;

// Параметры увеличения и смещения общих углов малой сетки
public sealed class TerrainContourSettings
{
    // Число пикселей большой маски на одну клетку малой сетки
    public int UpscaleFactor { get; init; } = 30;

    // Сила смещения от 0 до 1, максимальный сдвиг меньше четверти клетки
    public double CornerOffset { get; init; } = 0.6;

    // Проверяет масштаб и нормализованную силу смещения
    internal void Validate()
    {
        if (UpscaleFactor < 1) throw new ArgumentOutOfRangeException(nameof(UpscaleFactor));
        if (!double.IsFinite(CornerOffset) || CornerOffset < 0 || CornerOffset > 1)
            throw new ArgumentOutOfRangeException(nameof(CornerOffset));
    }
}

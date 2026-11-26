namespace F2P_P2W_Worms.Terrain;

// Параметры роста на малой сетке, вероятности и размеры задаются долями от 0 до 1
public sealed class TerrainGrowthSettings
{
    // Количество полных итераций роста
    public int Iterations { get; init; } = 4;

    // Доля высоты верхней области, отсчитываемая от верхнего края
    public double UpperRegionHeight { get; init; } = 0.45;

    // Вероятность роста в верхней области при двух и более соседях
    public double UpperRegionGrowthChance { get; init; } = 0.5;

    // Максимальная вероятность роста при одном соседе, убывающая к верхнему краю
    public double SingleNeighborChance { get; init; } = 0.2;

    // Количество различных стартовых клеток в предпоследней строке
    public int BottomSeedCount { get; init; } = 3;

    // Размеры одного нижнего блока относительно внутренней области сетки
    public double BottomChunkWidth { get; init; } = 0.3;
    public double BottomChunkHeight { get; init; } = 0.12;

    // Проверяет параметры до изменения маски
    internal void Validate(int width)
    {
        if (Iterations < 0) throw new ArgumentOutOfRangeException(nameof(Iterations));
        if (BottomSeedCount < 0 || BottomSeedCount > width - 2)
            throw new ArgumentOutOfRangeException(nameof(BottomSeedCount));
        ValidateFraction(UpperRegionHeight, nameof(UpperRegionHeight));
        ValidateFraction(UpperRegionGrowthChance, nameof(UpperRegionGrowthChance));
        ValidateFraction(SingleNeighborChance, nameof(SingleNeighborChance));
        ValidateFraction(BottomChunkWidth, nameof(BottomChunkWidth));
        ValidateFraction(BottomChunkHeight, nameof(BottomChunkHeight));
    }

    // Отклоняет значения вне диапазона и нечисловые значения
    private static void ValidateFraction(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0 || value > 1)
            throw new ArgumentOutOfRangeException(name, "Значение должно находиться между 0 и 1.");
    }
}

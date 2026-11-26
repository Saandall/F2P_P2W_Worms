namespace F2P_P2W_Worms.Terrain;

// Целочисленные координаты клетки
public readonly record struct GridPoint(int X, int Y);
// Индексы соединённых опор в списке точек
public readonly record struct TerrainEdge(int StartIndex, int EndIndex);

// Хранит маску земли, исходные опоры и рёбра каркаса
public sealed class TerrainModel
{
    private readonly bool[,] solid;
    public int Width => solid.GetLength(0);
    public int Height => solid.GetLength(1);
    public int Seed { get; }
    public IReadOnlyList<GridPoint> Anchors { get; }
    public IReadOnlyList<TerrainEdge> Edges { get; }
    public int SolidCellCount { get; }
    public TerrainContourMap? ContourMap { get; }
    public TerrainFillResult? FilledMap { get; }

    // Сохраняет результат генерации и подсчитывает заполненные клетки
    internal TerrainModel(int seed, bool[,] mask, GridPoint[] anchors, TerrainEdge[] edges,
        TerrainContourMap? contourMap = null, TerrainFillResult? filledMap = null)
    {
        Seed = seed;
        ContourMap = contourMap;
        FilledMap = filledMap;
        solid = mask;
        Anchors = Array.AsReadOnly(anchors);
        Edges = Array.AsReadOnly(edges);
        foreach (bool value in solid)
            if (value) SolidCellCount++;
    }

    // Проверяет клетку малой сетки, а за её пределами возвращает пустоту
    public bool IsSolid(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height && solid[x, y];
}

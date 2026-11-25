namespace F2P_P2W_Worms.Terrain;

// Замкнутый контур без повторения первой вершины в конце списка
public sealed class TerrainContour
{
    public IReadOnlyList<GridPoint> Points { get; }
    public bool IsHole { get; }

    // Сохраняет вершины и признак внутренней границы
    internal TerrainContour(GridPoint[] points, bool isHole)
    {
        Points = Array.AsReadOnly(points);
        IsHole = isHole;
    }
}

// Хранит общие углы, замкнутые контуры и большую маску их линий
public sealed class TerrainContourMap
{
    private readonly GridPoint[,] corners;
    private readonly bool[,] outline;
    public int Width => outline.GetLength(0);
    public int Height => outline.GetLength(1);
    public IReadOnlyList<TerrainContour> Contours { get; }

    // Сохраняет результат построения без заполнения внутренностей
    internal TerrainContourMap(GridPoint[,] corners, TerrainContour[] contours, bool[,] outline)
    {
        this.corners = corners;
        this.outline = outline;
        Contours = Array.AsReadOnly(contours);
    }

    // Возвращает четыре угла клетки из общего массива вершин
    public GridPoint[] GetCellCorners(int x, int y) =>
        [corners[x, y], corners[x + 1, y], corners[x + 1, y + 1], corners[x, y + 1]];

    // Проверяет пиксель контура, за пределами маски возвращает пустоту
    public bool IsOutline(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height && outline[x, y];
}

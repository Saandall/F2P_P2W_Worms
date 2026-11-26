namespace F2P_P2W_Worms.Terrain;

// Добавляет нижние массы и наращивает землю по соседним заполненным клеткам
public static class TerrainGrowth
{
    // Изменяет переданную маску, сохраняя существующую землю и клетки внешней границы
    public static void Apply(bool[,] mask, TerrainGrowthSettings settings, Random random)
    {
        ArgumentNullException.ThrowIfNull(mask);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(random);
        int width = mask.GetLength(0), height = mask.GetLength(1);
        if (width < 3 || height < 3)
            throw new ArgumentException("Для роста нужна сетка не меньше 3 на 3.", nameof(mask));
        settings.Validate(width);

        AddBottomTerrain(mask, settings, random);
        var pending = new List<GridPoint>();
        for (int iteration = 0; iteration < settings.Iterations; iteration++)
        {
            pending.Clear();
            for (int y = 1; y < height - 1; y++)
                for (int x = 1; x < width - 1; x++)
                {
                    if (mask[x, y]) continue;
                    int neighbors = CountNeighbors(mask, x, y);
                    double chance = GrowthChance(neighbors, y, height, settings);
                    if (chance >= 1 || (chance > 0 && random.NextDouble() < chance))
                        pending.Add(new GridPoint(x, y));
                }

            // Отложенная запись исключает каскадное заполнение в пределах одной итерации
            foreach (var point in pending) mask[point.X, point.Y] = true;
        }
    }

    // Размещает различные нижние клетки и один прямоугольный блок до начала роста
    private static void AddBottomTerrain(bool[,] mask, TerrainGrowthSettings settings, Random random)
    {
        int width = mask.GetLength(0), height = mask.GetLength(1);
        int[] columns = Enumerable.Range(1, width - 2).ToArray();
        for (int i = 0; i < settings.BottomSeedCount; i++)
        {
            int index = random.Next(i, columns.Length);
            (columns[i], columns[index]) = (columns[index], columns[i]);
            mask[columns[i], height - 2] = true;
        }

        if (settings.BottomChunkWidth == 0 || settings.BottomChunkHeight == 0) return;
        int chunkWidth = Math.Max(1, (int)Math.Round((width - 2) * settings.BottomChunkWidth));
        int chunkHeight = Math.Max(1, (int)Math.Round((height - 2) * settings.BottomChunkHeight));
        int left = random.Next(1, width - chunkWidth);
        for (int y = height - 1 - chunkHeight; y < height - 1; y++)
            for (int x = left; x < left + chunkWidth; x++)
                mask[x, y] = true;
    }

    // Считает только верхнего, нижнего, левого и правого соседей
    private static int CountNeighbors(bool[,] mask, int x, int y) =>
        (mask[x - 1, y] ? 1 : 0) + (mask[x + 1, y] ? 1 : 0) +
        (mask[x, y - 1] ? 1 : 0) + (mask[x, y + 1] ? 1 : 0);

    // Ограничивает верхний рост и уменьшает вероятность одиночного соседа с высотой
    private static double GrowthChance(int neighbors, int y, int height, TerrainGrowthSettings settings)
    {
        double depth = (double)y / (height - 1);
        if (neighbors > 1)
            return depth < settings.UpperRegionHeight ? settings.UpperRegionGrowthChance : 1;
        if (neighbors == 1) return settings.SingleNeighborChance * depth;
        return 0;
    }
}

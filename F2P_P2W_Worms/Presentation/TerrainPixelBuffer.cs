using F2P_P2W_Worms.Terrain;

namespace F2P_P2W_Worms.Presentation;

// Готовые данные кадра без объектов WPF
public sealed record TerrainFrame(TerrainModel Terrain, byte[] Pixels, bool ShowDebugLayers, TimeSpan BufferDuration);

// Готовит цветовой буфер большой маски обычными массивами
public static class TerrainPixelBuffer
{
    private const uint LandColor = 0xFF4E9D3C;
    private const uint SkeletonColor = 0xFF00A1B5;
    private const uint AnchorColor = 0xFF087FA3;
    private const uint OutlineColor = 0xFFFFFFFF;

    // Записывает строки BGRA независимо, последовательно или через Parallel.For
    public static byte[] Create(TerrainModel terrain, bool showDebugLayers = true, bool parallel = true)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        var filled = terrain.FilledMap ?? throw new ArgumentException("Не задана большая маска земли.", nameof(terrain));
        var contour = terrain.ContourMap ?? throw new ArgumentException("Не задана маска контуров.", nameof(terrain));
        int width = filled.Width, height = filled.Height;
        int stride = checked(width * 4);
        var pixels = new byte[checked(stride * height)];
        var skeleton = new bool[terrain.Width, terrain.Height];
        var anchors = new bool[terrain.Width, terrain.Height];
        if (showDebugLayers)
        {
            foreach (var point in terrain.Anchors) skeleton[point.X, point.Y] = anchors[point.X, point.Y] = true;
            foreach (var edge in terrain.Edges)
                TerrainGenerator.RasterizeLine(skeleton, terrain.Anchors[edge.StartIndex], terrain.Anchors[edge.EndIndex]);
        }

        if (parallel) Parallel.For(0, height, WriteRow);
        else for (int y = 0; y < height; y++) WriteRow(y);
        return pixels;

        // Читает неизменяемые маски и записывает только собственную строку буфера
        void WriteRow(int y)
        {
            for (int x = 0; x < width; x++)
            {
                if (!filled.IsSolid(x, y)) continue;
                uint color = LandColor;
                if (showDebugLayers)
                {
                    int cellX = x * terrain.Width / width, cellY = y * terrain.Height / height;
                    if (skeleton[cellX, cellY]) color = SkeletonColor;
                    if (anchors[cellX, cellY]) color = AnchorColor;
                }
                if (contour.IsOutline(x, y)) color = OutlineColor;
                int index = y * stride + x * 4;
                pixels[index] = (byte)color;
                pixels[index + 1] = (byte)(color >> 8);
                pixels[index + 2] = (byte)(color >> 16);
                pixels[index + 3] = (byte)(color >> 24);
            }
        }
    }
}

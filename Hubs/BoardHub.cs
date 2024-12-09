using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

public class BoardHub : Hub, IDisposable
{
    private static int EventCount = 0;

    private static readonly object _lock = new();
    private readonly IDistributedCache _cache;
    private const string BoardCacheKey = "MainBoard";

    private List<Task> tasks = new List<Task>();

    public BoardHub(IDistributedCache cache)
    {
        Console.WriteLine("Instantiated BoardHub");
        _cache = cache;
        tiles = new();
    }

    // juanito - A Board is a 2d set of 128x128 tiles
    private static Dictionary<string, Tile>? tiles = new();

    private static string[][] InitializeBoard(int rows, int cols)
    {
        var board = new string[rows][];
        for (int i = 0; i < rows; i++)
        {
            board[i] = new string[cols];
            for (int j = 0; j < cols; j++)
            {
                board[i][j] = "#FFFFFF"; // Default color (white)
            }
        }
        return board;
    }

    private string GetTilePartitionKey(int x, int y)
    {
        return $"{BoardCacheKey}_{x}_{y}";
    }

    private Tile? GetTileFromLocal(int x, int y)
    {
        var tileKey = GetTilePartitionKey(x, y);
        return tiles.GetValueOrDefault(tileKey, null);
    }

    private Tile? GetTileFromCache(int x, int y)
    {
        _ = tiles ?? throw new InvalidOperationException("Tiles not initialized");

        var tileKey = GetTilePartitionKey(x, y);

        var cachedTileString = _cache.GetString(tileKey);

        return
            cachedTileString != null
            ? new Tile(JsonSerializer.Deserialize<string[][]>(cachedTileString) ?? throw new InvalidOperationException("Failed to deserialize tile"))
            : null;
    }

    private void SaveTileToCache(Tile tile)
    {
        var tileKey = GetTilePartitionKey(tile.X, tile.Y);
        var serializedTile = JsonSerializer.Serialize(tile.Pixels);
        _cache.SetStringAsync(tileKey, serializedTile);
    }

    public async Task SendPixel(int x, int y, string color)
    {
        var tile = GetTileOrDefault(x, y);

        // Relative coords
        var relative_x = x % PixelBoardConstants.TileRows;
        var relative_y = y % PixelBoardConstants.TileCols;

        relative_x = Math.Abs(relative_x);
        relative_y = Math.Abs(relative_y);

        _ = tile ?? throw new InvalidOperationException("Tile not initialized");

        lock (_lock)
        {
            tile.Pixels[relative_x][relative_y] = color;
        }

        Console.WriteLine($"Event {EventCount++}: {x}, {y}, {color}");


        tasks.Add(Task.Run(() => SaveTileToCache(tile)));
        tasks.Add(Clients.All.SendAsync("UpdateBoard", x, y, color));

        await Task.WhenAll(tasks);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("Client connected");
        //await Clients.Caller.SendAsync("SyncBoard", GetTileOrDefault(0, 0));
        await base.OnConnectedAsync();
    }

    private Tile GetTileOrDefault(int x_coord, int y_coord)
    {
        Console.WriteLine($"GetTileOrDefault: {x_coord}, {y_coord}");

        // Convert coords to tile coords
        var x = x_coord / PixelBoardConstants.TileRows;
        var y = y_coord / PixelBoardConstants.TileCols;

        // Handle negative coords
        if (x_coord < 0) x--;
        if (y_coord < 0) y--;

        x = Math.Abs(x);
        y = Math.Abs(y);

        Console.WriteLine($"Tile coords: {x}, {y}");

        return
            GetTileFromLocal(x, y)
            ?? GetTileFromCache(x, y)
            ?? new Tile(InitializeBoard(PixelBoardConstants.TileRows, PixelBoardConstants.TileCols));
    }

    private class Tile
    {
        public string[][] Pixels { get; set; }
        public Tile(string[][] pixels)
        {
            Pixels = pixels;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }
}


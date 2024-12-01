using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Threading.Tasks;

public class BoardHub : Hub
{
    private static int EventCount = 0;

    private static readonly object _lock = new object();
    private readonly IDistributedCache _cache;
    private const string BoardCacheKey = "MainBoard";

    public BoardHub(IDistributedCache cache)
    {
        _cache = cache;
        LoadBoardFromCache();
    }

    private static string[][]? board;

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

    private void LoadBoardFromCache()
    {
        var cachedBoard = _cache.GetString(BoardCacheKey);
        if (cachedBoard != null)
        {
            board = JsonSerializer.Deserialize<string[][]>(cachedBoard);
        }
        else
        {
            board = InitializeBoard(PixelBoardConstants.Rows, PixelBoardConstants.Cols);
        }
    }

    private void SaveBoardToCache()
    {
        var serializedBoard = JsonSerializer.Serialize(board);
        _cache.SetStringAsync(BoardCacheKey, serializedBoard);
    }

    public async Task SendPixel(int x, int y, string color)
    {
        lock (_lock)
        {
            board[x][y] = color;
        }

        _ = Task.Run(() => SaveBoardToCache());
        _ = Task.Run(async () => Clients.All.SendAsync("UpdateBoard", x, y, color));

        await Task.CompletedTask;
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("Client connected");
        await Clients.Caller.SendAsync("SyncBoard", board);
        await base.OnConnectedAsync();
    }
}
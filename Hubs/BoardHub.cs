using Microsoft.AspNetCore.SignalR;

public class BoardHub : Hub
{

    private static string[][] board = InitializeBoard(20, 50);

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

    public async Task SendPixel(int x, int y, string color)
    {
        Console.WriteLine("Received pixel: " + x + ", " + y + ", " + color);

        // Print board

        board[x][y] = color;

        await Clients.All.SendAsync("UpdateBoard", x, y, color);
    }

    public override async Task OnConnectedAsync()
    {
        // Send the current state of the board to the newly connected client
        await Clients.Caller.SendAsync("SyncBoard", board);

        Console.WriteLine(board[0][0]);

        await base.OnConnectedAsync();
    }
}
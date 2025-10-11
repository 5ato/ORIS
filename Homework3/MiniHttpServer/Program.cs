using System.Text.Json;
using MiniHttpServer.Settings;
using MiniHttpServer.Shared;

namespace MiniHttpServer;

public class Program
{
    public static void Main()
    {
        var manager = SettingsManager.Instance;

        var config = manager.Settings;

        var httpServer = new HttpServer(config);
        var cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            await httpServer.Start(cts.Token);
        });

        Console.WriteLine("Server started");
        Console.WriteLine($"http://{config.Domain}:{config.Port}/");

        while (true)
        {
            var command = Console.ReadLine();
            if (string.Equals(command, "/stop"))
            {
                cts.Cancel();
                break;
            }
        }
        Console.WriteLine("Server stopped");
    }

    public static byte[]? GetPathFile(string path)
    {
        byte[]? response = null;
        try
        {
            response = File.ReadAllBytes(path);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("File Not Found");
        }
        return response;
    }
}

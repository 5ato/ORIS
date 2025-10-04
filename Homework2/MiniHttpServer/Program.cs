using System.Text.Json;
using MiniHttpServer.Shared;

namespace MiniHttpServer;

public class MyConfiguration
{
    public string PublicDirectoryPath { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
}

public class Program
{
    public static void Main()
    {
        string? html = GetPathFile("public/index.html");
        if (html == null)
            return;

        using var stream = new FileStream("settings.json", FileMode.OpenOrCreate);

        MyConfiguration? config = JsonSerializer.Deserialize<MyConfiguration>(stream);

        if (config == null)
        {
            Console.WriteLine("Empty Json property");
            return;
        }

        var httpServer = new HttpServer(config, html);
        var cts = new CancellationTokenSource();


        Task.Run(async () =>
        {
            await httpServer.Start(cts.Token);
        });

        Console.WriteLine("Server started");
        Console.WriteLine($"http://{config.Domain}:{config.Port}/{config.PublicDirectoryPath}");

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

    public static string? GetPathFile(string path)
    {
        string? responseText = null;
        try
        {
            responseText = File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("File Not Found");
        }
        return responseText;
    }
}

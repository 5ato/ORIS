using System.Net;
using System.Text;
using MiniHttpServer;
using MiniHttpServer.Settings;

namespace MiniHttpServer.Shared;

public class HttpServer(AppSettings configuration)
{
    private readonly AppSettings _config = configuration;

    private HttpListener _httpListener;

    private readonly Dictionary<string, string> ContentTypeDictionary = new()
    {
        {"json", "application/json"},
        {"xml", "application/xml"},
        {"jpeg", "image/jpeg"},
        {"png", "image/png"},
        {"svg", "image/svg+xml"},
        {"webp", "image/webp"},
        {"css", "text/css"},
        {"scss", "text/css"},
        { "js", "text/javascript"},
        {"woff", "font/woff"},
        {"woff2", "font/woff2"},
        { "ttf", "font/ttf"},
        {"otf", "font/otf"},
    };

    public async Task Start(CancellationToken token)
    {
        _httpListener = new();
        _httpListener.Prefixes.Add($"http://{_config.Domain}:{_config.Port}/");
        _httpListener.Start();

        await RunningServerAsync(token);
    }

    private async Task RunningServerAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var context = await _httpListener.GetContextAsync();

                var responseByte = Process(context);

                var response = context.Response;

                response.ContentLength64 = responseByte.Length;
                using Stream output = response.OutputStream;

                await output.WriteAsync(responseByte, token);
                await output.FlushAsync(token);
            }
        }
        catch (OperationCanceledException)
        { }
        finally
        {
            Stop();
        }
    }

    private byte[] Process(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        var path = request.Url!.AbsolutePath;

        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        if (path.EndsWith('/') || string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            string indexPath;

            if (path.EndsWith('/'))
            {
                indexPath = $"public{path}index.html";
            }
            else
            {
                indexPath = $"public{path}/index.html";
            }

            if (File.Exists(indexPath))
            {
                response.StatusCode = 200;
                response.ContentType = "text/html; charset=utf-8";
                return Program.GetPathFile(indexPath)!;
            }
            else
            {
                response.StatusCode = 404;
                return Program.GetPathFile($"public/404.html")!;
            }
        }

        string filePath = $"public{path}";
        
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            response.StatusCode = 404;
            return Program.GetPathFile($"public/404.html")!;
        }

        if (!File.Exists(filePath))
        {
            response.StatusCode = 404;
            return Program.GetPathFile($"public/404.html")!;
        }

        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

        response.StatusCode = 200;
        return Program.GetPathFile($"public{path}")!;

        if (ContentTypeDictionary.TryGetValue(extension, out string? contentType))
            response.ContentType = contentType;

        Console.WriteLine(path);

        if (path == "/")
            return Program.GetPathFile($"public{path}index.html")!;

        if (!File.Exists($"public{path}"))
        {
            response.StatusCode = 404;
            return Program.GetPathFile($"public/404.html")!;
        }
        
        if (File.Exists($"public{path.TrimEnd('/')}"))
            path = path.TrimEnd('/');
        
        // var extension = path.Split('.', StringSplitOptions.RemoveEmptyEntries)[^1];

        if (path.EndsWith('/') ||
            string.Equals(extension, "html", StringComparison.OrdinalIgnoreCase))
        {
            response.ContentType = "text/html; charset=utf-8";
            if (path.EndsWith('/'))
                return Program.GetPathFile($"public{path}index.html")!;

            return Program.GetPathFile($"public{path}")!;
        }

        // if (ContentTypeDictionary.TryGetValue(extension, out string? contentType))
        //     response.ContentType = contentType;

        return Program.GetPathFile($"public{path}")!;
    }

    private void Stop()
    {
        _httpListener.Stop();
        _httpListener.Close();
    }
}

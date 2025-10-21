using System.Net;
using System.Text;

namespace MiniHttpServer.Shared;

public class HttpServer(MyConfiguration configuration, string _htmlPage)
{
    private readonly MyConfiguration _config = configuration;

    private HttpListener _httpListener;

    public string _htmlPage = _htmlPage;

    public async Task Start(CancellationToken token)
    {
        _httpListener = new();
        _httpListener.Prefixes.Add($"http://{_config.Domain}:{_config.Port}/{_config.PublicDirectoryPath}/");
        _httpListener.Start();

        await RunningServerAsync(token);
    }

    private async Task RunningServerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();

                var response = context.Response;

                byte[] buffer = Encoding.UTF8.GetBytes(_htmlPage);

                response.ContentLength64 = buffer.Length;
                using Stream output = response.OutputStream;

                await output.WriteAsync(buffer, token);
                await output.FlushAsync(token);
            }
            catch
            {
                Console.WriteLine("Фигня");
            }
        }
        Stop();
    }

    private void Stop()
    {
        _httpListener.Stop();
        _httpListener.Close();
    }
}

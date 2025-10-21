using System.Net;
using System.Text;
using MiniHttpServer;
using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Handlers;
using MiniHttpServer.Settings;

namespace MiniHttpServer.Shared;

public class HttpServer(AppSettings configuration)
{
    private readonly AppSettings _config = configuration;

    private HttpListener _httpListener;

    private readonly Handler staticFilesHandler = new StaticFilesHandler();
    private readonly Handler endPointsHandler = new EndpointsHandler();

    public async Task Start(CancellationToken token)
    {
        staticFilesHandler.Successor = endPointsHandler;
        _httpListener = new();
        _httpListener.Prefixes.Add($"http://{_config.Domain}:{_config.Port}/");
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

                var responseByte = Process(context);

                var response = context.Response;

                response.ContentLength64 = responseByte.Length;
                using Stream output = response.OutputStream;

                await output.WriteAsync(responseByte, token);
                await output.FlushAsync(token);
            }
            catch
            {
                Console.WriteLine("Фигня");
            }
        }
        Stop();
    }

    private byte[] Process(HttpListenerContext context)
    {
        return staticFilesHandler.HandleRequest(context);;
    }

    private void Stop()
    {
        _httpListener.Stop();
        _httpListener.Close();
    }
}
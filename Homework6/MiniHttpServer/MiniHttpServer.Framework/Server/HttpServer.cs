using System.Net;
using System.Text;
using MiniHttpServer.Framework.Core.Handlers;
using MiniHttpServer.Framework.Core.Abstracts;
using MiniHttpServer.Framework.Settings;

namespace MiniHttpServer.Framework.Server;

public class HttpServer(AppSettings configuration)
{
    private readonly AppSettings _config = configuration;

    private HttpListener _httpListener;

    private readonly Handler staticFilesHandler = new StaticFilesHandler();
    private readonly Handler endPointsHandler = new EndpointsHandler();
    private readonly Handler notFoundHandler = new NotFoundHandler();

    public async Task Start(CancellationToken token)
    {
        endPointsHandler.Successor = notFoundHandler;
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

                await Process(context, token);
            }
            catch
            {
                Console.WriteLine("Фигня");
            }
        }
        Stop();
    }

    private async Task Process(HttpListenerContext context, CancellationToken token)
    {
        await staticFilesHandler.HandleRequest(context, token);;
    }

    private void Stop()
    {
        _httpListener.Stop();
        _httpListener.Close();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MiniHttpServer.Framework.Core.Abstracts;
using MiniHttpServer.Framework.Shared;

namespace MiniHttpServer.Framework.Core.Handlers
{
    public class StaticFilesHandler : Handler
    {
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
            {"js", "text/javascript"},
            {"woff", "font/woff"},
            {"woff2", "font/woff2"},
            {"ttf", "font/ttf"},
            {"otf", "font/otf"},
        };

        public override async Task HandleRequest(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            var response = context.Response;

            var isGetMethod = string.Equals(request.HttpMethod, "get", StringComparison.OrdinalIgnoreCase);
            var isStaticFile = Path.HasExtension(request.Url!.AbsoluteUri);

            if (isGetMethod && isStaticFile)
            {
                var path = request.Url!.AbsolutePath;

                Console.WriteLine(path);

                var extension = path.Split('.', StringSplitOptions.RemoveEmptyEntries)[^1];
                string resultPath;

                if (!File.Exists($"public{path}"))
                {
                    await Successor.HandleRequest(context, cancellationToken);
                    return;
                }

                if (path.EndsWith('/') ||
                    string.Equals(extension, "html", StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 200;
                    response.ContentType = "text/html; charset=utf-8";
                    if (path.EndsWith('/'))
                    {
                        resultPath = $"public{path}index.html";
                    } else
                    {
                        resultPath = $"public{path}";
                    }

                    await WriteResponseAsync(context, resultPath, cancellationToken);
                    return;
                }

                response.StatusCode = 200;

                if (ContentTypeDictionary.TryGetValue(extension, out string? contentType))
                    response.ContentType = contentType;

                resultPath = $"public{path}";
                await WriteResponseAsync(context, resultPath, cancellationToken);
                return;
            }
            else if (Successor != null)
            {
                await Successor.HandleRequest(context, cancellationToken);
            }
        }
    }
}
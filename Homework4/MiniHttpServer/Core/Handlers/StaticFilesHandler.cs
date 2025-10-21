using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MiniHttpServer.Core.Abstracts;

namespace MiniHttpServer.Core.Handlers
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
            { "js", "text/javascript"},
            {"woff", "font/woff"},
            {"woff2", "font/woff2"},
            { "ttf", "font/ttf"},
            {"otf", "font/otf"},
        };

        public override byte[] HandleRequest(HttpListenerContext context)
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

                if (path.EndsWith('/') ||
                    string.Equals(extension, "html", StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 200;
                    response.ContentType = "text/html; charset=utf-8";
                    if (path.EndsWith('/'))
                        return Program.GetPathFile($"public{path}index.html")!;

                    return Program.GetPathFile($"public{path}")!;
                }

                if (!File.Exists($"public{path}"))
                {
                    response.StatusCode = 404;
                    return Program.GetPathFile($"public/404.html")!;
                }

                response.StatusCode = 200;

                if (ContentTypeDictionary.TryGetValue(extension, out string? contentType))
                    response.ContentType = contentType;

                return Program.GetPathFile($"public{path}")!;
            }
            else if (Successor != null)
            {
                return Successor.HandleRequest(context);
            }

            response.StatusCode = 404;
            return Program.GetPathFile($"public/404.html")!;
        }
    }
}
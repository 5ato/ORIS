using MiniHttpServer.Framework.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.Abstracts
{
    public abstract class Handler
    {
        public Handler Successor { get; set; }
        public abstract Task HandleRequest(HttpListenerContext context, CancellationToken cancellationToken);

        protected static async Task WriteResponseAsync(HttpListenerContext context, string path, CancellationToken cancellationToken)
        {
            var response = context.Response;

            var responseByte = Utils.GetPathFile(path);

            response.ContentLength64 = responseByte.Length;
            using Stream output = response.OutputStream;

            await output.WriteAsync(responseByte, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        protected static async Task WriteResponseContentAsync(HttpListenerContext context, string content, CancellationToken cancellationToken)
        {
            var response = context.Response;

            var responseByte = Encoding.UTF8.GetBytes(content);

            response.ContentLength64 = responseByte.Length;
            using Stream output = response.OutputStream;

            await output.WriteAsync(responseByte, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }
    }
}
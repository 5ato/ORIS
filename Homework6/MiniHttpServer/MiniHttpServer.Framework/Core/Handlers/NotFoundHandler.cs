using MiniHttpServer.Framework.Core.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.Handlers
{
    public class NotFoundHandler : Handler
    {
        public override async Task HandleRequest(HttpListenerContext context, CancellationToken cancellationToken)
        {
            context.Response.StatusCode = 404;
            await WriteResponseAsync(context, "public/404.html", cancellationToken);
        }
    }
}

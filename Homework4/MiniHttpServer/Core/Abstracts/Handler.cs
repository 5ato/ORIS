using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Abstracts
{
    public abstract class Handler
    {
        public Handler Successor { get; set; }
        public abstract byte[] HandleRequest(HttpListenerContext context);
    }
}
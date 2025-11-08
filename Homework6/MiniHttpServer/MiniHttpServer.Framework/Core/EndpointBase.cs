using MiniHttpServer.Framework.Core.HttpResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core
{
    public abstract class EndpointBase
    {
        protected HttpListenerContext Context { get; private set; }

        internal void SetContext(HttpListenerContext context)
        {
            Context = context;
        }

        protected PageResult Page(string pathTemplate, object data) 
            => new PageResult($"Pages/{GetType().Name}/{pathTemplate}", data);

        protected JsonResult Json(object data) 
            => new JsonResult(data);
    }
}

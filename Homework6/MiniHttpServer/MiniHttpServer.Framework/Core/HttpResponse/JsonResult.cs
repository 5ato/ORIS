using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class JsonResult : IHttpResult
    {
        private object _data;
        public JsonResult(object data) 
        {
            _data = data;
        }
        public string Execute(HttpListenerContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            var resultJson = JsonSerializer.Serialize(_data);
            return resultJson;
        }
    }
}

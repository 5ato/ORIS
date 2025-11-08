using MiniTemplateEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class PageResult : IHttpResult
    {
        private readonly string _pathTemplate;
        private readonly object _data;

        public PageResult(string pathTemplate, object data) 
        {
            _pathTemplate = pathTemplate;
            _data = data;
        }

        public string Execute(HttpListenerContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html; charset=utf-8";
            return new HtmlTemplateRenderer().RenderFromFile(_pathTemplate, _data);
        }
    }
}

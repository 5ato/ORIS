using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpGetAttribute : Attribute
    {
        public string? Route { get; set; }
        public HttpGetAttribute() { }
        public HttpGetAttribute(string? route)
        {
            Route = route;
        }
    }
}
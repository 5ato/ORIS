using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPostAttribute : Attribute
    {
        public string? Route { get; set; }
        public HttpPostAttribute() { }
        public HttpPostAttribute(string? route)
        {
            Route = route;
        }
    }
}
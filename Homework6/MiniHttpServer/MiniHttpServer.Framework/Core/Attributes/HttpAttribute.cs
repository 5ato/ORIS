using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class HttpAttribute : Attribute
    {
        public string? Route { get; set; }
        public HttpAttribute() { }
        public HttpAttribute(string? route)
        {
            Route = route;
        }
    }
}

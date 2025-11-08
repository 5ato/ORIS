using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.Attributes
{
    public class HttpGetAttribute : HttpAttribute
    {
        public HttpGetAttribute() { }
        public HttpGetAttribute(string Route) : base(Route) { }
    }
}
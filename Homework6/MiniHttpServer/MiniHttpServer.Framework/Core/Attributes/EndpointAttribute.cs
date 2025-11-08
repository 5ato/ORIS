using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EndpointAttribute : Attribute
    {
    }
}
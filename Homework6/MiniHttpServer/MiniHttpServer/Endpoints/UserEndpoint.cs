using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    public class UserEndpoint : EndpointBase
    {
        [HttpGet("users")]
        public IHttpResult GetUsers()
        {
            return Json(new { Name = "Aboba", Age = 1123 });
        }
    }
}

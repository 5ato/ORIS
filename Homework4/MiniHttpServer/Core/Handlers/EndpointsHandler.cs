using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Attributes;

namespace MiniHttpServer.Core.Handlers
{
    public class EndpointsHandler : Handler
    {
        public override byte[] HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var endPointName = request.Url!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).First();

            var assembly = Assembly.GetExecutingAssembly();

            var endPoint = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<EndpointAttribute>() != null)
                .FirstOrDefault(e => IsCheckedNameEndpoint(e.Name, endPointName));

            if (endPoint == null) return PassOrNotFound(context);

            var method = endPoint.GetMethods().Where(t => t.GetCustomAttributes(true)
                        .Any(attr => attr.GetType().Name.Equals($"Http{request.HttpMethod}Attribute", StringComparison.OrdinalIgnoreCase)))
                        .FirstOrDefault();

            if (method == null) return PassOrNotFound(context);

            using StreamReader reader = new(request.InputStream);

            var result = HttpUtility.ParseQueryString(reader.ReadToEnd());

            var arguments = method.GetParameters();

            List<object> methodParams = [];
            foreach (var arg in arguments)
            {
                methodParams.Add(result[arg.Name]);
            }

            var ret = method.Invoke(Activator.CreateInstance(endPoint), [.. methodParams]);

            if (ret == null) return PassOrNotFound(context);

            context.Response.StatusCode = 200;

            return Program.GetPathFile($"public/{(string)ret}")!;
        }

        private byte[] PassOrNotFound(HttpListenerContext context)
        {
            if (Successor != null)
                return Successor.HandleRequest(context);

            context.Response.StatusCode = 404;
            return Program.GetPathFile($"public/404.html")!;
        }

        private bool IsCheckedNameEndpoint(string endPointName, string className) =>
            endPointName.Equals(className, StringComparison.OrdinalIgnoreCase) ||
            endPointName.Equals($"{className}Endpoint", StringComparison.OrdinalIgnoreCase);
    }
}
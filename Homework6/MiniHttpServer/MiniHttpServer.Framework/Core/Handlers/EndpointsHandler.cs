using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MiniHttpServer.Framework.Core.Abstracts;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Framework.Shared;

namespace MiniHttpServer.Framework.Core.Handlers
{
    public class EndpointsHandler : Handler
    {
        public override async Task HandleRequest(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;

            var urlSegments = request.Url!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var endPointName = urlSegments.Length > 0 ? urlSegments[0] : string.Empty;
            var actionName = urlSegments.Length > 1 ? urlSegments[1] : string.Empty;

            var assembly = Assembly.GetEntryAssembly();

            var endPoint = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<EndpointAttribute>() != null)
                .FirstOrDefault(e => IsCheckedNameEndpoint(e.Name, endPointName));

            if (endPoint == null)
            {
                await Successor.HandleRequest(context, cancellationToken);
                return;
            }

            var methods = endPoint.GetMethods()
                .Where(m => m.GetCustomAttributes(true)
                    .Any(attr => attr.GetType().Name.StartsWith("Http", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var method = FindMatchingMethod(methods, request, actionName);

            if (method == null)
            {
                await Successor.HandleRequest(context, cancellationToken);
                return;
            }

            bool isBaseEndpoint = endPoint.Assembly.GetTypes()
                .Any(t => typeof(EndpointBase).IsAssignableFrom(t) && !t.IsAbstract);

            var instanceEndpoint = Activator.CreateInstance(endPoint);

            if (isBaseEndpoint)
            {
                (instanceEndpoint as EndpointBase).SetContext(context);
            }

            using StreamReader reader = new(request.InputStream);

            var result = HttpUtility.ParseQueryString(reader.ReadToEnd());

            var arguments = method.GetParameters();

            List<object> methodParams = [];
            foreach (var arg in arguments)
            {
                methodParams.Add(result[arg.Name]);
            }

            var ret = method.Invoke(instanceEndpoint, [.. methodParams]);

            if (ret is PageResult page)
            {
               
                await WriteResponseContentAsync(context, page.Execute(context), cancellationToken);
            }
            else if (ret is JsonResult json)
            {
                await WriteResponseContentAsync(context, json.Execute(context), cancellationToken);
            }

            if (ret == null)
            {
                await Successor.HandleRequest(context, cancellationToken);
                return;
            }

            context.Response.StatusCode = 200;
        }

        private bool IsCheckedNameEndpoint(string endPointName, string className) =>
            endPointName.Equals(className, StringComparison.OrdinalIgnoreCase) ||
            endPointName.Equals($"{className}Endpoint", StringComparison.OrdinalIgnoreCase);

        private MethodInfo FindMatchingMethod(List<MethodInfo> methods, HttpListenerRequest request, string actionRoute)
        {
            var httpMethodAttributes = methods
                .SelectMany(m => m.GetCustomAttributes(true)
                    .Where(attr => attr.GetType().Name.Equals($"Http{request.HttpMethod}Attribute", StringComparison.OrdinalIgnoreCase))
                    .Select(attr => new { Method = m, Attribute = attr }))
                .ToList();

            // Если запрос без действия (например: /auth)
            if (string.IsNullOrEmpty(actionRoute))
            {
                // Ищем первый метод без роута (Route = null или пустая строка)
                var methodWithoutRoute = httpMethodAttributes
                    .FirstOrDefault(x =>
                        x.Attribute is HttpAttribute httpAttr &&
                        string.IsNullOrEmpty(httpAttr.Route))?
                    .Method;

                return methodWithoutRoute;
            }
            // Если запрос с действием (например: /auth/login)
            else
            {
                // Ищем метод с соответствующим роутом
                var methodWithRoute = httpMethodAttributes
                    .FirstOrDefault(x =>
                        x.Attribute is HttpAttribute httpAttr &&
                        !string.IsNullOrEmpty(httpAttr.Route) &&
                        httpAttr.Route.Equals(actionRoute, StringComparison.OrdinalIgnoreCase))?
                    .Method;

                return methodWithRoute;
            }
        }
    }
}
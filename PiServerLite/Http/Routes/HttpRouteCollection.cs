using PiServerLite.Http.Handlers;
using PiServerLite.Http.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Http.Routes
{
    public class HttpRouteCollection
    {
        private readonly Dictionary<string, HttpRouteDescription> routeList;


        public HttpRouteCollection(StringComparer comparer = null)
        {
            var _comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
            routeList = new Dictionary<string, HttpRouteDescription>(_comparer);
        }

        public void Scan(Assembly assembly)
        {
            var typeList = assembly.DefinedTypes
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var classType in typeList) {
                var attrList = classType.GetCustomAttributes<HttpHandlerAttribute>();

                var attrSecure = classType.GetCustomAttribute<SecureAttribute>();

                foreach (var attr in attrList) {
                    var _path = attr.Path.StartsWith("/")
                        ? attr.Path.Substring(1) : attr.Path;

                    routeList[_path] = new HttpRouteDescription {
                        ClassType = classType,
                        IsSecure = attrSecure != null,
                    };
                }
            }
        }

        internal async Task<HttpHandlerResult> ExecuteAsync(IHttpHandler handlerObj, CancellationToken token)
        {
            var method = handlerObj.HttpContext.Request.HttpMethod.ToUpper();

            if (handlerObj is HttpHandlerAsync handlerAsync) {
                switch (method) {
                    case "GET":
                        return await handlerAsync.GetAsync(token);
                    case "POST":
                        return await handlerAsync.PostAsync(token);
                    case "HEAD":
                        return await handlerAsync.HeadAsync(token);
                    case "OPTIONS":
                        return await handlerAsync.OptionsAsync(token);
                    default:
                        throw new ApplicationException($"Unsupported method '{method}'!");
                }
            }

            if (handlerObj is HttpHandler handler) {
                return await Task.Run(() => {
                    switch (method) {
                        case "GET":
                            return handler.Get();
                        case "POST":
                            return handler.Post();
                        case "HEAD":
                            return handler.Head();
                        case "OPTIONS":
                            return handler.Options();
                        default:
                            throw new ApplicationException($"Unsupported method '{method}'!");
                    }
                }, token);
            }

            return null;
        }

        internal bool FindRoute(string path, out HttpRouteDescription routeDescription)
        {
            var _path = path.StartsWith("/")
                ? path.Substring(1) : path;

            return routeList.TryGetValue(_path, out routeDescription);
        }

        internal IHttpHandler GetHandler(HttpRouteDescription routeDescription, HttpListenerContext httpContext, HttpReceiverContext context)
        {
            if (!(Activator.CreateInstance(routeDescription.ClassType) is IHttpHandler handler))
                throw new ApplicationException($"Unable to construct HttpHandler implementation '{routeDescription.ClassType.Name}'!");

            handler.HttpContext = httpContext;
            handler.Context = context;
            handler.OnRequestReceived();
            return handler;
        }
    }
}

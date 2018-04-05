using PiServerLite.Http.Handlers;
using PiServerLite.Http.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
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
                    routeList[attr.Path] = new HttpRouteDescription {
                        ClassType = classType,
                        IsSecure = attrSecure != null,
                    };
                }
            }
        }

        internal HttpHandlerResult Execute(IHttpHandler handlerObj)
        {
            var method = handlerObj.HttpContext.Request.HttpMethod.ToUpper();

            if (handlerObj is HttpHandlerAsync handlerAsync) {
                switch (method) {
                    case "GET":
                        return handlerAsync.GetAsync().GetAwaiter().GetResult();
                    case "POST":
                        return handlerAsync.PostAsync().GetAwaiter().GetResult();
                    case "HEAD":
                        return handlerAsync.HeadAsync().GetAwaiter().GetResult();
                    case "OPTIONS":
                        return handlerAsync.OptionsAsync().GetAwaiter().GetResult();
                    default:
                        throw new ApplicationException();
                }
            }

            if (handlerObj is HttpHandler handler) {
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
            }

            return null;
        }

        internal async Task<HttpHandlerResult> ExecuteAsync(IHttpHandler handlerObj)
        {
            var method = handlerObj.HttpContext.Request.HttpMethod.ToUpper();

            if (handlerObj is HttpHandlerAsync handlerAsync) {
                switch (method) {
                    case "GET":
                        return await handlerAsync.GetAsync();
                    case "POST":
                        return await handlerAsync.PostAsync();
                    case "HEAD":
                        return await handlerAsync.HeadAsync();
                    case "OPTIONS":
                        return await handlerAsync.OptionsAsync();
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
                });
            }

            return null;
        }

        internal bool FindRoute(string path, out HttpRouteDescription routeDescription)
        {
            return routeList.TryGetValue(path, out routeDescription);
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

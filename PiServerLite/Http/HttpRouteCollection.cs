using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace PiServerLite.Http
{
    //public delegate HttpHandlerResult RouteEvent(HttpListenerContext httpContext, HttpReceiverContext context);
    //public delegate Task<HttpHandlerResult> RouteEventAsync(HttpListenerContext httpContext, HttpReceiverContext context);

    public class HttpRouteCollection
    {
        public Dictionary<string, Type> RouteList {get;}


        public HttpRouteCollection(StringComparer comparer = null)
        {
            var _comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
            RouteList = new Dictionary<string, Type>(_comparer);
        }

        public HttpHandlerResult Execute(string path, HttpListenerContext httpContext, HttpReceiverContext context)
        {
            var handlerObj = GetHandler(path, httpContext, context);
            if (handlerObj == null) return null;

            var method = httpContext.Request.HttpMethod.ToUpper();

            var handlerAsync = handlerObj as HttpHandlerAsync;
            if (handlerAsync != null) {
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

            var handler = handlerObj as HttpHandler;
            if (handler != null) {
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

        public async Task<HttpHandlerResult> ExecuteAsync(string path, HttpListenerContext httpContext, HttpReceiverContext context)
        {
            var handlerObj = GetHandler(path, httpContext, context);
            if (handlerObj == null) return null;

            var method = httpContext.Request.HttpMethod.ToUpper();

            var handlerAsync = handlerObj as HttpHandlerAsync;
            if (handlerAsync != null) {
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

            var handler = handlerObj as HttpHandler;
            if (handler != null) {
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

        public void Scan(Assembly assembly)
        {
            var typeList = assembly.DefinedTypes
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var classType in typeList) {
                var attrList = classType.GetCustomAttributes<HttpHandlerAttribute>();

                foreach (var attr in attrList)
                    RouteList[attr.Path] = classType;
            }
        }

        private IHttpHandler GetHandler(string path, HttpListenerContext httpContext, HttpReceiverContext context)
        {
            Type type;
            if (!RouteList.TryGetValue(path, out type))
                return null;

            var handlerObj = Activator.CreateInstance(type) as IHttpHandler;
            if (handlerObj == null) throw new ApplicationException($"Unable to construct HttpHandler implementation '{type.Name}'!");

            handlerObj.HttpContext = httpContext;
            handlerObj.Context = context;
            handlerObj.OnRequestReceived();

            return handlerObj;
        }
    }
}

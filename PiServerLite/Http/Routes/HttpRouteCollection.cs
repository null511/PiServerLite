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
            var method = handlerObj.HttpContext.Request.HttpMethod;
            HttpHandlerResult result = null;

            var filters = handlerObj.GetType().GetCustomAttributes()
                .OfType<HttpFilterAttribute>().ToArray();

            foreach (var filter in filters) {
                result = filter.Run(handlerObj, HttpFilterEvents.Before);
                if (result != null) return result;
            }

            try {
                if (handlerObj is HttpHandlerAsync handlerAsync) {
                    if (!execMapAsync.TryGetValue(method, out var execFunc))
                        throw new ApplicationException($"Unsupported method '{method}'!");

                    result = await execFunc.Invoke(handlerAsync, token);
                }
                else if (handlerObj is HttpHandler handler) {
                    if (!execMap.TryGetValue(method, out var execFunc))
                        throw new ApplicationException($"Unsupported method '{method}'!");

                    result = await Task.Run(() => execFunc.Invoke(handler), token);
                }
            }
            finally {
                foreach (var filter in filters) {
                    var newResult = filter.RunAfter(handlerObj, result);
                    if (newResult != null) {
                        result = newResult;
                        break;
                    }
                }
            }

            return result;
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

        private static readonly Dictionary<string, Func<HttpHandler, HttpHandlerResult>> execMap =
            new Dictionary<string, Func<HttpHandler, HttpHandlerResult>>(StringComparer.OrdinalIgnoreCase) {
                ["GET"] = handler => handler.Get(),
                ["POST"] = handler => handler.Post(),
                ["HEAD"] = handler => handler.Head(),
                ["OPTIONS"] = handler => handler.Options(),
            };

        private static readonly Dictionary<string, Func<HttpHandlerAsync, CancellationToken, Task<HttpHandlerResult>>> execMapAsync =
            new Dictionary<string, Func<HttpHandlerAsync, CancellationToken, Task<HttpHandlerResult>>>(StringComparer.OrdinalIgnoreCase) {
                ["GET"] = (handler, token) => handler.GetAsync(token),
                ["POST"] = (handler, token) => handler.PostAsync(token),
                ["HEAD"] = (handler, token) => handler.HeadAsync(token),
                ["OPTIONS"] = (handler, token) => handler.OptionsAsync(token),
            };
    }
}

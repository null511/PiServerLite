using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace PiServerLite.Http
{
    public delegate HttpHandlerResult RouteEvent(HttpListenerContext httpContext, HttpReceiverContext context);

    public class HttpRouteCollection
    {
        public Dictionary<string, RouteEvent> RouteList {get;}


        public HttpRouteCollection(StringComparer comparer = null)
        {
            RouteList = new Dictionary<string, RouteEvent>(comparer ?? StringComparer.OrdinalIgnoreCase);
        }

        public bool TryFind(string path, out RouteEvent action)
        {
            return RouteList.TryGetValue(path, out action);
        }

        public void Scan(Assembly assembly)
        {
            var typeList = assembly.DefinedTypes
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var classType in typeList) {
                var attrList = classType.GetCustomAttributes<HttpHandlerAttribute>().ToArray();
                if (!attrList.Any()) continue;

                foreach (var attr in attrList) {
                    RouteList[attr.Path] = (httpContext, context) => {
                        var handler = Activator.CreateInstance(classType) as HttpHandler;
                        if (handler == null) throw new ApplicationException($"Unable to construct HttpHandler implementation '{classType.Name}'!");

                        handler.HttpContext = httpContext;
                        handler.Context = context;

                        HttpHandlerResult result = null;
                        switch (httpContext.Request.HttpMethod.ToUpper()) {
                            case "GET":
                                result = handler.Get();
                                break;
                            case "POST":
                                result = handler.Post();
                                break;
                            case "HEAD":
                                result = handler.Head();
                                break;
                            case "OPTIONS":
                                result = handler.Options();
                                break;
                        }

                        return result;
                    };
                }
            }
        }
    }
}

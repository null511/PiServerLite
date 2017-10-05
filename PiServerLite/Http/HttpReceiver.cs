using System;
using System.IO;
using System.Linq;
using System.Net;

namespace PiServerLite.Http
{
    public class HttpReceiver : IDisposable
    {
        public event EventHandler HttpError;

        private readonly HttpListener listener;

        public HttpRouteCollection Routes {get;}
        public HttpReceiverContext Context {get;}


        public HttpReceiver(string prefix, HttpReceiverContext context)
        {
            this.Context = context;

            listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            Routes = new HttpRouteCollection();
        }

        public void Dispose()
        {
            Stop();
            listener?.Close();
        }

        public void Start()
        {
            listener.Start();
            Wait();
        }

        public void Stop()
        {
            listener.Stop();
        }

        private void Wait()
        {
            var state = new object();
            listener.BeginGetContext(OnContextReceived, state);
        }

        private void OnContextReceived(IAsyncResult result)
        {
            HttpListenerContext context;
            try {
                context = listener.EndGetContext(result);
            }
            catch (ObjectDisposedException) {
                // ignore
                return;
            }
            catch (Exception error) {
                OnHttpError(error);
                return;
            }
            finally {
                if (listener.IsListening) Wait();
            }

            try {
                RouteRequest(context);
            }
            catch (Exception error) {
                OnHttpError(error);
            }
        }

        private void RouteRequest(HttpListenerContext context)
        {
            var path = context.Request.Url.AbsolutePath.TrimEnd('/');

            Console.WriteLine($"Request received from '{context.Request.RemoteEndPoint}' -> '{path}'.");

            if (path.StartsWith(Context.UrlRoot))
                path = path.Substring(Context.UrlRoot.Length);

            if (path.Length == 0 || path == "/")
                path = Context.DefaultRoute;

            try {
                GetRouteResult(context, path).Apply(context);
            }
            finally {
                try {
                    context.Response.Close();
                }
                catch {}
            }
        }

        private HttpHandlerResult GetRouteResult(HttpListenerContext httpContext, string path)
        {
            // Content Directories
            var contentRoute = Context.ContentDirectories
                .FirstOrDefault(x => path.StartsWith(x.UrlPath));

            if (contentRoute != null) {
                var localPath = path.Substring(contentRoute.UrlPath.Length);
                return ProcessContent(httpContext, localPath, contentRoute);
            }

            // Handlers
            if (Routes.TryFind(path, out RouteEvent routeAction)) {
                try {
                    return routeAction.Invoke(httpContext, Context);
                }
                catch (Exception error) {
                    return HttpHandlerResult.Exception(error);
                }
            }

            return HttpHandlerResult.NotFound()
                .SetText($"No handler found matching path '{path}'!");
        }

        private HttpHandlerResult ProcessContent(HttpListenerContext context, string localPath, ContentDirectory directory)
        {
            if (Path.DirectorySeparatorChar != '/')
                localPath = localPath.Replace('/', Path.DirectorySeparatorChar);

            var filename = Path.Combine(directory.DirectoryPath, localPath);

            if (!File.Exists(filename))
                return HttpHandlerResult.NotFound();

            return HttpHandlerResult.File(Context, filename);
        }

        protected virtual void OnHttpError(Exception error)
        {
            try {
                HttpError?.Invoke(this, new EventArgs());
            }
            catch {}
        }
    }
}

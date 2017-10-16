using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Http
{
    /// <summary>
    /// Wraps <see cref="HttpListener"/> and routes incoming
    /// requests to <see cref="IHttpHandler"/> implementations.
    /// </summary>
    public class HttpReceiver : IDisposable
    {
        /// <summary>
        /// Occurs when an exception is thrown by the underlying
        /// <see cref="HttpListener"/>, or when an uncaught exception
        /// is raised by an <see cref="IHttpHandler"/> implementation.
        /// </summary>
        public event EventHandler HttpError;

        public HttpListener Listener {get;}
        public HttpRouteCollection Routes {get;}
        public HttpReceiverContext Context {get;}


        /// <summary>
        /// Creates an <see cref="HttpReceiver"/> instance with no attached prefixes.
        /// </summary>
        /// <param name="context"></param>
        public HttpReceiver(HttpReceiverContext context)
        {
            this.Context = context;

            Listener = new HttpListener();
            Routes = new HttpRouteCollection();
        }

        public HttpReceiver(HttpReceiverContext context, string prefix) : this(context)
        {
            Listener.Prefixes.Add(prefix);
        }

        public void Dispose()
        {
            Stop();
            Listener?.Close();
        }

        public void Start()
        {
            Listener.Start();
            Wait();
        }

        public void Stop()
        {
            Listener.Stop();
        }

        private void Wait()
        {
            var state = new object();
            Listener.BeginGetContext(OnContextReceived, state);
        }

        public void AddPrefix(string prefix)
        {
            Listener.Prefixes.Add(prefix);
        }

        private void OnContextReceived(IAsyncResult result)
        {
            HttpListenerContext context;
            try {
                context = Listener.EndGetContext(result);
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
                if (Listener.IsListening) Wait();
            }

            try {
                RouteRequest(context);
            }
            catch (Exception error) {
                OnHttpError(error);
            }
        }

        private async Task RouteRequest(HttpListenerContext context)
        {
            var path = context.Request.Url.AbsolutePath.TrimEnd('/');

            Console.WriteLine($"Request received from '{context.Request.RemoteEndPoint}' -> '{path}'.");

            var root = Context.ListenUri.AbsolutePath.TrimEnd('/');

            if (path.StartsWith(root))
                path = path.Substring(root.Length);

            if (path.Length == 0)
                path = "/";

            HttpHandlerResult result = null;
            var tokenSource = new CancellationTokenSource();
            try {
                result = await GetRouteResult(context, path);
                await result.ApplyAsync(context, tokenSource.Token);
            }
            catch (Exception) {
                tokenSource.Cancel();
                throw;
            }
            finally {
                try {
                    context.Response.Close();
                }
                catch {}

                try {
                    result?.Dispose();
                }
                catch {}

                tokenSource.Dispose();
            }
        }

        private async Task<HttpHandlerResult> GetRouteResult(HttpListenerContext httpContext, string path)
        {
            // Content Directories
            var contentRoute = Context.ContentDirectories
                .FirstOrDefault(x => path.StartsWith(x.UrlPath));

            if (contentRoute != null) {
                var localPath = path.Substring(contentRoute.UrlPath.Length);
                return ProcessContent(httpContext, localPath, contentRoute);
            }

            // Handlers
            try {
                var result = await Routes.ExecuteAsync(path, httpContext, Context);
                if (result != null) return result;
            }
            catch (Exception error) {
                return HttpHandlerResult.Exception(error);
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

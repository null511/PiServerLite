﻿using PiServerLite.Http.Content;
using PiServerLite.Http.Handlers;
using PiServerLite.Http.Routes;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

        /// <summary>
        /// Gets the underlying <see cref="HttpListener"/> instance.
        /// </summary>
        public HttpListener Listener {get;}

        /// <summary>
        /// Gets the collection of route mappings.
        /// </summary>
        public HttpRouteCollection Routes {get;}

        /// <summary>
        /// Collection of configuration information and resources
        /// used to define this <see cref="HttpReceiver"/>.
        /// </summary>
        public HttpReceiverContext Context {get;}


        /// <summary>
        /// Creates an instance of <see cref="HttpReceiver"/> with no attached prefixes.
        /// </summary>
        /// <param name="context"></param>
        public HttpReceiver(HttpReceiverContext context)
        {
            this.Context = context;

            Listener = new HttpListener();
            Routes = new HttpRouteCollection();
        }

        /// <summary>
        /// Creates an instance of <see cref="HttpReceiver"/> with the given prefix.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="prefix">
        /// A string that identifies the URI information that is
        /// compared in incoming requests. The prefix must be
        /// terminated with a forward slash ("/").
        /// </param>
        public HttpReceiver(HttpReceiverContext context, string prefix) : this(context)
        {
            Listener.Prefixes.Add(prefix);
        }

        /// <summary>
        /// Stops receiving incoming requests and
        /// shuts down the <see cref="HttpListener"/>.
        /// </summary>
        public void Dispose()
        {
            Stop();
            Listener?.Close();
        }

        /// <summary>
        /// Start receiving incoming HTTP requests.
        /// </summary>
        public void Start()
        {
            Listener.Start();
            BeginWait();
        }

        /// <summary>
        /// Stop receiving incoming HTTP requests.
        /// </summary>
        public void Stop()
        {
            Listener.Stop();
        }

        /// <summary>
        /// Add a URI prefix to the collection of handled prefixes.
        /// </summary>
        /// <param name="prefix">
        /// A string that identifies the URI information that is
        /// compared in incoming requests. The prefix must be
        /// terminated with a forward slash ("/").
        /// </param>
        public void AddPrefix(string prefix)
        {
            Listener.Prefixes.Add(prefix);
        }

        private void BeginWait()
        {
            var state = new object();
            Listener.BeginGetContext(OnContextReceived, state);
        }

        private void OnContextReceived(IAsyncResult result)
        {
            HttpListenerContext httpContext;
            try {
                httpContext = Listener.EndGetContext(result);
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
                if (Listener.IsListening) BeginWait();
            }

            try {
                var routeTask = RouteRequest(httpContext)
                    .ContinueWith(t => {
                        // TODO: Remove from collection of active route tasks

                        try {
                            httpContext.Response.Close();
                        }
                        catch {}
                    });

                // TODO: Add to collection of active route tasks
            }
            catch (Exception error) {
                OnHttpError(error);
            }
        }

        private async Task RouteRequest(HttpListenerContext httpContext)
        {
            if (Context.UseSecure) {
                // Auto-Redirect HTTP to HTTPS
                if (!string.Equals("https", httpContext.Request.Url.Scheme, StringComparison.OrdinalIgnoreCase)) {
                    RedirectToSecure(httpContext);
                    return;
                }
            }

            var path = httpContext.Request.Url.AbsolutePath.TrimEnd('/');

            Console.WriteLine($"Request received from '{httpContext.Request.RemoteEndPoint}' -> '{path}'.");

            var root = Context.ListenUri.AbsolutePath.TrimEnd('/');

            if (path.StartsWith(root))
                path = path.Substring(root.Length);

            if (path.Length == 0)
                path = "/";

            HttpHandlerResult result = null;
            var tokenSource = new CancellationTokenSource();
            try {
                result = (await GetRouteResult(httpContext, path))
                    ?? HttpHandlerResult.NotFound(Context)
                        .SetText($"No content found matching path '{path}'!");

                await result.ApplyAsync(httpContext, tokenSource.Token);
            }
            catch (Exception) {
                tokenSource.Cancel();
                throw;
            }
            finally {
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

            // Http Route
            HttpRouteDescription routeDesc;
            if (Routes.FindRoute(path, out routeDesc)) {
                if (routeDesc.IsSecure && !AuthorizeRoute(httpContext)) {
                    return Context.SecurityMgr.OnUnauthorized(httpContext, Context)
                        ?? HttpHandlerResult.Unauthorized(Context);
                }

                try {
                    var handler = Routes.GetHandler(routeDesc, httpContext, Context);
                    if (handler == null) return null;

                    return await Routes.ExecuteAsync(handler);
                }
                catch (Exception error) {
                    return HttpHandlerResult.Exception(Context, error);
                }
            }

            // Not Found
            return null;
        }

        private bool AuthorizeRoute(HttpListenerContext httpContext)
        {
            if (Context.SecurityMgr == null) return false;

            var token = Context.SecurityMgr.GetContextToken(httpContext);
            if (string.IsNullOrEmpty(token)) return false;

            var user = Context.SecurityMgr.Authorize(token);
            return user != null;
        }

        private HttpHandlerResult ProcessContent(HttpListenerContext context, string localPath, ContentDirectory directory)
        {
            if (Path.DirectorySeparatorChar != '/')
                localPath = localPath.Replace('/', Path.DirectorySeparatorChar);

            var filename = Path.Combine(directory.DirectoryPath, localPath);

            if (!File.Exists(filename))
                return HttpHandlerResult.NotFound(Context)
                    .SetText($"File not found! '{filename}'");

            return HttpHandlerResult.File(Context, filename);
        }

        private void RedirectToSecure(HttpListenerContext httpContext)
        {
            var uriBuilder = new StringBuilder()
                .Append("https://").Append(httpContext.Request.Url.Host);

            if (Context.SecurePort != 443)
                uriBuilder.Append(':').Append(Context.SecurePort);

            uriBuilder.Append(httpContext.Request.RawUrl);

            var newUrl = uriBuilder.ToString();
            httpContext.Response.Redirect(newUrl);
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

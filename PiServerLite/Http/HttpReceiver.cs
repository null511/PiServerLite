using PiServerLite.Extensions;
using PiServerLite.Http.Content;
using PiServerLite.Http.Handlers;
using PiServerLite.Http.Routes;
using PiServerLite.Http.Security;
using System;
using System.Collections.Generic;
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
        /// Occurs when an exception is thrown by the
        /// underlying <see cref="HttpListener"/>.
        /// </summary>
        public event EventHandler HttpError;

        /// <summary>
        /// Occurs when an exception is thrown when an uncaught exception
        /// is raised by an <see cref="IHttpHandler"/> implementation.
        /// </summary>
        public event EventHandler ServerError;

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
        /// Collection of routes that overrides <see cref="Routes"/>
        /// using filters.
        /// </summary>
        public List<HttpRouteOverride> RouteOverrides {get;}

        /// <summary>
        /// Creates an instance of <see cref="HttpReceiver"/> with no attached prefixes.
        /// </summary>
        /// <param name="context"></param>
        public HttpReceiver(HttpReceiverContext context)
        {
            this.Context = context;

            Listener = new HttpListener();
            Routes = new HttpRouteCollection();
            RouteOverrides = new List<HttpRouteOverride>();
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
            try {
                Listener.Stop();
            }
            catch (ObjectDisposedException) {}
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
                if (!Listener.IsListening) return;
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
                OnServerError(error);
            }
        }

        private async Task RouteRequest(HttpListenerContext httpContext)
        {
            // Auto-Redirect HTTP to HTTPS if 'HttpsState' is 'Forced'.
            if (Context.Https == HttpsStates.Forced && !string.Equals("https", httpContext.Request.Url.Scheme, StringComparison.OrdinalIgnoreCase)) {
                RedirectToSecure(httpContext);
                return;
            }

            var path = httpContext.Request.Url.AbsolutePath.TrimEnd('/');

            var root = Context.ListenerPath.TrimEnd('/');

            if (path.StartsWith(root))
                path = path.Substring(root.Length);

            if (path.Length == 0)
                path = "/";

            HttpHandlerResult result = null;
            var tokenSource = new CancellationTokenSource();
            try {
                result = await GetRouteResult(httpContext, path, tokenSource.Token)
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

        private async Task<HttpHandlerResult> GetRouteResult(HttpListenerContext httpContext, string path, CancellationToken token)
        {
            var overrideRoute = RouteOverrides.Where(x => x.IsEnabled)
                .FirstOrDefault(x => x.FilterFunc(path));

            // Http Route Overrides with Content
            if (overrideRoute != null && overrideRoute.IncludesContent) {
                if (overrideRoute.IsSecure && Context.SecurityMgr != null) {
                    if (!Context.SecurityMgr.Authorize(httpContext.Request)) {
                        return Context.SecurityMgr.OnUnauthorized(httpContext, Context)
                            ?? HttpHandlerResult.Unauthorized(Context);
                    }
                }

                try {
                    if (overrideRoute.ExecuteEvent == null) return null;

                    return await overrideRoute.ExecuteEvent.Invoke(httpContext, Context);
                }
                catch (Exception error) {
                    return HttpHandlerResult.Exception(Context, error);
                }
            }

            // Content Directories
            var contentRoute = Context.ContentDirectories
                .FirstOrDefault(x => path.StartsWith(x.UrlPath));

            if (contentRoute != null) {
                if (contentRoute.IsSecure && Context.SecurityMgr != null) {
                    if (!Context.SecurityMgr.Authorize(httpContext.Request)) {
                        return Context.SecurityMgr.OnUnauthorized(httpContext, Context)
                            ?? HttpHandlerResult.Unauthorized(Context);
                    }
                }

                var localPath = path.Substring(contentRoute.UrlPath.Length);
                return ProcessContent(httpContext, localPath, contentRoute);
            }

            // Http Route Overrides without Content
            if (overrideRoute != null) {
                if (overrideRoute.IsSecure && Context.SecurityMgr != null) {
                    if (!Context.SecurityMgr.Authorize(httpContext.Request)) {
                        return Context.SecurityMgr.OnUnauthorized(httpContext, Context)
                            ?? HttpHandlerResult.Unauthorized(Context);
                    }
                }

                try {
                    if (overrideRoute.ExecuteEvent == null) return null;

                    return await overrideRoute.ExecuteEvent.Invoke(httpContext, Context);
                }
                catch (Exception error) {
                    return HttpHandlerResult.Exception(Context, error);
                }
            }

            // Http Route
            if (Routes.FindRoute(path, out var routeDesc)) {
                if (routeDesc.IsSecure && Context.SecurityMgr != null) {
                    if (!Context.SecurityMgr.Authorize(httpContext.Request)) {
                        return Context.SecurityMgr.OnUnauthorized(httpContext, Context)
                            ?? HttpHandlerResult.Unauthorized(Context);
                    }
                }

                try {
                    var handler = Routes.GetHandler(routeDesc, httpContext, Context);
                    if (handler == null) return null;

                    return await Routes.ExecuteAsync(handler, token);
                }
                catch (Exception error) {
                    return HttpHandlerResult.Exception(Context, error);
                }
            }

            // Not Found
            return null;
        }

        private HttpHandlerResult ProcessContent(HttpListenerContext context, string localPath, ContentDirectory directory)
        {
            if (Path.DirectorySeparatorChar != '/')
                localPath = localPath.Replace('/', Path.DirectorySeparatorChar);

            var localFilename = Path.Combine(directory.DirectoryPath, localPath);

            // Ensure requested content is within the specified directory
            // IE, prevent relative path hacking
            var fullRootPath = Path.GetFullPath(directory.DirectoryPath);
            var fullLocalFilename = Path.GetFullPath(localFilename);

            if (!fullLocalFilename.StartsWith(fullRootPath))
                return HttpHandlerResult.NotFound(Context)
                    .SetText($"Requested file is outisde of the content directory! [{fullLocalFilename}]");

            var localFile = new FileInfo(fullLocalFilename);

            // Ensure file exists
            if (!localFile.Exists)
                return HttpHandlerResult.NotFound(Context)
                    .SetText($"File not found! [{fullLocalFilename}]");

            // HTTP Caching - Last-Modified
            var ifModifiedSince = context.Request.Headers.Get("If-Modified-Since");
            if (DateTime.TryParse(ifModifiedSince, out var ifModifiedSinceValue)) {
                if (localFile.LastWriteTime.TrimMilliseconds() <= ifModifiedSinceValue)
                    return HttpHandlerResult.Status(Context, HttpStatusCode.NotModified);
            }

            return HttpHandlerResult.File(Context, fullLocalFilename)
                .SetHeader("Last-Modified", localFile.LastWriteTimeUtc.ToString("r"));
        }

        /// <summary>
        /// Redirects an incoming request to HTTPS.
        /// </summary>
        /// <param name="httpContext"></param>
        private void RedirectToSecure(HttpListenerContext httpContext)
        {
            if (Context.HttpsPort <= 0)
                throw new ArgumentOutOfRangeException(nameof(Context.HttpsPort), "Value must be greater than 0!");

            var uriBuilder = new StringBuilder()
                .Append("https://").Append(httpContext.Request.Url.Host);

            if (Context.HttpsPort != 443)
                uriBuilder.Append(':').Append(Context.HttpsPort);

            uriBuilder.Append(httpContext.Request.RawUrl);

            var newUrl = uriBuilder.ToString();
            httpContext.Response.Redirect(newUrl);
        }

        /// <summary>
        /// Raises the <see cref="HttpError"/> event.
        /// </summary>
        protected virtual void OnHttpError(Exception error)
        {
            try {
                HttpError?.Invoke(this, new EventArgs());
            }
            catch {}
        }

        /// <summary>
        /// Raises the <see cref="ServerError"/> event.
        /// </summary>
        protected virtual void OnServerError(Exception error)
        {
            try {
                ServerError?.Invoke(this, new EventArgs());
            }
            catch {}
        }
    }
}

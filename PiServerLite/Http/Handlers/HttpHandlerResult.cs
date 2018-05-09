using PiServerLite.Html;
using PiServerLite.Http.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Http.Handlers
{
    public class HttpHandlerResult : IDisposable
    {
        private readonly HttpReceiverContext context;

        private Stream streamContent;
        private Action<HttpHandlerResult, Stream> contentAction;
        private Func<HttpHandlerResult, Stream, CancellationToken, Task> contentActionAsync;
        private string redirectUrl;

        public int StatusCode {get; set;}
        public string StatusDescription {get; set;}
        public string ContentType {get; set;}
        public long ContentLength {get; set;}
        public bool SendChunked {get; set;}

        public MimeTypeDictionary MimeTypes {get; set;}
        public Dictionary<string, string> Headers {get; set;}


        public HttpHandlerResult(HttpReceiverContext context)
        {
            this.context = context;

            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            streamContent?.Dispose();
        }

        public HttpHandlerResult SetText(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            streamContent = new MemoryStream(data);
            ContentLength = data.LongLength;
            return this;
        }

        public HttpHandlerResult SetContentType(string contentType)
        {
            ContentType = contentType;
            return this;
        }

        public HttpHandlerResult SetHeader(string name, string value)
        {
            Headers[name] = value;
            return this;
        }

        public HttpHandlerResult SetChunked(bool value)
        {
            SendChunked = value;
            return this;
        }

        public HttpHandlerResult SetContent(Stream stream)
        {
            ContentLength = stream.Length;
            streamContent = stream;
            return this;
        }

        public HttpHandlerResult SetContent(Action<HttpHandlerResult, Stream> writeAction)
        {
            contentAction = writeAction;
            return this;
        }

        public HttpHandlerResult SetContent(Func<HttpHandlerResult, Stream, Task> writeActionAsync)
        {
            contentActionAsync = async (response, stream, token) => await writeActionAsync(response, stream);
            return this;
        }

        public HttpHandlerResult SetContent(Func<HttpHandlerResult, Stream, CancellationToken, Task> writeActionAsync)
        {
            contentActionAsync = async (response, stream, token) => await writeActionAsync(response, stream, token);
            return this;
        }

        internal void Apply(HttpListenerContext context)
        {
            if (!string.IsNullOrEmpty(redirectUrl)) {
                context.Response.Redirect(redirectUrl);
                return;
            }

            context.Response.StatusCode = StatusCode;
            context.Response.StatusDescription = StatusDescription;
            context.Response.ContentType = ContentType;
            context.Response.SendChunked = SendChunked;

            foreach (var headerKey in Headers.Keys)
                context.Response.Headers[headerKey] = Headers[headerKey];

            if (!SendChunked)
                context.Response.ContentLength64 = ContentLength;

            if (contentActionAsync != null) {
                var tokenSource = new CancellationTokenSource();
                try {
                    var token = tokenSource.Token;
                    contentActionAsync.Invoke(this, context.Response.OutputStream, token)
                        .GetAwaiter().GetResult();
                }
                catch (Exception) {
                    tokenSource.Cancel();
                    throw;
                }
                finally {
                    tokenSource.Dispose();
                }
            }
            else if (contentAction != null) {
                contentAction.Invoke(this, context.Response.OutputStream);
            }
            else if (streamContent != null) {
                streamContent.Seek(0, SeekOrigin.Begin);
                streamContent.CopyTo(context.Response.OutputStream);
            }
        }

        internal async Task ApplyAsync(HttpListenerContext context, CancellationToken token)
        {
            if (!string.IsNullOrEmpty(redirectUrl)) {
                context.Response.Redirect(redirectUrl);
                return;
            }

            context.Response.StatusCode = StatusCode;
            context.Response.StatusDescription = StatusDescription;
            context.Response.ContentType = ContentType;
            context.Response.SendChunked = SendChunked;

            foreach (var headerKey in Headers.Keys)
                context.Response.Headers[headerKey] = Headers[headerKey];

            if (!SendChunked)
                context.Response.ContentLength64 = ContentLength;

            if (contentActionAsync != null) {
                await contentActionAsync.Invoke(this, context.Response.OutputStream, token);
            }
            else if (contentAction != null) {
                await Task.Run(() => contentAction.Invoke(this, context.Response.OutputStream), token);
            }
            else if (streamContent != null) {
                streamContent.Seek(0, SeekOrigin.Begin);
                await streamContent.CopyToAsync(context.Response.OutputStream);
                await context.Response.OutputStream.FlushAsync(token);
            }
        }

        public static HttpHandlerResult Status(HttpReceiverContext context, HttpStatusCode statusCode)
        {
            return new HttpHandlerResult(context) {
                StatusCode = (int)statusCode,
                StatusDescription = statusCode.ToString(),
            };
        }

        public static HttpHandlerResult Ok(HttpReceiverContext context)
        {
            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.OK,
                StatusDescription = "OK.",
            };
        }

        public static HttpHandlerResult NotFound(HttpReceiverContext context)
        {
            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.NotFound,
                StatusDescription = "Not Found!",
            }.SetText("404 - Not Found");
        }

        /// <summary>
        /// Return a [400] HTTP Bad Request response.
        /// </summary>
        /// <returns>HTTP [400] Bad Request.</returns>
        public static HttpHandlerResult BadRequest(HttpReceiverContext context)
        {
            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.BadRequest,
                StatusDescription = "Bad Request!",
            };
        }

        /// <summary>
        /// Return a [401] HTTP Unauthorized response.
        /// </summary>
        /// <returns>HTTP [401] Unauthorized.</returns>
        public static HttpHandlerResult Unauthorized(HttpReceiverContext context)
        {
            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                StatusDescription = "Unauthorized!",
            };
        }

        /// <summary>
        /// Redirect the response to a relative path.
        /// </summary>
        /// <returns>HTTP [302] Redirect.</returns>
        public static HttpHandlerResult Redirect(HttpReceiverContext context, string path, object queryArgs = null)
        {
            var urlUtility = new UrlUtility(context);
            var url = urlUtility.GetRelative(path, queryArgs);

            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.Redirect,
                redirectUrl = url,
            };
        }

        /// <summary>
        /// Redirect the response to an absolute URL.
        /// </summary>
        /// <returns>HTTP [302] Redirect.</returns>
        public static HttpHandlerResult RedirectUrl(HttpReceiverContext context, string url)
        {
            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.Redirect,
                redirectUrl = url,
            };
        }

        public static HttpHandlerResult File(HttpReceiverContext context, string filename)
        {
            var ext = Path.GetExtension(filename);

            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.OK,
                StatusDescription = "OK.",
                ContentType = context.MimeTypes.Get(ext),
                ContentLength = new FileInfo(filename).Length,
            }.SetContent(async (response, responseStream, token) => {
                using (var stream = System.IO.File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    response.ContentLength = stream.Length - stream.Position;
                    await stream.CopyToAsync(responseStream);
                }
            });
        }

        /// <summary>
        /// Return a [500] HTTP Internal Server Error response.
        /// </summary>
        /// <returns>HTTP [500] Internal Server Error.</returns>
        public static HttpHandlerResult Exception(HttpReceiverContext context, Exception error)
        {
            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                StatusDescription = "Internal Server Error!",
            }.SetText(error.ToString());
        }

        /// <summary>
        /// Returns a named view from the <see cref="ViewCollection"/>.
        /// </summary>
        /// <param name="name">The name of the view.</param>
        /// <param name="param">Optional view-model object.</param>
        public static HttpHandlerResult View(HttpReceiverContext context, string name, object param = null)
        {
            if (!context.Views.TryFind(name, out var content))
                throw new ApplicationException($"View '{name}' was not found!");

            var engine = new HtmlEngine(context.Views) {
                UrlRoot = context.ListenerPath,
            };

            content = engine.Process(content, param);

            return new HttpHandlerResult(context) {
                StatusCode = (int)HttpStatusCode.OK,
                StatusDescription = "OK.",
            }.SetText(content);
        }
    }
}

using PiServerLite.Html;
using System;
using System.IO;
using System.Net;

namespace PiServerLite.Http
{
    public class HttpHandlerResult
    {
        private string content;
        private Action<Stream> contentAction;
        private string redirectUrl;

        public int StatusCode {get; set;}
        public string StatusDescription {get; set;}
        public string ContentType {get; set;}

        public MimeTypeDictionary MimeTypes {get; set;}


        public HttpHandlerResult SetText(string text)
        {
            content = text;
            return this;
        }

        public HttpHandlerResult SetContentType(string contentType)
        {
            ContentType = contentType;
            return this;
        }

        public HttpHandlerResult SetContent(Action<Stream> writeAction)
        {
            contentAction = writeAction;
            return this;
        }

        public void Apply(HttpListenerContext context)
        {
            if (!string.IsNullOrEmpty(redirectUrl)) {
                context.Response.Redirect(redirectUrl);
                return;
            }

            context.Response.StatusCode = StatusCode;
            context.Response.StatusDescription = StatusDescription;
            context.Response.ContentType = ContentType;

            if (contentAction != null) {
                contentAction.Invoke(context.Response.OutputStream);
            }
            else if (content != null) {
                using (var writer = new StreamWriter(context.Response.OutputStream)) {
                    writer.Write(content);
                }
            }
        }

        public static HttpHandlerResult Ok()
        {
            return new HttpHandlerResult() {
                StatusCode = (int)HttpStatusCode.OK,
                StatusDescription = "OK.",
            };
        }

        public static HttpHandlerResult NotFound()
        {
            return new HttpHandlerResult() {
                StatusCode = (int)HttpStatusCode.NotFound,
                StatusDescription = "Not Found!",
                content = "404 - Not Found",
            };
        }

        public static HttpHandlerResult BadRequest()
        {
            return new HttpHandlerResult() {
                StatusCode = (int)HttpStatusCode.BadRequest,
                StatusDescription = "Bad Request!",
            };
        }

        public static HttpHandlerResult Redirect(string url)
        {
            return new HttpHandlerResult() {
                StatusCode = (int)HttpStatusCode.Redirect,
                redirectUrl = url,
            };
        }

        public static HttpHandlerResult File(HttpReceiverContext context, string filename)
        {
            var ext = Path.GetExtension(filename);

            return new HttpHandlerResult() {
                StatusCode = (int)HttpStatusCode.OK,
                StatusDescription = "OK.",
                ContentType = context.MimeTypes.Get(ext),
            }.SetContent(responseStream => {
                using (var stream = System.IO.File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    stream.CopyTo(responseStream);
                }
            });
        }

        public static HttpHandlerResult Exception(Exception error)
        {
            return new HttpHandlerResult() {
                StatusCode = (int)HttpStatusCode.NotFound,
                StatusDescription = "Not Found!",
                content = error.ToString(),
            };
        }

        public static HttpHandlerResult View(HttpReceiverContext context, string name, object param = null)
        {
            string content;
            if (!context.Views.TryFind(name, out content))
                throw new ApplicationException($"View '{name}' was not found!");

            var engine = new HtmlEngine(context.Views) {
                UrlRoot = context.UrlRoot,
            };

            content = engine.Process(content, param);

            return new HttpHandlerResult {
                StatusCode = (int)HttpStatusCode.OK,
                StatusDescription = "OK.",
                content = content,
            };
        }
    }
}

using System;
using System.Net;

namespace PiServerLite.Http.Handlers
{
    public class ResponseBuilder
    {
        private readonly IHttpHandler handler;


        public ResponseBuilder(IHttpHandler handler)
        {
            this.handler = handler;
        }

        /// <summary>
        /// Creates an empty HTTP '200 OK' response.
        /// </summary>
        public HttpHandlerResult Ok()
        {
            return HttpHandlerResult.Ok();
        }

        /// <summary>
        /// Creates an empty HTTP response with the
        /// specified <paramref name="statusCode"/>.
        /// </summary>
        public HttpHandlerResult Status(HttpStatusCode statusCode)
        {
            return HttpHandlerResult.Status(statusCode);
        }

        /// <summary>
        /// Creates an empty HTTP '404 Not Found' response.
        /// </summary>
        public HttpHandlerResult NotFound()
        {
            return HttpHandlerResult.NotFound();
        }

        /// <summary>
        /// Creates an empty HTTP '400 Bad Request' response.
        /// </summary>
        public HttpHandlerResult BadRequest()
        {
            return HttpHandlerResult.BadRequest();
        }

        /// <summary>
        /// Creates a response that redirects to the specified relative path.
        /// </summary>
        public HttpHandlerResult Redirect(string path, object queryArgs = null)
        {
            return HttpHandlerResult.Redirect(handler.Context, path, queryArgs);
        }

        /// <summary>
        /// Creates a response that redirects to the specified absolute URL.
        /// </summary>
        public HttpHandlerResult RedirectUrl(string url)
        {
            return HttpHandlerResult.RedirectUrl(url);
        }

        /// <summary>
        /// Creates an empty HTTP '500 Internal Server Error' response.
        /// </summary>
        public HttpHandlerResult Exception(Exception error)
        {
            return HttpHandlerResult.Exception(error);
        }

        /// <summary>
        /// Creates a response that returns an HTML view.
        /// </summary>
        public HttpHandlerResult View(string name, object param = null)
        {
            return HttpHandlerResult.View(handler.Context, name, param);
        }

        /// <summary>
        /// Creates a response that returns a File.
        /// </summary>
        public HttpHandlerResult File(string filename)
        {
            return HttpHandlerResult.File(handler.Context, filename);
        }
    }
}

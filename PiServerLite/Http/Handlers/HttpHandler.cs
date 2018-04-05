using PiServerLite.Extensions;
using PiServerLite.Html;
using System;
using System.Net;
using System.Web;

namespace PiServerLite.Http.Handlers
{
    public abstract class HttpHandler : IHttpHandler
    {
        public HttpListenerContext HttpContext {get; set;}
        public HttpReceiverContext Context {get; set;}

        public RequestAs As => new RequestAs(HttpContext.Request);
        public UrlUtility Urls => new UrlUtility(Context);


        public virtual void OnRequestReceived() {}

        //-----------------------------
        #region Methods

        public virtual HttpHandlerResult Get()
        {
            return NotFound();
        }

        public virtual HttpHandlerResult Post()
        {
            return NotFound();
        }

        public virtual HttpHandlerResult Head()
        {
            return NotFound();
        }

        public virtual HttpHandlerResult Options()
        {
            return NotFound();
        }

        #endregion
        //-----------------------------
        #region Results

        /// <summary>
        /// Creates an empty HTTP '200 OK' response.
        /// </summary>
        public HttpHandlerResult Ok()
        {
            return HttpHandlerResult.Ok(Context);
        }

        /// <summary>
        /// Creates an empty HTTP response with the
        /// specified <paramref name="statusCode"/>.
        /// </summary>
        public HttpHandlerResult Status(HttpStatusCode statusCode)
        {
            return HttpHandlerResult.Status(Context, statusCode);
        }

        /// <summary>
        /// Creates an empty HTTP '404 Not Found' response.
        /// </summary>
        public HttpHandlerResult NotFound()
        {
            return HttpHandlerResult.NotFound(Context);
        }

        /// <summary>
        /// Creates an empty HTTP '400 Bad Request' response.
        /// </summary>
        public HttpHandlerResult BadRequest()
        {
            return HttpHandlerResult.BadRequest(Context);
        }

        /// <summary>
        /// Creates a response that redirects to the specified relative path.
        /// </summary>
        public HttpHandlerResult Redirect(string path, object queryArgs = null)
        {
            return HttpHandlerResult.Redirect(Context, path, queryArgs);
        }

        /// <summary>
        /// Creates a response that redirects to the specified absolute URL.
        /// </summary>
        public HttpHandlerResult RedirectUrl(string url)
        {
            return HttpHandlerResult.RedirectUrl(Context, url);
        }

        /// <summary>
        /// Creates an empty HTTP '500 Internal Server Error' response.
        /// </summary>
        public HttpHandlerResult Exception(Exception error)
        {
            return HttpHandlerResult.Exception(Context, error);
        }

        /// <summary>
        /// Creates a response that returns an HTML view.
        /// </summary>
        public HttpHandlerResult View(string name, object param = null)
        {
            return HttpHandlerResult.View(Context, name, param);
        }

        #endregion
        //-----------------------------

        /// <summary>
        /// Gets a value from the query string.
        /// </summary>
        public string GetQuery(string key, string defaultValue = null)
        {
            var value = HttpContext.Request.QueryString.Get(key);
            if (value == null) return defaultValue;
            return HttpUtility.UrlDecode(value);
        }

        /// <summary>
        /// Gets a value from the query string, cast to the specified type.
        /// </summary>
        public T GetQuery<T>(string key, T defaultValue = default(T))
        {
            var value = HttpContext.Request.QueryString.Get(key);
            if (value == null) return defaultValue;
            return HttpUtility.UrlDecode(value).To<T>();
        }
    }
}

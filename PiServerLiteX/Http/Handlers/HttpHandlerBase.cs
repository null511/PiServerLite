using PiServerLite.Extensions;
using PiServerLite.Html;
using System.Net;
using System.Web;

namespace PiServerLite.Http.Handlers
{
    public abstract class HttpHandlerBase : IHttpHandler
    {
        public HttpListenerContext HttpContext {get; set;}
        public HttpReceiverContext Context {get; set;}

        public RequestAs Request => new RequestAs(HttpContext.Request);
        public ResponseBuilder Response => new ResponseBuilder(this);
        public UrlUtility Urls => new UrlUtility(Context);


        public virtual void OnRequestReceived() {}

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

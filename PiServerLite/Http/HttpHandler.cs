using PiServerLite.Html;
using System;
using System.Net;
using System.Web;

namespace PiServerLite.Http
{
    public abstract class HttpHandler : IHttpHandler
    {
        public HttpListenerContext HttpContext {get; set;}
        public HttpReceiverContext Context {get; set;}

        public RequestAs As => new RequestAs(HttpContext.Request);
        public UrlUtility Urls => new UrlUtility(Context.ListenUri);


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

        public HttpHandlerResult Ok()
        {
            return HttpHandlerResult.Ok();
        }

        public HttpHandlerResult NotFound()
        {
            return HttpHandlerResult.NotFound();
        }

        public HttpHandlerResult BadRequest()
        {
            return HttpHandlerResult.BadRequest();
        }

        public HttpHandlerResult Redirect(string url)
        {
            return HttpHandlerResult.Redirect(url);
        }

        public HttpHandlerResult Exception(Exception error)
        {
            return HttpHandlerResult.Exception(error);
        }

        public HttpHandlerResult View(string name, object param = null)
        {
            return HttpHandlerResult.View(Context, name, param);
        }

        #endregion
        //-----------------------------

        public string GetQuery(string key, string defaultValue = null)
        {
            var value = HttpContext.Request.QueryString.Get(key);
            if (value == null) return defaultValue;
            return HttpUtility.UrlDecode(value);
        }

        public T GetQuery<T>(string key, T defaultValue = default(T))
        {
            var value = HttpContext.Request.QueryString.Get(key);
            if (value == null) return defaultValue;
            return HttpUtility.UrlDecode(value).To<T>();
        }
    }
}

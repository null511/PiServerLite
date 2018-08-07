using PiServerLite.Html;
using System.Net;

namespace PiServerLite.Http.Handlers
{
    public interface IHttpHandler
    {
        HttpListenerContext HttpContext {get; set;}
        HttpReceiverContext Context {get; set;}
        RequestAs Request {get;}
        ResponseBuilder Response {get;}
        UrlUtility Urls {get;}

        void OnRequestReceived();
    }
}

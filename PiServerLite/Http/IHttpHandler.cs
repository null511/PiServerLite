using PiServerLite.Html;
using System.Net;

namespace PiServerLite.Http
{
    public interface IHttpHandler
    {
        HttpListenerContext HttpContext {get; set;}
        HttpReceiverContext Context {get; set;}
        UrlUtility Urls {get;}

        void OnRequestReceived();
    }
}

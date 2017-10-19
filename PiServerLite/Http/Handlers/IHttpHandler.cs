using PiServerLite.Html;
using System.Net;

namespace PiServerLite.Http.Handlers
{
    public interface IHttpHandler
    {
        HttpListenerContext HttpContext {get; set;}
        HttpReceiverContext Context {get; set;}
        UrlUtility Urls {get;}

        void OnRequestReceived();
    }
}

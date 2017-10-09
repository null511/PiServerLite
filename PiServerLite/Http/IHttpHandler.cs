using System.Net;

namespace PiServerLite.Http
{
    interface IHttpHandler
    {
        HttpListenerContext HttpContext {get; set;}
        HttpReceiverContext Context {get; set;}

        //RequestAs As {get;}
    }
}

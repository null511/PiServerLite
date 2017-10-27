using PiServerLite.Http.Handlers;
using System.Net;

namespace PiServerLite.Http.Security
{
    public interface ISecurityManager
    {
        bool Authorize(HttpListenerRequest request);

        HttpHandlerResult OnUnauthorized(HttpListenerContext httpContext, HttpReceiverContext context);
    }
}

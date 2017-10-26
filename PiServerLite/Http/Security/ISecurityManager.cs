using PiServerLite.Http.Handlers;
using System.Net;

namespace PiServerLite.Http.Security
{
    public interface ISecurityManager
    {
        bool Authenticate(ISecurityUser user, out string token);
        bool Authorize(string token);
        ISecurityUser GetUser(string token);
        HttpHandlerResult OnUnauthorized(HttpListenerContext httpContext, HttpReceiverContext context);
        void SignOut(HttpListenerContext httpContext);

        string GetContextToken(HttpListenerContext httpContext);
        void SetContextToken(HttpListenerContext httpContext, string token);
    }
}

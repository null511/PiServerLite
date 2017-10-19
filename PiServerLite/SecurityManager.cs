using PiServerLite.Http;
using System;
using System.Collections.Concurrent;
using System.Net;

namespace PiServerLite
{
    class SecurityManager : ISecurityManager
    {
        private readonly ConcurrentDictionary<string, ISecurityUser> userTokenMap;


        public SecurityManager()
        {
            userTokenMap = new ConcurrentDictionary<string, ISecurityUser>(StringComparer.OrdinalIgnoreCase);
        }

        public bool Authenticate(ISecurityUser user, out string token)
        {
            if (!string.Equals(user.Username, "admin") || !string.Equals(user.Password, "password")) {
                token = null;
                return false;
            }

            token = Guid.NewGuid().ToString("N");
            userTokenMap[token] = user;
            return true;
        }

        public ISecurityUser Authorize(string token)
        {
            ISecurityUser user;
            if (!userTokenMap.TryGetValue(token, out user))
                return null;

            return user;
        }

        public string GetContextToken(HttpListenerContext httpContext)
        {
            return httpContext.Request.Cookies["AUTH"].Value;
        }

        public void SetContextToken(HttpListenerContext httpContext, string token)
        {
            var cookie = new Cookie("AUTH", token) {
                Expires = DateTime.Now.AddYears(1)
            };

            httpContext.Response.SetCookie(cookie);
        }
    }
}

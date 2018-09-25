using System;

namespace PiServerLite.Http.Security
{
    /// <inheritdoc />
    /// <summary>
    /// Marks an <see cref="T:PiServerLite.Http.Handlers.IHttpHandler" /> as requiring authorization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SecureAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether the request should be redirected to the login handler.
        /// </summary>
        public bool RedirectToLogin {get; set;}


        public SecureAttribute()
        {
            RedirectToLogin = true;
        }
    }
}

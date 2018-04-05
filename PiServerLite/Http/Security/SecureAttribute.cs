using System;

namespace PiServerLite.Http.Security
{
    /// <inheritdoc />
    /// <summary>
    /// Marks an <see cref="T:PiServerLite.Http.Handlers.IHttpHandler" /> as requiring authorization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SecureAttribute : Attribute {}
}

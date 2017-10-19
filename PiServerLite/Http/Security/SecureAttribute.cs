using System;

namespace PiServerLite.Http.Security
{
    /// <summary>
    /// Marks an <see cref="IHttpHandler"/> as requiring authorization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SecureAttribute : Attribute {}
}

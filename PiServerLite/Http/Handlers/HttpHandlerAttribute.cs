using System;

namespace PiServerLite.Http.Handlers
{
    /// <summary>
    /// Maps a network path to an <see cref="IHttpHandler"/> implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class HttpHandlerAttribute : Attribute
    {
        /// <summary>
        /// Gets the network path attached to this handler.
        /// </summary>
        public string Path {get;}


        /// <summary>
        /// Creates a new instance of an <see cref="HttpHandlerAttribute"/>
        /// mapped to the provided network path.
        /// </summary>
        /// <param name="path"></param>
        public HttpHandlerAttribute(string path)
        {
            this.Path = path;
        }
    }
}

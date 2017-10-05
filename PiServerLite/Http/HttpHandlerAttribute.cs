using System;

namespace PiServerLite.Http
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class HttpHandlerAttribute : Attribute
    {
        public string Path {get; set;}


        public HttpHandlerAttribute(string path)
        {
            this.Path = path;
        }
    }
}

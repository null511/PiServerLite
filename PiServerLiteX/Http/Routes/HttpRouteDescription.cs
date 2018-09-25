using System;

namespace PiServerLite.Http.Routes
{
    internal class HttpRouteDescription
    {
        public Type ClassType {get; set;}
        public bool IsSecure {get; set;}
    }
}

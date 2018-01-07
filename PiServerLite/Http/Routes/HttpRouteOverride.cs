using PiServerLite.Http.Handlers;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PiServerLite.Http.Routes
{
    public delegate Task<HttpHandlerResult> RouteOverrideExecuteEventHandler(HttpListenerContext httpContext, HttpReceiverContext context);

    public class HttpRouteOverride
    {
        /// <summary>
        /// Gets or Sets whether this route override is enabled.
        /// </summary>
        public bool IsEnabled {get; set;}

        /// <summary>
        /// Gets or Sets whether this route override requires authorization.
        /// </summary>
        public bool IsSecure {get; set;}

        /// <summary>
        /// Gets or Sets the function used to filter paths which this
        /// route should override.
        /// </summary>
        public Func<string, bool> FilterFunc {get; set;}

        /// <summary>
        /// Gets or Sets the route event that is fired when a route override occurs.
        /// </summary>
        public RouteOverrideExecuteEventHandler ExecuteEvent {get; set;}
    }
}

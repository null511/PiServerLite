using PiServerLite.Http.Handlers;

namespace PiServerLite.Sample.Handlers
{
    [HttpHandler("/")]
    [HttpHandler("/index")]
    internal class IndexHandler : HttpHandler
    {
        public override HttpHandlerResult Get()
        {
            return Response.View("Index.html");
        }
    }
}

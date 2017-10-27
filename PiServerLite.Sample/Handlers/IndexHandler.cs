using PiServerLite.Http.Handlers;

namespace PiServerLite.Sample.Handlers
{
    [HttpHandler("/")]
    [HttpHandler("/index")]
    class IndexHandler : HttpHandler
    {
        public override HttpHandlerResult Get()
        {
            return View("Index.html");
        }
    }
}

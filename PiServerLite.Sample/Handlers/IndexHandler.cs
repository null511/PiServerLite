using PiServerLite.Http;

namespace PiServerLite.Sample.Handlers
{
    [HttpHandler("/index")]
    class IndexHandler : HttpHandler
    {
        public override HttpHandlerResult Get()
        {
            return View("Index.html");
        }
    }
}

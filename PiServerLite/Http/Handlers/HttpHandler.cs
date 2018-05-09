namespace PiServerLite.Http.Handlers
{
    public abstract class HttpHandler : HttpHandlerBase
    {
        public virtual HttpHandlerResult Get()
        {
            return Response.NotFound();
        }

        public virtual HttpHandlerResult Post()
        {
            return Response.NotFound();
        }

        public virtual HttpHandlerResult Head()
        {
            return Response.NotFound();
        }

        public virtual HttpHandlerResult Options()
        {
            return Response.NotFound();
        }
    }
}

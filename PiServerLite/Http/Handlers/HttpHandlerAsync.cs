using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Http.Handlers
{
    public abstract class HttpHandlerAsync : HttpHandlerBase
    {
        public virtual async Task<HttpHandlerResult> GetAsync(CancellationToken token)
        {
            return await Task.Run(() => Response.NotFound(), token);
        }

        public virtual async Task<HttpHandlerResult> PostAsync(CancellationToken token)
        {
            return await Task.Run(() => Response.NotFound(), token);
        }

        public virtual async Task<HttpHandlerResult> HeadAsync(CancellationToken token)
        {
            return await Task.Run(() => Response.NotFound(), token);
        }

        public virtual async Task<HttpHandlerResult> OptionsAsync(CancellationToken token)
        {
            return await Task.Run(() => Response.NotFound(), token);
        }
    }
}

using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace PiServerLite.Http
{
    public class RequestAs
    {
        private readonly HttpListenerRequest request;


        public RequestAs(HttpListenerRequest request)
        {
            this.request = request;
        }

        public string Text()
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding)) {
                return reader.ReadToEnd();
            }
        }

        public async Task<string> TextAsync()
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding)) {
                return await reader.ReadToEndAsync();
            }
        }

        public NameValueCollection FormData()
        {
            return HttpUtility.ParseQueryString(Text());
        }
    }
}

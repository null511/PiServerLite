using PiServerLite.Extensions;
using PiServerLite.Http;
using System.Text;
using System.Web;

namespace PiServerLite.Html
{
    /// <summary>
    /// Utility class for generating full URLs using a root path.
    /// </summary>
    public class UrlUtility
    {
        public string RootPath {get;}


        public UrlUtility() {}

        public UrlUtility(HttpReceiverContext context)
        {
            this.RootPath = context.ListenerPath;
        }

        /// <summary>
        /// Creates a full URL by joining <see cref="RootPath"/> and
        /// <paramref name="path"/>, with optional <paramref name="queryArgs"/>.
        /// </summary>
        public string GetRelative(string path, object queryArgs = null)
        {
            var url = NetPath.Combine(RootPath, path);

            if (queryArgs == null) return url;

            var argList = ObjectExtensions.ToDictionary(queryArgs);

            var builder = new StringBuilder();
            foreach (var arg in argList) {
                var eKey = HttpUtility.UrlEncode(arg.Key);
                var eValue = HttpUtility.UrlEncode(arg.Value?.ToString() ?? string.Empty);

                builder.Append(builder.Length > 0 ? "&" : "?");
                builder.Append(eKey);
                builder.Append('=');
                builder.Append(eValue);
            }

            url += builder.ToString();

            return url;
        }
    }
}

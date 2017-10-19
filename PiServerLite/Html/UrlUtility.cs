using PiServerLite.Extensions;
using System;
using System.Text;
using System.Web;

namespace PiServerLite.Html
{
    public class UrlUtility
    {
        public string Host {get;}
        public string Root {get;}


        public UrlUtility() {}

        public UrlUtility(Uri uri)
        {
            this.Host = uri.Host;
            this.Root = uri.AbsolutePath;
        }

        public string GetRelative(string path, object queryArgs = null)
        {
            var url = NetPath.Combine(Root, path);

            if (queryArgs != null) {
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
            }

            return url;
        }
    }
}

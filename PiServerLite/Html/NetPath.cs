using System.Text;

namespace PiServerLite.Html
{
    internal static class NetPath
    {
        public static string Combine(params string[] paths)
        {
            var result = new StringBuilder();

            for (var i = 0; i < paths.Length; i++) {
                var p = paths[i];

                if (i > 0 && result[result.Length - 1] != '/' && !p.StartsWith("/"))
                    result.Append('/');

                result.Append(p);
            }

            return result.ToString();
        }
    }
}

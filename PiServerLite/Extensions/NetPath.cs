using System.Text;

namespace PiServerLite.Extensions
{
    /// <summary>
    /// Utility class for modifying network paths.
    /// </summary>
    public static class NetPath
    {
        /// <summary>
        /// Combines a series of relative paths.
        /// </summary>
        public static string Combine(params string[] paths)
        {
            var result = new StringBuilder();

            foreach (var path in paths) {
                if (result.Length > 0) {
                    var lastChar = result[result.Length - 1];

                    if (lastChar == '/') {
                        if (path.StartsWith("/"))
                            result.Append(path.Substring(1));
                        else
                            result.Append(path);
                    }
                    else {
                        if (!path.StartsWith("/"))
                            result.Append('/');

                        result.Append(path);
                    }
                }
                else {
                    result.Append(path);
                }
            }

            return result.ToString();
        }
    }
}

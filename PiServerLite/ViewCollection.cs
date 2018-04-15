using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PiServerLite
{
    public class ViewCollection
    {
        private readonly ConcurrentDictionary<string, string> viewCacheList;
        private readonly Dictionary<string, Func<string>> viewList;


        public ViewCollection()
        {
            viewCacheList = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            viewList = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryFind(string viewName, out string content)
        {
            if (viewCacheList.TryGetValue(viewName, out content))
                return true;

            if (viewList.TryGetValue(viewName, out var getFunc)) {
                content = getFunc();
                viewCacheList[viewName] = content;
                return true;
            }

            return false;
        }

        public ViewCollection Add(string viewName, Func<string> getFunc)
        {
            viewList[viewName] = getFunc;
            return this;
        }

        public ViewCollection AddFromExternal(string filename, string viewName = null)
        {
            viewList[viewName ?? filename] = () => LoadFromExternal(filename);
            return this;
        }

        public ViewCollection AddFolderFromExternal(string path, string prefix = null, string filter = "*.html", bool recursive = true)
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filename in Directory.EnumerateFiles(path, filter, option)) {
                var viewName = filename.Substring(path.Length);
                viewName = viewName.TrimStart(Path.DirectorySeparatorChar);

                if (Path.DirectorySeparatorChar != '\\')
                    viewName = viewName.Replace(Path.DirectorySeparatorChar, '\\');

                if (!string.IsNullOrEmpty(prefix))
                    viewName = prefix+viewName;

                viewList[viewName] = () => LoadFromExternal(filename);
            }

            return this;
        }

        /// <summary>
        /// Adds a single view to the collection using the specified path.
        /// </summary>
        /// <param name="assembly">The assembly where the views are located.</param>
        /// <param name="path">The assembly path to the view.</param>
        /// <param name="viewName">The unique key used to retrieve the view.</param>
        public ViewCollection AddFromAssembly(Assembly assembly, string path, string viewName = null)
        {
            viewList[viewName ?? path] = () => LoadFromAssembly(assembly, path);
            return this;
        }

        /// <summary>
        /// Adds a collection of views found under the specified root assembly path.
        /// </summary>
        /// <param name="assembly">The assembly where the views are located.</param>
        /// <param name="rootPath">The root path of the views.</param>
        public ViewCollection AddAllFromAssembly(Assembly assembly, string rootPath)
        {
            var resourceNameArray = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNameArray) {
                if (!resourceName.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var x = rootPath.Length;
                if (resourceName.Length >= x+1 && resourceName[x] == '.') x++;

                var localName = resourceName.Substring(x);

                viewList[localName] = () => LoadFromAssembly(assembly, resourceName);
            }

            return this;
        }

        private static string LoadFromExternal(string filename)
        {
            if (Path.DirectorySeparatorChar != '\\')
                filename = filename.Replace("\\", Path.DirectorySeparatorChar.ToString());

            filename = Path.GetFullPath(filename);

            if (!File.Exists(filename))
                throw new ApplicationException($"External View '{filename}' could not be found!");

            using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }

        private static string LoadFromAssembly(Assembly assembly, string path)
        {
            using (var stream = assembly.GetManifestResourceStream(path)) {
                if (stream == null)
                    throw new ApplicationException($"Assembly View '{path}' could not be found!");

                using (var reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PiServerLite
{
    public class ViewCollection
    {
        private readonly ViewCache viewCache;
        private readonly Dictionary<string, Func<string>> viewList;


        public ViewCollection()
        {
            viewCache = new ViewCache();
            viewList = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryFind(string viewName, out string content)
        {
            if (!viewList.TryGetValue(viewName, out var getFunc)) {
                content = null;
                return false;
            }

            content = getFunc();
            return true;
        }

        public ViewCollection Add(string viewName, Func<string> getFunc)
        {
            viewList[viewName] = getFunc;
            return this;
        }

        public ViewCollection AddFromExternal(string filename, string viewName = null)
        {
            var viewKey = viewName ?? filename;
            viewList[viewKey] = () => LoadFromExternal(viewKey, filename);
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

                viewList[viewName] = () => LoadFromExternal(viewName, filename);
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
            var viewKey = viewName ?? path;
            viewList[viewKey] = () => LoadFromAssembly(viewKey, assembly, path);
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

                viewList[localName] = () => LoadFromAssembly(localName, assembly, resourceName);
            }

            return this;
        }

        private string LoadFromExternal(string viewKey, string filename)
        {
            if (viewCache.TryFind(viewKey, out var viewData))
                return viewData;

            if (Path.DirectorySeparatorChar != '\\')
                filename = filename.Replace("\\", Path.DirectorySeparatorChar.ToString());

            filename = Path.GetFullPath(filename);
            var fileInfo = new FileInfo(filename);

            if (!fileInfo.Exists)
                throw new ApplicationException($"External View '{filename}' could not be found!");

            var lastWrite = fileInfo.LastWriteTimeUtc;

            var view = new CachedView {
                IsExpiredFunc = () => File.GetLastWriteTimeUtc(filename) != lastWrite,
            };

            using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream)) {
                view.Data = reader.ReadToEnd();
            }

            viewCache.Update(viewKey, view);

            return view.Data;
        }

        private string LoadFromAssembly(string viewKey, Assembly assembly, string path)
        {
            if (viewCache.TryFind(viewKey, out var viewData))
                return viewData;

            var view = new CachedView {
                IsExpiredFunc = () => false,
            };

            using (var stream = assembly.GetManifestResourceStream(path)) {
                if (stream == null)
                    throw new ApplicationException($"Assembly View '{path}' could not be found!");

                using (var reader = new StreamReader(stream)) {
                    view.Data = reader.ReadToEnd();
                }
            }

            viewCache.Update(viewKey, view);

            return view.Data;
        }
    }
}

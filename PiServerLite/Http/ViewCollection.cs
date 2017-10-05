using System;
using System.Collections.Generic;
using System.IO;

namespace PiServerLite.Http
{
    public class ViewCollection
    {
        private readonly Dictionary<string, string> viewCacheList;
        private readonly Dictionary<string, Func<string>> viewGetList;


        public ViewCollection()
        {
            viewCacheList = new Dictionary<string, string>();
            viewGetList = new Dictionary<string, Func<string>>();
        }

        public bool TryFind(string viewName, out string content)
        {
            if (viewCacheList.TryGetValue(viewName, out content))
                return true;

            Func<string> getFunc;
            if (viewGetList.TryGetValue(viewName, out getFunc)) {
                content = getFunc();
                viewCacheList[viewName] = content;
                return true;
            }

            content = null;
            return false;
        }

        public ViewCollection Add(string viewName, Func<string> getFunc)
        {
            viewGetList[viewName] = getFunc;
            return this;
        }

        public ViewCollection AddFromExternal(string filename, string viewName = null)
        {
            viewGetList[viewName ?? filename] = () => LoadFromExternal(filename);
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

                viewGetList[viewName] = () => LoadFromExternal(filename);
            }

            return this;
        }

        public ViewCollection AddFromAssembly(string path, string viewName = null)
        {
            viewGetList[viewName ?? path] = () => LoadFromAssembly(path);
            return this;
        }

        private string LoadFromExternal(string filename)
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

        private string LoadFromAssembly(string path)
        {
            throw new NotImplementedException();
        }
    }
}

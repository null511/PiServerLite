using System;
using System.Collections.Concurrent;

namespace PiServerLite
{
    internal class ViewCache
    {
        private readonly ConcurrentDictionary<string, CachedView> viewCacheList;


        public ViewCache()
        {
            viewCacheList = new ConcurrentDictionary<string, CachedView>(StringComparer.OrdinalIgnoreCase);
        }
        
        public bool TryFind(string viewKey, out string viewData)
        {
            if (viewCacheList.TryGetValue(viewKey, out var cachedView) && !cachedView.IsExpiredFunc()) {
                viewData = cachedView.Data;
                return true;
            }

            viewData = null;
            return false;
        }

        public void Update(string viewKey, CachedView view)
        {
            viewCacheList.AddOrUpdate(viewKey, k => view, (k, v) => view);
        }
    }

    internal class CachedView
    {
        public string Data {get; set;}
        public Func<bool> IsExpiredFunc {get; set;}
    }
}

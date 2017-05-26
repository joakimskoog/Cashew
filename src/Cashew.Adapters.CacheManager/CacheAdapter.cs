using System;
using CacheManager.Core;
using Cashew.Core;

namespace Cashew.Adapters.CacheManager
{
    public class CacheAdapter : IHttpCache
    {
        private readonly ICache<object> _cache;

        public CacheAdapter(ICache<object> cache)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            _cache = cache;
        }

        public object Get(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _cache.Get(key);
        }

        public void Put(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            _cache.Put(key, value);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
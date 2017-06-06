using System;
using CacheManager.Core;
using Cashew;

namespace Cashew.Adapters.CacheManager
{
    public class CacheAdapter : IHttpCache
    {
        private readonly ICache<object> _cache;

        public CacheAdapter(ICache<object> cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public object Get(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _cache.Get(key);
        }

        public void Remove(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            _cache.Remove(key);
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
using System;
using CacheManager.Core;
using Cashew.Core;

namespace Cashew.Adapters.CacheManager
{
    public class CacheManagerAdapter : IHttpCache
    {
        private readonly ICacheManager<object> _cacheManager;

        public CacheManagerAdapter(ICacheManager<object> cacheManager)
        {
            if (cacheManager == null) throw new ArgumentNullException(nameof(cacheManager));
            _cacheManager = cacheManager;
        }

        public object Get(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _cacheManager.Get(key);
        }

        public void Put(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            _cacheManager.AddOrUpdate(key, value, currentValue => value);
        }

        public void Dispose()
        {
            _cacheManager.Dispose();
        }

    }
}
using System.Collections.Generic;

namespace Cashew.Tests.IntegrationTests
{
    public class SimpleCache : IHttpCache
    {
        private readonly IDictionary<string,object> _cache = new Dictionary<string, object>();
        
        public object Get(string key)
        {
            if (_cache.TryGetValue(key, out object value))
            {
                return value;
            }

            return null;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void Put(string key, object value)
        {
            Remove(key);
            _cache[key] = value;
        }

        public void Dispose()
        {
            _cache.Clear();
        }
    }
}
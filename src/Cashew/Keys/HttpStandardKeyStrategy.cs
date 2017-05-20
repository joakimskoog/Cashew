using System;
using System.Net.Http;

namespace Cashew.Keys
{
    public class HttpStandardKeyStrategy : ICacheKeyStrategy
    {
        private readonly CacheKeySetting _cacheKeySetting;

        public HttpStandardKeyStrategy(CacheKeySetting cacheKeySetting = CacheKeySetting.Standard)
        {
            _cacheKeySetting = cacheKeySetting;
        }

        public string GetCacheKey(HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }

        public string GetCacheKey(HttpRequestMessage request, HttpResponseMessage response)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
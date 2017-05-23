using System;
using System.Linq;
using System.Net.Http;
using CacheManager.Core;

namespace Cashew.Keys
{
    public class HttpStandardKeyStrategy : ICacheKeyStrategy
    {
        private const string VaryHeaderDelimiter = "_";

        private readonly ICacheManager<object> _cache;
        private readonly CacheKeySetting _cacheKeySetting;

        public HttpStandardKeyStrategy(ICacheManager<object> cache, CacheKeySetting cacheKeySetting = CacheKeySetting.Standard)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            _cache = cache;
            _cacheKeySetting = cacheKeySetting;
        }

        public string GetCacheKey(HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var uri = GetUri(request);
            var varyHeaders = _cache.Get<string>(uri);

            return $"{uri}{varyHeaders}";
        }

        public string GetCacheKey(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (response == null) throw new ArgumentNullException(nameof(response));

            var uri = GetUri(request);
            var varyHeaders = request.Headers.Where(x => response.Headers.Vary.Any(y => y.Equals(x.Key, StringComparison.CurrentCultureIgnoreCase))).SelectMany(o => o.Value);
            var formattedVaryheaderString = varyHeaders.Aggregate("", (current, varyHeader) => current + (VaryHeaderDelimiter + varyHeader));

            _cache.AddOrUpdate(uri, formattedVaryheaderString, current => formattedVaryheaderString);

            return $"{uri}{formattedVaryheaderString}";
        }

        private string GetUri(HttpRequestMessage request)
        {
            var requestUri = request.RequestUri.ToString();
            if (_cacheKeySetting == CacheKeySetting.Standard)
            {
                return requestUri;
            }

            return requestUri.Replace(request.RequestUri.Query, "");
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}

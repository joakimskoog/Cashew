using System;
using System.Linq;
using System.Net.Http;

namespace Cashew.Keys
{
    /// <summary>
    /// The default implementation of <see cref="ICacheKeyStrategy"/> that is HTTP standard compliant with support for the vary header.
    /// </summary>
    public class HttpStandardKeyStrategy : ICacheKeyStrategy
    {
        private const string VaryHeadersCacheKeyPrefix = "Vary:";
        private const string VaryHeaderDelimiter = "_";

        private readonly IHttpCache _cache;
        private readonly CacheKeySetting _cacheKeySetting;

        /// <summary>
        /// Initialises a new instance of the <see cref="HttpStandardKeyStrategy"/> class.
        /// </summary>
        /// <param name="cache">The <see cref="IHttpCache"/> that will be used to store vary headers.</param>
        /// <param name="cacheKeySetting">The <see cref="CacheKeySetting"/> that will be used to determine which part of the URI that will be used to create the key.</param>
        public HttpStandardKeyStrategy(IHttpCache cache, CacheKeySetting cacheKeySetting = CacheKeySetting.Standard)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheKeySetting = cacheKeySetting;
        }

        public string GetCacheKey(HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var uri = GetUri(request);
            var varyHeaders = _cache.Get<string>($"{VaryHeadersCacheKeyPrefix}{uri}");

             return $"{uri}{varyHeaders}";
        }

        public string GetCacheKey(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (response == null) throw new ArgumentNullException(nameof(response));

            var uri = GetUri(request);

            //todo: Simplify this
            var varyHeaders = request.Headers.Where(x => response.Headers.Vary.Any(y => y.Equals(x.Key, StringComparison.CurrentCultureIgnoreCase))).SelectMany(o => o.Value);
            var formattedVaryheaderString = varyHeaders.Aggregate("", (current, varyHeader) => current + (VaryHeaderDelimiter + varyHeader));

            _cache.Put($"{VaryHeadersCacheKeyPrefix}{uri}", formattedVaryheaderString);
            
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

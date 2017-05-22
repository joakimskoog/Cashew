using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using Cashew.Keys;

namespace Cashew
{
    public class HttpCachingHandler : DelegatingHandler
    {
        private readonly ICacheKeyStrategy _keyStrategy;
        private readonly ICacheManager<object> _cache;

        public HttpCachingHandler(ICacheManager<object> cache, ICacheKeyStrategy keyStrategy)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            

            return base.SendAsync(request, cancellationToken);
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                _cache.Dispose();
                _cache.Dispose();
            }

            base.Dispose(disposeManaged);
        }
    }
}
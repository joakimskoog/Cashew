using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cashew.Core.Keys;

namespace Cashew.Core
{
    public class HttpCachingHandler : DelegatingHandler
    {
        private readonly IHttpCache _cache;
        private readonly ICacheKeyStrategy _keyStrategy;
        
        public HttpCachingHandler(IHttpCache cache, ICacheKeyStrategy keyStrategy)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (keyStrategy == null) throw new ArgumentNullException(nameof(keyStrategy));
            _cache = cache;
            _keyStrategy = keyStrategy;
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
            }

            base.Dispose(disposeManaged);
        }
    }
}
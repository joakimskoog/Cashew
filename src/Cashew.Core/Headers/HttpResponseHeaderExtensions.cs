using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace Cashew.Core.Headers
{
    public static class HttpResponseHeaderExtensions
    {
        private const string CashewStatusHeader = "cw-cache-status";
        private const string StatusHit = "HIT";
        private const string StatusMiss = "MISS";
        private const string StatusStale = "STALE";
        private const string StatusRevalidated = "REVALIDATED";

        public static CacheStatus? GetCashewStatusHeader(this HttpResponseHeaders headers)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));

            IEnumerable<string> statuses;
            if (headers.TryGetValues(CashewStatusHeader, out statuses))
            {
                var firstStatus = statuses.FirstOrDefault();
                if (firstStatus != null)
                {
                    return GetHeaderStatus(firstStatus);
                }
            }

            return null;
        }


        internal static void AddClientCacheStatusHeader(this HttpResponseHeaders headers, CacheStatus status)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            headers.Remove(CashewStatusHeader);
            headers.Add(CashewStatusHeader, GetHeaderStringValue(status));
        }

        private static string GetHeaderStringValue(CacheStatus status)
        {
            switch (status)
            {
                case CacheStatus.Hit: return StatusHit;
                case CacheStatus.Miss: return StatusMiss;
                case CacheStatus.Stale: return StatusStale;
                case CacheStatus.Revalidated: return StatusRevalidated;
                default: throw new ArgumentOutOfRangeException(nameof(status), $"{status} is not recognized as a valid value");
            }
        }

        private static CacheStatus? GetHeaderStatus(string status)
        {
            switch (status)
            {
                case StatusHit: return CacheStatus.Hit;
                case StatusMiss: return CacheStatus.Miss;
                case StatusStale: return CacheStatus.Stale;
                case StatusRevalidated: return CacheStatus.Revalidated;
                default: return null;
            }
        }
    }
}
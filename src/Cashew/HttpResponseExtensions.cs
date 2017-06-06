using System;
using System.Net.Http;

namespace Cashew
{
    internal static class HttpResponseExtensions
    {
        /// <summary>
        /// Calculates the age of this <see cref="HttpResponseMessage"/> at the given <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> whose age should be calculated.</param>
        /// <param name="at">The point in time of which will be used to calculate the age of the response.</param>
        /// <returns>The age of the <see cref="HttpResponseMessage"/> at the given <see cref="DateTimeOffset"/>.</returns>
        internal static TimeSpan? CalculateAgeAt(this HttpResponseMessage response, DateTimeOffset at)
        {
            var date = response.Headers.Date;
            if (date == null)
            {
                return null;
            }

            return at - date.Value;
        }

        /// <summary>
        /// Calculates how long this <see cref="HttpResponseMessage"/> will stay fresh.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> whose freshness limit should be calculated.</param>
        /// <returns>The freshness limit of this <see cref="HttpResponseMessage"/>.</returns>
        internal static TimeSpan? CalculateFreshnessLifetime(this HttpResponseMessage response)
        {
            var cacheControl = response.Headers.CacheControl;
            if (cacheControl == null)
            {
                return null;
            }

            if (cacheControl.SharedMaxAge.HasValue)
            {
                return cacheControl.SharedMaxAge.Value;
            }
            if (cacheControl.MaxAge.HasValue)
            {
                return cacheControl.MaxAge.Value;
            }
            if (response.Content.Headers.Expires.HasValue)
            {
                return response.CalculateAgeAt(response.Content.Headers.Expires.Value);
            }

            return null;
        }
    }
}

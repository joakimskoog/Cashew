using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Cashew.Core.Headers
{
    internal static class HttpRequestExtensions
    {
        /// <summary>
        /// Adds a header that will be used for validating the <see cref="HttpResponseMessage"/>. If the cached response contains an E-Tag it will use If-None-Match to validate
        /// the response, otherwise it will check for Last-Modified and use If-Modified-Since if it exists. 
        /// </summary>
        /// <param name="requestHeaders"></param>
        /// <param name="cachedResponse"></param>
        internal static void AddCacheValidationHeader(this HttpRequestHeaders requestHeaders, HttpResponseMessage cachedResponse)
        {
            if (cachedResponse == null) throw new ArgumentNullException(nameof(cachedResponse));
            if (requestHeaders == null) throw new ArgumentNullException(nameof(requestHeaders));

            if (cachedResponse.Headers.ETag != null)
            {
                requestHeaders.Add("If-None-Match", cachedResponse.Headers.ETag.ToString());
            }
            else if (cachedResponse.Content.Headers.LastModified != null)
            {
                requestHeaders.Add("If-Modified-Since", cachedResponse.Content.Headers.LastModified.Value.ToString("R"));
            }
        }
    }
}

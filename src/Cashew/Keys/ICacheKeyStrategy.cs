using System;
using System.Net.Http;

namespace Cashew.Keys
{
    /// <summary>
    /// Defines how cache keys are retrieved from <see cref="HttpRequestMessage"/> and <see cref="HttpRequestMessage"/> for usage in <see cref="HttpCachingHandler"/>
    /// </summary>
    public interface ICacheKeyStrategy : IDisposable
    {
        /// <summary>
        /// Gets a cache key from the given HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The cache key.</returns>
        /// <exception cref="T:System.ArgumentNullException">If the <paramref name="request"/> is null</exception>
        string GetCacheKey(HttpRequestMessage request);

        /// <summary>
        /// Gets a cache key from the given HTTP request and HTTP response.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="response">The HTTP response.</param>
        /// <returns>The cache key.</returns>
        /// <exception cref="T:System.ArgumentNullException">If the <paramref name="request"/> or <paramref name="response"/> is null</exception>
        string GetCacheKey(HttpRequestMessage request, HttpResponseMessage response);
    }
}
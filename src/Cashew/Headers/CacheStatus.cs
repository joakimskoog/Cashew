namespace Cashew.Headers
{
    public enum CacheStatus
    {
        /// <summary>
        /// Existed in cache, served from cache.
        /// </summary>
        Hit = 1,

        /// <summary>
        /// Did not exist in cache, served from origin server.
        /// </summary>
        Miss = 2,

        /// <summary>
        /// Existed in cache but was stale, served from cache since it was deemed acceptable to serve a stale response.
        /// </summary>
        Stale = 3,

        /// <summary>
        /// Existed in cache but was stale, revalidated by using If-None-Match or If-Modified-Since since it was not deemed acceptable to serve a stale response. 
        /// </summary>
        Revalidated = 4
    }
}
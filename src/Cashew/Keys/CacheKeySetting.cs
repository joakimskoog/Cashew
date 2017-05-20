namespace Cashew.Keys
{
    /// <summary>
    /// Defines how many requests you want to cache, the recommended approach is to use <see cref="Standard"/>.
    /// </summary>
    public enum CacheKeySetting
    {
        /// <summary>
        /// Different cache key each time the query string changes.
        /// </summary>
        Standard = 1,

        /// <summary>
        /// Same cache key independent of the query string. This setting will remove query string when generating the cache key, a request for "style.css?v=2" will be normalised to "style.css".
        /// </summary>
        IgnoreQueryString = 2
    }
}
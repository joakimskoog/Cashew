using System;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Cashew.Core.Tests")]
namespace Cashew.Core
{
    public interface IHttpCache : IDisposable
    {
        object Get(string key);

        void Put(string key, object value);
    }

    internal static class HttpCacheExtensions
    {
        internal static T Get<T>(this IHttpCache cache, string key)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            return (T)cache.Get(key);
        }
    }
}
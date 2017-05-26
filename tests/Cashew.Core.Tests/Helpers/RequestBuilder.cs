using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Cashew.Core.Tests.Helpers
{
    public class RequestBuilder
    {
        private static RequestBuilder _instance;

        private readonly List<Action<HttpRequestMessage>> _builderActions = new List<Action<HttpRequestMessage>>();

        public static RequestBuilder Request(HttpMethod method, string url)
        {
            var instance = _instance ?? (_instance = new RequestBuilder());
            instance._builderActions.Add(delegate (HttpRequestMessage request)
            {
                request.Method = method;
                request.RequestUri = new Uri(url);
            });

            return instance;
        }

        public RequestBuilder WithNoStore()
        {
            _builderActions.Add(delegate (HttpRequestMessage r)
            {
                EnsureCacheControlHeaders(r);
                r.Headers.CacheControl.NoStore = true;
            });

            return _instance;
        }

        public RequestBuilder WithNoCache()
        {
            _builderActions.Add(delegate (HttpRequestMessage r)
            {
                EnsureCacheControlHeaders(r);
                r.Headers.CacheControl.NoCache = true;
            });

            return _instance;
        }

        public RequestBuilder WithOnlyIfCached()
        {
            _builderActions.Add(delegate(HttpRequestMessage r)
            {
                EnsureCacheControlHeaders(r);
                r.Headers.CacheControl.OnlyIfCached = true;
            });

            return _instance;
        }

        public RequestBuilder WithMaxStale()
        {
            _builderActions.Add(delegate (HttpRequestMessage r)
            {
                EnsureCacheControlHeaders(r);
                r.Headers.CacheControl.MaxStale = true;
            });

            return _instance;
        }

        public RequestBuilder WithMaxStaleLimit(int ageInSeconds)
        {
            _builderActions.Add(delegate (HttpRequestMessage r)
            {
                EnsureCacheControlHeaders(r);
                r.Headers.CacheControl.MaxStaleLimit = TimeSpan.FromSeconds(ageInSeconds);
            });

            return _instance;
        }

        public RequestBuilder WithMaxAge(int secondsAge)
        {
            _builderActions.Add(delegate (HttpRequestMessage r)
            {
                EnsureCacheControlHeaders(r);
                r.Headers.CacheControl.MaxAge = TimeSpan.FromSeconds(secondsAge);
            });

            return _instance;
        }

        public RequestBuilder WithMinFresh(int seconds)
        {
            _builderActions.Add(delegate (HttpRequestMessage r)
            {
                EnsureCacheControlHeaders(r);
                r.Headers.CacheControl.MinFresh = TimeSpan.FromSeconds(seconds);
            });

            return _instance;
        }

        private static void EnsureCacheControlHeaders(HttpRequestMessage request)
        {
            if (request.Headers.CacheControl == null)
            {
                request.Headers.CacheControl = new CacheControlHeaderValue();
            }
        }

        public HttpRequestMessage Build()
        {
            var request = new HttpRequestMessage();

            _builderActions.ForEach(a => a(request));
            _builderActions.Clear();

            return request;
        }
    }
}
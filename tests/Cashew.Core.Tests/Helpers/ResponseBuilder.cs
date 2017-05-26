using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Cashew.Core.Tests.Helpers
{
    public class ResponseBuilder
    {
        private static ResponseBuilder _instance;

        private readonly List<Action<HttpResponseMessage>> _builderActions = new List<Action<HttpResponseMessage>>();

        public static ResponseBuilder Response(HttpStatusCode statusCode)
        {
            var instance = _instance ?? (_instance = new ResponseBuilder());
            instance._builderActions.Add(delegate (HttpResponseMessage response)
            {
                response.StatusCode = statusCode;
            });

            return instance;
        }

        public ResponseBuilder Created(DateTimeOffset date)
        {
            _instance._builderActions.Add(r => r.Headers.Date = date);
            return _instance;
        }

        public ResponseBuilder WithSharedMaxAge(TimeSpan age)
        {
            _instance._builderActions.Add(delegate (HttpResponseMessage response)
            {
                EnsureCacheControlHeaders(response);
                response.Headers.CacheControl.SharedMaxAge = age;
            });

            return _instance;
        }

        public ResponseBuilder WithMaxAge(int ageInSeconds)
        {
            _instance._builderActions.Add(delegate (HttpResponseMessage response)
            {
                EnsureCacheControlHeaders(response);
                response.Headers.CacheControl.MaxAge = TimeSpan.FromSeconds(ageInSeconds);
            });

            return _instance;
        }

        public ResponseBuilder Expires(DateTimeOffset date)
        {
            _instance._builderActions.Add(r => r.Content.Headers.Expires = date);
            return _instance;
        }

        public ResponseBuilder WithNoCache()
        {
            _instance._builderActions.Add(delegate (HttpResponseMessage response)
            {
                EnsureCacheControlHeaders(response);
                response.Headers.CacheControl.NoCache = true;
            });

            return _instance;
        }

        private static void EnsureCacheControlHeaders(HttpResponseMessage response)
        {
            if (response.Headers.CacheControl == null)
            {
                response.Headers.CacheControl = new CacheControlHeaderValue();
            }
        }


        public HttpResponseMessage Build()
        {
            var response = new HttpResponseMessage { Content = new ByteArrayContent(new byte[256]) };

            _builderActions.ForEach(a => a(response));
            _builderActions.Clear();

            return response;
        }
    }
}
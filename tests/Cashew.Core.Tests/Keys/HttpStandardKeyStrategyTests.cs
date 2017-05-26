using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Cashew.Core.Keys;
using Moq;
using Xunit;

namespace Cashew.Core.Tests.Keys
{
    public class HttpStandardKeyStrategyTests
    {
        private const string UrlWithoutQueryString = "https://anapioficeandfire.com/api/characters";
        private const string UrlWithQueryString = "https://anapioficeandfire.com/api/characters?name=Jon+Snow";

        [Fact]
        public void GetCacheKey_RequestIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<IHttpCache>();
            var sut = new HttpStandardKeyStrategy(cacheMock.Object);

            Assert.Throws<ArgumentNullException>(() => sut.GetCacheKey(null));
        }

        [Fact]
        public void GetCacheKey_RequestUrlWithQueryStringAndSettingIsStandard_KeyIsUrlWithQueryString()
        {
            var cacheMock = new Mock<IHttpCache>();
            cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns("");

            var sut = new HttpStandardKeyStrategy(cacheMock.Object, CacheKeySetting.Standard);
            var request = new HttpRequestMessage(HttpMethod.Get, UrlWithQueryString);

            var key = sut.GetCacheKey(request);

            Assert.Equal(UrlWithQueryString, key);
        }

        [Fact]
        public void GetCacheKey_RequestUrlWithQueryStringAndSettingIsIgnoreQueryString_KeyIsUrlWithoutQueryString()
        {
            var cacheMock = new Mock<IHttpCache>();
            cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns("");
            var sut = new HttpStandardKeyStrategy(cacheMock.Object, CacheKeySetting.IgnoreQueryString);
            var request = new HttpRequestMessage(HttpMethod.Get, UrlWithQueryString);

            var key = sut.GetCacheKey(request);

            Assert.Equal(UrlWithoutQueryString, key);
        }

        [Fact]
        public void GetCacheKeyForRequestAndResponse_RequestIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<IHttpCache>();
            cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns("");
            var sut = new HttpStandardKeyStrategy(cacheMock.Object, CacheKeySetting.Standard);

            Assert.Throws<ArgumentNullException>(() => sut.GetCacheKey(null, new HttpResponseMessage()));
        }

        [Fact]
        public void GetCacheKeyForRequestAndResponse_ResponseIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<IHttpCache>();
            cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns("");
            var sut = new HttpStandardKeyStrategy(cacheMock.Object, CacheKeySetting.Standard);

            Assert.Throws<ArgumentNullException>(() => sut.GetCacheKey(new HttpRequestMessage(HttpMethod.Get, UrlWithQueryString), null));
        }

        [Fact]
        public void GetCacheKeyForRequestAndResponse_NoVaryHeadersInResponseAndStandardSettings_KeyIsUrlWithQueryString()
        {
            var cacheMock = new Mock<IHttpCache>();
            cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns("");
            var sut = new HttpStandardKeyStrategy(cacheMock.Object, CacheKeySetting.Standard);
            var request = new HttpRequestMessage(HttpMethod.Get, UrlWithQueryString);
            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                RequestMessage = request,
            };

            var key = sut.GetCacheKey(request, response);

            Assert.Equal(UrlWithQueryString, key);
        }

        [Fact]
        public void GetCacheKeyForRequestAndResponse_VaryHeaderInResponseAndStandardSettings_KeyIsUrlWithQueryStringAndVaryHeader()
        {
            var cacheMock = new Mock<IHttpCache>();
            cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns("");
            var sut = new HttpStandardKeyStrategy(cacheMock.Object, CacheKeySetting.Standard);
            var request = new HttpRequestMessage(HttpMethod.Get, UrlWithQueryString);
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                RequestMessage = request
            };
            response.Headers.Vary.Add("Accept-Encoding");

            var key = sut.GetCacheKey(request, response);

            Assert.Equal(UrlWithQueryString + "_gzip", key);
        }

        [Fact]
        public void GetCacheKeyForRequestAndResponse_MultipleVaryHeadersInResponseAndStandardSettings_KeyIsUrlWithQueryStringAndVaryHeaders()
        {
            var cacheMock = new Mock<IHttpCache>();
            cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns("");
            var sut = new HttpStandardKeyStrategy(cacheMock.Object, CacheKeySetting.Standard);
            var request = new HttpRequestMessage(HttpMethod.Get, UrlWithQueryString);
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                RequestMessage = request
            };
            response.Headers.Vary.Add("Accept-Encoding");
            response.Headers.Vary.Add("Accept-Language");


            var key = sut.GetCacheKey(request, response);

            Assert.Equal(UrlWithQueryString + "_gzip_en-US", key);
        }

        [Fact]
        public void GetCacheKey_VaryHeadersAreCachedFromPreviousRequestResponsePair_TheTwoKeysAreMatching()
        {
            var usedKey = "";
            var usedVaryHeaders = "";
            var cacheMock = new Mock<IHttpCache>();
            cacheMock.Setup(x => x.Put(It.IsAny<string>(), It.IsAny<object>())).Callback(
                delegate(string key, object value)
                {
                    usedKey = key;
                    usedVaryHeaders = value as string;
                });

            var sut = new HttpStandardKeyStrategy(cacheMock.Object, CacheKeySetting.Standard);
            var request = new HttpRequestMessage(HttpMethod.Get, UrlWithQueryString);
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                RequestMessage = request
            };
            response.Headers.Vary.Add("Accept-Encoding");
            response.Headers.Vary.Add("Accept-Language");

            var firstKey = sut.GetCacheKey(request, response);

            //We set it up here because usedKey and usedVaryHeaders are set in the call above
            cacheMock.Setup(x => x.Get(It.Is<string>(key => key.Equals(usedKey)))).Returns(usedVaryHeaders);
            var secondKey = sut.GetCacheKey(request);

            Assert.Equal(firstKey, secondKey);
        }
    }
}
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cashew.Headers;
using Cashew.Keys;
using Cashew.Tests.Helpers;
using Moq;
using Xunit;

namespace Cashew.Tests.UnitTests
{
    public class HttpCachingHandlerTests
    {
        private const string Url = "https://anapioficeandfire.com/api/characters/583";

        private static DateTimeOffset _testDate = new DateTimeOffset(2017, 1, 1, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset TenMinutesAgo = _testDate.Subtract(TimeSpan.FromMinutes(10));
        private static readonly DateTimeOffset ThirtyMinutesAgo = _testDate.Add(TimeSpan.FromMinutes(30));

        private readonly HttpClient _client;
        private HttpCachingHandler _cachingHandler;
        private readonly FakeMessageHandler _fakeMessageHandler = new FakeMessageHandler();
        private readonly Mock<IHttpCache> _cacheMock = new Mock<IHttpCache>();

        public HttpCachingHandlerTests()
        {
            var mockKeyStrategy = new Mock<ICacheKeyStrategy>();
            mockKeyStrategy.Setup(x => x.GetCacheKey(It.IsAny<HttpRequestMessage>())).Returns<HttpRequestMessage>(r => r.RequestUri.ToString());
            mockKeyStrategy.Setup(x => x.GetCacheKey(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>()))
                .Returns<HttpRequestMessage, HttpResponseMessage>((req, res) => req.RequestUri.ToString());

            _cachingHandler = new HttpCachingHandler(_cacheMock.Object, mockKeyStrategy.Object)
            {
                SystemClock = new FakeClock()
                {
                    UtcNow = _testDate
                },
                InnerHandler = _fakeMessageHandler
            };
            _client = new HttpClient(_cachingHandler);
        }

        #region Constructor

        [Fact]
        public void Constructor_CacheIsNull_ArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new HttpCachingHandler(null, new Mock<ICacheKeyStrategy>().Object));
        }

        [Fact]
        public void Constructor_KeyStrategyIsNull_ArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new HttpCachingHandler(new Mock<IHttpCache>().Object, null));
        }

        #endregion

        #region Non-supported HTTP verbs

        [Fact]
        public async Task SendAsync_MethodIsPut_CacheIsNotUsed()
        {
            var request = RequestBuilder.Request(HttpMethod.Put, Url).Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.OK).Build();
            _fakeMessageHandler.Response = fakeResponse;

            var response = await _client.SendAsync(request);

            Assert.Null(response.Headers.GetCashewStatusHeader());
            Assert.Equal(fakeResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Never);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MethodIsPost_CacheIsNotUsed()
        {
            var request = RequestBuilder.Request(HttpMethod.Post, Url).Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.Created).Build();
            _fakeMessageHandler.Response = fakeResponse;

            var response = await _client.SendAsync(request);

            Assert.Null(response.Headers.GetCashewStatusHeader());
            Assert.Equal(fakeResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Never);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MethodIsDelete_CacheIsNotUsed()
        {
            var request = RequestBuilder.Request(HttpMethod.Delete, Url).Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.NoContent).Build();
            _fakeMessageHandler.Response = fakeResponse;

            var response = await _client.SendAsync(request);

            Assert.Null(response.Headers.GetCashewStatusHeader());
            Assert.Equal(fakeResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Never);

            _cacheMock.Reset();
        }

        #endregion

        #region Request.No-store

        [Fact]
        public async Task SendAsync_NoStoreHeaderInRequest_CacheIsNotUsed()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithNoStore().Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.OK).Build();
            _fakeMessageHandler.Response = fakeResponse;

            var response = await _client.SendAsync(request);

            Assert.Null(response.Headers.GetCashewStatusHeader());
            Assert.Equal(fakeResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Never);

            _cacheMock.Reset();
        }

        #endregion

        #region Request.Max-age

        [Fact]
        public async Task SendAsync_MaxAgeAndFresh_CacheIsHit()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxAge(3600).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(TenMinutesAgo).WithMaxAge(3600).Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.Is<string>(s => s.Equals(Url)))).Returns(new SerializedHttpResponseMessage(cachedResponse, content));

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Hit, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MaxAgeAndStaleWithStaleNotAcceptable_ResponseIsRevalidated()
        {
            var etag = "\"awesomeetag\"";
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxAge(3600).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate.Subtract(TimeSpan.FromHours(2))).WithMaxAge(3600).WithETag(etag).Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.NotModified).Created(_testDate).Build();
            _fakeMessageHandler.Response = fakeResponse;
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(cachedResponse, content));

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MaxAgeAndStaleWithStaleAcceptable_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxAge(3600).WithMaxStale().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate.Subtract(TimeSpan.FromHours(2))).WithMaxAge(3000).Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(cachedResponse, content));

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Stale, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        #region Request.Max-stale

        [Fact]
        public async Task SendAsync_MaxStaleAndStaleResponse_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).Expires(TenMinutesAgo).Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Stale, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_StaleResponseWithinMaxStaleLimit_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStaleLimit(3600).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate.Subtract(TimeSpan.FromMinutes(20))).Expires(TenMinutesAgo).Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Stale, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_StaleResponseOverMaxStaleLimit_ResponseIsRevalidated()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStaleLimit(60).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).Expires(TenMinutesAgo).Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.NotModified).Created(_testDate).Build();
            _fakeMessageHandler.Response = freshResponse;

            var response = await _client.SendAsync(request);


            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        #region Request.Only-if-cached

        [Fact]
        public async Task SendAsync_OnlyIfCachedButNoItemsInCache_GatewayTimeout()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithOnlyIfCached().Build();
            _fakeMessageHandler.Response = ResponseBuilder.Response(HttpStatusCode.OK).Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_OnlyIfCachedResponseStaleButMaxStale_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithOnlyIfCached().WithMaxStale().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                    .Created(TenMinutesAgo)
                    .WithMaxAge(300)
                    .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));

            var response = await _client.SendAsync(request);

            Assert.Equal(cachedResponse, response);
            Assert.Equal(CacheStatus.Stale, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_OnlyIfCachedAndCachedResponseIsFresh_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithOnlyIfCached().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                    .Created(TenMinutesAgo)
                    .Expires(_testDate.AddHours(1))
                    .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));

            var response = await _client.SendAsync(request);

            Assert.Equal(cachedResponse, response);
            Assert.Equal(CacheStatus.Hit, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        #region Request.Min-fresh

        [Fact]
        public async Task SendAsync_MinFreshOneMinuteAndCachedResponseIsStillFreshForTenMinutes_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMinFresh(60).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(TenMinutesAgo)
                .Expires(_testDate.AddMinutes(10))
                .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));

            var response = await _client.SendAsync(request);

            Assert.Equal(cachedResponse, response);
            Assert.Equal(CacheStatus.Hit, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MinFreshOneMinuteCachedResponseStillFreshForTwentySeconds_CacheIsRevalidated()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMinFresh(60).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(TenMinutesAgo)
                .Expires(_testDate.AddSeconds(20))
                .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));
            _fakeMessageHandler.Response = ResponseBuilder.Response(HttpStatusCode.OK).Build();

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        #region Request.No-cache

        [Fact]
        public async Task SendAsync_NoCacheAndFreshCachedResponse_ResponseIsRevalidated()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithNoCache().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(ThirtyMinutesAgo)
                .Expires(_testDate.AddHours(2))
                .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.NotModified).Created(_testDate).Build();
            _fakeMessageHandler.Response = freshResponse;

            var response = await _client.SendAsync(request);

            Assert.Equal(cachedResponse, response);
            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion



        #region Response.No-store

        [Fact]
        public async Task SendAsync_ResponseNoStore_ResponseIsNotCached()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.OK).WithNoStore().Build();
            _fakeMessageHandler.Response = fakeResponse;
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);
            _cacheMock.Verify(x => x.Put(It.IsAny<string>(), It.IsAny<object>()), Times.Never);

            _cacheMock.Reset();
        }

        #endregion

        #region Response.Must-revalidate

        [Fact]
        public async Task SendAsync_MustRevalidateAndFresh_CachedResponseIsReturned()
        {
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(TenMinutesAgo)
                .Expires(_testDate.AddHours(2))
                .WithMustRevalidate()
                .Build();
            var content = await freshResponse.Content.ReadAsByteArrayAsync();
            _fakeMessageHandler.Response = freshResponse;
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            _cacheMock.Setup(x => x.Put(It.IsAny<string>(), It.IsAny<object>()));

            var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());

            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(freshResponse, content));

            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Hit, secondResponse.Headers.GetCashewStatusHeader());

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MustRevalidateAndStale_ResponseIsRevalidatedOnSubsequentRequest()
        {
            var firstResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(TenMinutesAgo)
                .WithMaxAge(60)
                .WithMustRevalidate()
                .Build();
            var content = await firstResponse.Content.ReadAsByteArrayAsync();
            _fakeMessageHandler.Response = firstResponse;
            var revalidatedResponse = ResponseBuilder.Response(HttpStatusCode.NotModified).Created(_testDate).Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            _cacheMock.Setup(x => x.Put(It.IsAny<string>(), It.IsAny<object>()));

            var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());
            Assert.Equal(firstResponse, response);

            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(firstResponse, content));
            _fakeMessageHandler.Response = revalidatedResponse;

            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());

            Assert.Equal(CacheStatus.Revalidated, secondResponse.Headers.GetCashewStatusHeader());
            Assert.Equal(firstResponse, secondResponse);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Exactly(2));
            _cacheMock.Verify(x => x.Put(It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(2));

            _cacheMock.Reset();
        }

        #endregion

        #region Response.Proxy-revalidate

        [Fact]
        public async Task SendAsync_ProxyRevalidateAndFresh_CachedResponseIsReturned()
        {
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(TenMinutesAgo)
                .Expires(_testDate.AddHours(2))
                .WithProxyRevalidate()
                .Build();
            var content = await freshResponse.Content.ReadAsByteArrayAsync();
            _fakeMessageHandler.Response = freshResponse;
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            _cacheMock.Setup(x => x.Put(It.IsAny<string>(), It.IsAny<object>()));

            var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());

            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(freshResponse, content));

            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Hit, secondResponse.Headers.GetCashewStatusHeader());

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_ProxyRevalidateAndStale_ResponseIsRevalidatedOnSubsequentRequest()
        {
            var firstResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(TenMinutesAgo)
                .WithMaxAge(60)
                .WithProxyRevalidate()
                .Build();
            var content = await firstResponse.Content.ReadAsByteArrayAsync();
            _fakeMessageHandler.Response = firstResponse;
            var revalidatedResponse = ResponseBuilder.Response(HttpStatusCode.NotModified).Created(_testDate).Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            _cacheMock.Setup(x => x.Put(It.IsAny<string>(), It.IsAny<object>()));
            //save put

            var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());
            Assert.Equal(firstResponse, response);

            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(firstResponse, content));
            _fakeMessageHandler.Response = revalidatedResponse;

            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Revalidated, secondResponse.Headers.GetCashewStatusHeader());
            Assert.Equal(firstResponse, secondResponse);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Exactly(2));
            _cacheMock.Verify(x => x.Put(It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(2));

            _cacheMock.Reset();
        }

        #endregion

        #region Response.max-age

        [Fact]
        public async Task SendAsync_MaxAgeInResponseAndAgeLessThanMaxAge_CachedResponseIsFresh()
        {
            var serverResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate.Subtract(TimeSpan.FromMinutes(30))).WithMaxAge(3600).Build();
            var content = await serverResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            _fakeMessageHandler.Response = serverResponse;

            var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());

            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(serverResponse, content));

            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Hit, secondResponse.Headers.GetCashewStatusHeader());
        }

        [Fact]
        public async Task SendAsync_MaxAgeInResponseAndAgeHigherThanMaxAge_CachedResponseIsStale()
        {
            var serverResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate.Subtract(TimeSpan.FromHours(2))).WithMaxAge(3600).Build();
            var content = await serverResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            _fakeMessageHandler.Response = serverResponse;

            var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());

            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(serverResponse, content));

            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
            Assert.Equal(CacheStatus.Stale, secondResponse.Headers.GetCashewStatusHeader());
        }

        #endregion

        #region Response.s-maxage

        [Fact]
        public async Task SendAsync_SharedMaxAgeInResponseAndAgeLessThanSharedMaxAge_CachedResponseIsFresh()
        {
            var serverResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate.Subtract(TimeSpan.FromMinutes(30))).WithSharedMaxAge(3600).Build();
            var content = await serverResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            _fakeMessageHandler.Response = serverResponse;

            var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());

            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(serverResponse, content));

            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Hit, secondResponse.Headers.GetCashewStatusHeader());
        }

        [Fact]
        public async Task SendAsync_SharedMaxAgeInResponseAndAgeHigherThanSharedMaxAge_CachedResponseIsStale()
        {
            var serverResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate.Subtract(TimeSpan.FromHours(2))).WithSharedMaxAge(3600).Build();
            var content = await serverResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            _fakeMessageHandler.Response = serverResponse;

            var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());

            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(serverResponse, content));

            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
            Assert.Equal(CacheStatus.Stale, secondResponse.Headers.GetCashewStatusHeader());
        }

        #endregion

        #region Response.No-cache

        [Fact]
        public async Task SendAsync_NoCacheInResponse_CachedResponseIsRevalidated()
        {
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => null);
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate).WithNoCache().Build();
            _fakeMessageHandler.Response = freshResponse;

            var firstResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());
            var content = await firstResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new SerializedHttpResponseMessage(freshResponse, content));
            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).Build());

            Assert.Equal(firstResponse, secondResponse);
            Assert.Equal(CacheStatus.Revalidated, secondResponse.Headers.GetCashewStatusHeader());

            _cacheMock.Reset();
        }

        #endregion


        #region Revalidation

        [Fact]
        public async Task SendAsync_MustRevalidateAndETagInResponse_IfNoneMatchAddedToRequest()
        {
            var etag = "\"awesomeetag\"";
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(TenMinutesAgo)
                .WithMustRevalidate()
                .WithETag(etag)
                .Expires(_testDate.Subtract(TimeSpan.FromMinutes(5)))
                .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));
            _fakeMessageHandler.Response = ResponseBuilder.Response(HttpStatusCode.NotModified).Created(_testDate).Build();
            var request = RequestBuilder.Request(HttpMethod.Get, Url).Build();

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            Assert.Equal(etag, request.Headers.IfNoneMatch.First().Tag);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MustRevalidateAndLastModifiedInResponse_IfModifiedSinceAddedToRequest()
        {
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(TenMinutesAgo)
                .WithMustRevalidate()
                .LastModified(TenMinutesAgo)
                .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));
            _fakeMessageHandler.Response = ResponseBuilder.Response(HttpStatusCode.NotModified).Created(_testDate).Build();
            var request = RequestBuilder.Request(HttpMethod.Get, Url).Build();

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            Assert.Equal(TenMinutesAgo, request.Headers.IfModifiedSince.Value);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MustRevalidateAndResponseIsNotModified_CachedResponseIsUpdatedAndUsed()
        {
            var etag = "\"awesomeetag\"";
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(TenMinutesAgo)
                .WithMustRevalidate()
                .WithETag(etag)
                .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));
            _fakeMessageHandler.Response = ResponseBuilder.Response(HttpStatusCode.NotModified).Created(_testDate).Build();
            var request = RequestBuilder.Request(HttpMethod.Get, Url).Build();

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            Assert.True(response.Headers.Date > TenMinutesAgo);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MustRevalidateAndResponseIsModified_NewResponseUsedAndReplacedOldCached()
        {
            var etag = "\"awesomeetag\"";
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(TenMinutesAgo)
                .WithMustRevalidate()
                .WithETag(etag)
                .Expires(_testDate.Subtract(TimeSpan.FromMinutes(5)))
                .Build();
            var content = await cachedResponse.Content.ReadAsByteArrayAsync();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => new SerializedHttpResponseMessage(cachedResponse, content));
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate).Expires(_testDate.Add(TimeSpan.FromMinutes(10))).Build();
            _fakeMessageHandler.Response = freshResponse;
            var request = RequestBuilder.Request(HttpMethod.Get, Url).Build();

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            Assert.Equal(freshResponse, response);
            _cacheMock.Verify(x => x.Put(It.IsAny<string>(), It.Is<SerializedHttpResponseMessage>(message => message.Response.Equals(freshResponse))), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        [Fact]
        public async Task SendAsync_NoItemsInCache_CacheMiss()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.OK).Build();
            _fakeMessageHandler.Response = fakeResponse;
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());
            Assert.Equal(fakeResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public void SendAsync_RequestIsNull_ArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => _client.SendAsync(null).GetAwaiter().GetResult());
        }

        [Fact]
        public async Task SendAsync_ResponseStatusCodeIsNotCacheable_ResponseNotPutInCache()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.Accepted).Build();
            _fakeMessageHandler.Response = fakeResponse;
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);
            var defaultCacheableStatusCodes = _cachingHandler.CacheableStatusCodes;
            _cachingHandler.CacheableStatusCodes = new[] { HttpStatusCode.OK };

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Put(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
            _cachingHandler.CacheableStatusCodes = defaultCacheableStatusCodes;
            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_ResponseContentIsNull_ResponseNotPutInCache()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.OK).Build();
            fakeResponse.Content = null;
            _fakeMessageHandler.Response = fakeResponse;
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(null);

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Miss, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Put(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
            _cacheMock.Reset();
        }
    }
}
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cashew.Core.Headers;
using Cashew.Core.Keys;
using Cashew.Core.Tests.Helpers;
using Moq;
using Xunit;

namespace Cashew.Core.Tests
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
            _cachingHandler = new HttpCachingHandler(_cacheMock.Object, new HttpStandardKeyStrategy(_cacheMock.Object))
            {
                SystemClock = new FakeClock()
                {
                    UtcNow = _testDate
                },
                InnerHandler = _fakeMessageHandler
            };
            _client = new HttpClient(_cachingHandler);
        }

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

        #region Max-age

        [Fact]
        public async Task SendAsync_MaxAgeAndFresh_CacheIsHit()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxAge(3600).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).WithMaxAge(4000).Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(cachedResponse);

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Hit, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MaxAgeAndStaleWithStaleNotAcceptable_ResponseIsRevalidated()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxAge(3600).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).WithMaxAge(3000).Build();
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate).Build();
            _fakeMessageHandler.Response = fakeResponse;
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(cachedResponse);

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            Assert.Equal(fakeResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MaxAgeAndStaleWithStaleAcceptable_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxAge(3600).WithMaxStale().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).WithMaxAge(3000).Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(cachedResponse);

            var response = await _client.SendAsync(request);

            Assert.Equal(CacheStatus.Stale, response.Headers.GetCashewStatusHeader());
            Assert.Equal(cachedResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        #region Max-stale

        [Fact]
        public async Task SendAsync_MaxStaleAndStaleResponse_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).Expires(TenMinutesAgo).Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);

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
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK).Expires(TenMinutesAgo).Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);

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
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate).Build();
            _fakeMessageHandler.Response = freshResponse;

            var response = await _client.SendAsync(request);


            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            Assert.Equal(freshResponse, response);
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        #region Only-if-cached

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
        public async Task Send_Async_OnlyIfCachedStoredResponseExistsButNotFresh_GatewayTimeout()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithOnlyIfCached().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                    .Created(TenMinutesAgo)
                    .WithMaxAge(300)
                    .Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            Assert.Equal(CacheStatus.Stale, response.Headers.GetCashewStatusHeader());
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
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);

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
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);

            var response = await _client.SendAsync(request);

            Assert.Equal(cachedResponse, response);
            Assert.Equal(CacheStatus.Hit, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        #region Min-fresh
        
        [Fact]
        public async Task SendAsync_MinFreshOneMinuteAndCachedResponseIsStillFreshForTenMinutes_CachedResponseIsReturned()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMinFresh(60).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(TenMinutesAgo)
                .Expires(_testDate.AddMinutes(10))
                .Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);

            var response = await _client.SendAsync(request);

            Assert.Equal(cachedResponse, response);
            Assert.Equal(CacheStatus.Hit, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        [Fact]
        public async Task SendAsync_MinFreshOneMinuteCachedResponseStillFreshForTwentySeconds_ResponseIsRevalidated()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithMinFresh(60).Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(TenMinutesAgo)
                .Expires(_testDate.AddSeconds(20))
                .Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate).Build();
            _fakeMessageHandler.Response = freshResponse;

            var response = await _client.SendAsync(request);

            Assert.Equal(freshResponse, response);
            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion

        #region No-cache

        [Fact]
        public async Task SendAsync_NoCacheAndFreshCachedResponse_ResponseIsRevalidated()
        {
            var request = RequestBuilder.Request(HttpMethod.Get, Url).WithNoCache().Build();
            var cachedResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .Created(ThirtyMinutesAgo)
                .Expires(_testDate.AddHours(2))
                .Build();
            _cacheMock.Setup(x => x.Get(It.IsAny<string>())).Returns(() => cachedResponse);
            var freshResponse = ResponseBuilder.Response(HttpStatusCode.OK).Created(_testDate).Build();
            _fakeMessageHandler.Response = freshResponse;

            var response = await _client.SendAsync(request);

            Assert.Equal(freshResponse, response);
            Assert.Equal(CacheStatus.Revalidated, response.Headers.GetCashewStatusHeader());
            _cacheMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);

            _cacheMock.Reset();
        }

        #endregion




    }

    public class FakeClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }

    public class FakeMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return Response;
        }
    }
}
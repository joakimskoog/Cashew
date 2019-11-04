using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using Cashew.Keys;
using Cashew.Tests.Helpers;
using Xunit;

namespace Cashew.Tests.IntegrationTests
{
    public class HttpCachingHandlerTests
    {
        private const string Url = "https://anapioficeandfire.com/api/characters/583";
        private const string ResponseContent = "{ \"Value\": \"abcdef\"}";
        private static readonly DateTimeOffset _testDate = new DateTimeOffset(2017, 1, 1, 10, 0, 0, TimeSpan.Zero);

        private readonly HttpClient _client;
        private HttpCachingHandler _cachingHandler;

        public HttpCachingHandlerTests()
        {
            var cache = new SimpleCache();
            var keyStrategy = new HttpStandardKeyStrategy(cache);
            var fakeResponse = ResponseBuilder.Response(HttpStatusCode.OK)
                .WithContent(new StringContent(ResponseContent, Encoding.UTF8, "application/json"))
                .Created(_testDate.Subtract(TimeSpan.FromMinutes(15)))
                .WithMaxAge(3600)
                .Build();

            _cachingHandler = new HttpCachingHandler(cache, keyStrategy)
            {
                SystemClock = new FakeClock
                {
                    UtcNow = _testDate
                },
                InnerHandler = new FakeMessageHandler
                {
                    Response = fakeResponse
                }
            };
            _client = new HttpClient(_cachingHandler);
        }

        [Fact]
        public async Task CachedResponse_ContentIsReadMultipleTimesWithReadAsByteArray_StreamIsNotConsumed()
        {
            var firstResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
            var firstBytes = await firstResponse.Content.ReadAsByteArrayAsync();
            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
            var secondBytes = await secondResponse.Content.ReadAsByteArrayAsync();

            Assert.NotNull(firstBytes);
            Assert.NotNull(secondBytes);
            Assert.Equal(firstBytes, secondBytes);
        }

        [Fact]
        public async Task CachedResponse_ContentIsReadMultipleTimesWithReadAsStream_StreamIsNotConsumed()
        {
            var firstResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
            var firstBytes = await firstResponse.Content.ReadAsStreamAsync();
            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
            var secondBytes = await secondResponse.Content.ReadAsStreamAsync();

            Assert.NotNull(firstBytes);
            Assert.NotNull(secondBytes);
        }


        [Fact]
        public async Task CachedResponse_ContentIsReadMultipleTimesWithReadAsString_StreamIsNotConsumed()
        {
            var firstResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
            var firstBytes = await firstResponse.Content.ReadAsStringAsync();
            var secondResponse = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
            var secondBytes = await secondResponse.Content.ReadAsStringAsync();

            Assert.NotNull(firstBytes);
            Assert.NotNull(secondBytes);
            Assert.Equal(firstBytes, secondBytes);
        }

        [Fact]
        public async Task CachedResponse_ContentIsReadMultipleTimesWithReadAsAsync_StreamIsNotConsumed()
        {
            for (int i = 0; i < 5; i++)
            {
                var response = await _client.SendAsync(RequestBuilder.Request(HttpMethod.Get, Url).WithMaxStale().Build());
                var dto = await response.Content.ReadAsAsync<SimpleDto>(new MediaTypeFormatter[] { new JsonMediaTypeFormatter() });

                Assert.NotNull(response);
                Assert.NotNull(dto);
                Assert.Equal("abcdef", dto.Value);
            }
        }
    }

    public class SimpleDto
    {
        public string Value { get; set; }
    }
}
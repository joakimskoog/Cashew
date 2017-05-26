using System;
using CacheManager.Core;
using Moq;
using Xunit;

namespace Cashew.Adapters.CacheManager.Tests
{
    public class CacheManagerAdapterTests
    {
        [Fact]
        public void Constructor_CacheManagerIsNull_ArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheManagerAdapter(null));
        }

        [Fact]
        public void Get_KeyIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<ICacheManager<object>>();
            var sut = new CacheManagerAdapter(cacheMock.Object);

            Assert.Throws<ArgumentNullException>(() => sut.Get(null));
        }

        [Fact]
        public void Get_KeyIsValid_CacheManagerGetIsCalled()
        {
            var cacheMock = new Mock<ICacheManager<object>>();
            cacheMock.Setup(x => x.Get("key")).Returns("value");
            var sut = new CacheManagerAdapter(cacheMock.Object);

            var result = sut.Get("key") as string;

            Assert.Equal("value", result);
            cacheMock.Verify(x => x.Get("key"), Times.Once);
        }

        [Fact]
        public void Put_KeyIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<ICacheManager<object>>();
            var sut = new CacheManagerAdapter(cacheMock.Object);

            Assert.Throws<ArgumentNullException>(() => sut.Put(null, "value"));
        }

        [Fact]
        public void Put_ValueIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<ICacheManager<object>>();
            var sut = new CacheManagerAdapter(cacheMock.Object);

            Assert.Throws<ArgumentNullException>(() => sut.Put("key", null));
        }

        [Fact]
        public void Put_ParametersAreValid_CacheManagerPutIsCalled()
        {
            var cacheMock = new Mock<ICacheManager<object>>();
            cacheMock.Setup(x => x.Put("key", "value"));
            var sut = new CacheManagerAdapter(cacheMock.Object);

            sut.Put("key", "value");

            cacheMock.Verify(x => x.AddOrUpdate("key", "value", It.IsAny<Func<object,object>>()), Times.Once);
        }

        [Fact]
        public void Dispose_CacheManagerDisposeIsCalled()
        {
            var cacheMock = new Mock<ICacheManager<object>>();
            var sut = new CacheManagerAdapter(cacheMock.Object);

            sut.Dispose();

            cacheMock.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
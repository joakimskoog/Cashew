using System;
using CacheManager.Core;
using Moq;
using Xunit;

namespace Cashew.Adapters.CacheManager.Tests
{
    public class CacheAdapterTests
    {
        [Fact]
        public void Constructor_CacheIsNull_ArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheAdapter(null));
        }

        [Fact]
        public void Get_KeyIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<ICache<object>>();
            var sut = new CacheAdapter(cacheMock.Object);

            Assert.Throws<ArgumentNullException>(() => sut.Get(null));
        }

        [Fact]
        public void Get_KeyIsValid_CacheGetIsCalled()
        {
            var cacheMock = new Mock<ICache<object>>();
            cacheMock.Setup(x => x.Get("key")).Returns("value");
            var sut = new CacheAdapter(cacheMock.Object);

            var result = sut.Get("key") as string;

            Assert.Equal("value", result);
            cacheMock.Verify(x => x.Get("key"), Times.Once);
        }

        [Fact]
        public void Put_KeyIsNull_Constructor_CacheIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<ICache<object>>();
            var sut = new CacheAdapter(cacheMock.Object);

            Assert.Throws<ArgumentNullException>(() => sut.Put(null, "abc"));
        }

        [Fact]
        public void Put_ValueIsNull_Constructor_CacheIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<ICache<object>>();
            var sut = new CacheAdapter(cacheMock.Object);

            Assert.Throws<ArgumentNullException>(() => sut.Put("key", null));
        }

        [Fact]
        public void Put_ParametersAreValid_CachePutIsCalled()
        {
            var keyToCheck = "key";
            var valueToCheck = "value";
            var cacheMock = new Mock<ICache<object>>();
            var sut = new CacheAdapter(cacheMock.Object);

            sut.Put(keyToCheck, valueToCheck);

            cacheMock.Verify(cache => cache.Put(It.Is<string>(key => key == keyToCheck), It.Is<object>(value => (string)value == valueToCheck)), Times.Once);
        }

        [Fact]
        public void Dispose_CacheDisposeIsCalled()
        {
            var cacheMock = new Mock<ICache<object>>();
            var sut = new CacheAdapter(cacheMock.Object);

            sut.Dispose();

            cacheMock.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
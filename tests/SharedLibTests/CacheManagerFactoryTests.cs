using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shared.Classes;

namespace SharedLibTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class CacheManagerFactoryTests
    {
        private const string TestCategoryName = "CacheManager Factory Tests";

        [TestCleanup]
        public void FinaliseTest()
        {

        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        public void Construct_Instance_Success()
        {
            CacheManagerFactory sut = new CacheManagerFactory();
            Assert.IsNotNull(sut);
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateCache_InvalidParam_CacheName_Null_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.CreateCache(null, new TimeSpan());
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateCache_InvalidParam_CacheName_EmptyString_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.CreateCache("", new TimeSpan());
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CreateCache_CacheAlreadyExists_Throws_InvalidOperationException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();
            try
            {
                ICacheManager testCache = sut.CreateCache("test123", new TimeSpan());

                Assert.IsNotNull(testCache);

                sut.CreateCache("test123", new TimeSpan(), true, false);
            }
            finally
            {
                sut.RemoveCache("test123");
            }
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CacheExists_InvalidParam_CacheName_Null_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.CacheExists(null);
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CacheExists_InvalidParam_CacheName_EmptyString_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.CacheExists("");
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetCache_InvalidParam_CacheName_Null_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.GetCache(null);
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetCache_InvalidParam_CacheName_EmptyString_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.GetCache("");
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetCacheIfExists_InvalidParam_CacheName_Null_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.GetCacheIfExists(null);
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetCacheIfExists_InvalidParam_CacheName_EmptyString_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.GetCacheIfExists("");
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RemoveCache_InvalidParam_CacheName_Null_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.RemoveCache(null);
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RemoveCache_InvalidParam_CacheName_EmptyString_Throws_ArgumentNullException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            sut.RemoveCache("");
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        public void CacheExists_CacheNotFound_Returns_False()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            Assert.IsFalse(sut.CacheExists("abcdefg"));
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        public void GetCacheIfExists_CacheNotFound_Returns_Null()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            Assert.IsNull(sut.GetCacheIfExists("abcdefg"));
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        public void GetCacheIfExists_CacheFound_Returns_ValidCacheManager()
        {
            CacheManagerFactory sut = new CacheManagerFactory();
            try
            {
                ICacheManager testCache = sut.CreateCache("test123a", new TimeSpan());

                Assert.IsNotNull(testCache);

                Assert.IsNotNull(sut.GetCacheIfExists("test123a"));
            }
            finally
            {
                sut.RemoveCache("test123a");
            }
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetCache_CacheNotFound_Throws_InvalidOperationException()
        {
            CacheManagerFactory sut = new CacheManagerFactory();

            Assert.IsNull(sut.GetCache("abcdefg"));
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        public void GetCache_CacheFound_Returns_ValidCacheManager()
        {
            CacheManagerFactory sut = new CacheManagerFactory();
            try
            {
                ICacheManager testCache = sut.CreateCache("test123a", new TimeSpan());

                Assert.IsNotNull(testCache);

                Assert.IsNotNull(sut.GetCache("test123a"));
            }
            finally
            {
                sut.RemoveCache("test123a");
            }
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        public void ClearAllCaches_RemovesAllCacheItems_Success()
        {
            CacheManagerFactory sut = new CacheManagerFactory();
            try
            {
                ICacheManager testCache = sut.CreateCache("clear all", new TimeSpan(), false, true);

                Assert.IsNotNull(testCache);
                testCache.Add("test 1", new CacheItem("test 1", true));

                Assert.AreEqual(1, testCache.Count);

                sut.ClearAllCaches();

                Assert.AreEqual(0, testCache.Count);
            }
            finally
            {
                sut.RemoveCache("clear all");
            }
        }

        [TestMethod]
        [TestCategory(TestCategoryName)]
        public void CleanAllCaches_RemovesExpiredCacheItems_Success()
        {
            CacheManagerFactory sut = new CacheManagerFactory();
            try
            {
                ICacheManager testCache = sut.CreateCache("clear all", new TimeSpan(0, 0, 0, 0, 15), false, true);

                Assert.IsNotNull(testCache);
                testCache.Add("test 1", new CacheItem("test 1", true));

                Assert.AreEqual(1, testCache.Count);

                Thread.Sleep(30);

                sut.CleanAllCaches();

                Assert.AreEqual(0, testCache.Count);
            }
            finally
            {
                sut.RemoveCache("clear all");
            }
        }
    }
}

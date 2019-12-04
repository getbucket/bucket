/*
 * This file is part of the Bucket package.
 *
 * (c) Yu Meng Han <menghanyu1994@gmail.com>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 *
 * Document: https://github.com/getbucket/bucket/wiki
 */

using Bucket.Cache;
using Bucket.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Bucket.Tests.Cache
{
    [TestClass]
    public class TestsExtensionICache
    {
        private CacheFileSystem cache;
        private string root;

        [TestInitialize]
        public void Initialize()
        {
            root = Helper.GetTestFolder<TestsExtensionICache>();
            cache = new CacheFileSystem(root, IONull.That);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(root, true);
        }

        [TestMethod]
        public void TestTryRead()
        {
            cache.Write("foo", "foo");
            Assert.IsTrue(cache.TryRead("foo", out string _));
            Assert.IsFalse(cache.TryRead("cache-not-exists", out _));
        }

        [TestMethod]
        public void TestWrite()
        {
            cache.Write("foo", "foo");
            Assert.AreEqual("foo", cache.Read("foo").ToText());
        }
    }
}

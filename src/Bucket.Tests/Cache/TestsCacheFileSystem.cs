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
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace Bucket.Tests.Cache
{
    [TestClass]
    public class TestsCacheFileSystem
    {
        private CacheFileSystem cache;
        private string root;
        private FileInfo[] files;
        private string zero;

        [TestInitialize]
        public void Initialize()
        {
            root = Helper.GetTestFolder<TestsCacheFileSystem>();

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            if (File.Exists(root))
            {
                File.Delete(root);
            }

            Directory.CreateDirectory(root);

            cache = new CacheFileSystem(root, IONull.That);
            zero = Str.Repeat("0", 1000);

            files = new FileInfo[4];
            for (var i = 0; i < 4; i++)
            {
                File.WriteAllText(GetCachedFileWithIndex(i), zero);
                files[i] = new FileInfo(GetCachedFileWithIndex(i));

                // Except the first one is outdated.
                if (i != 0)
                {
                    files[i].LastAccessTime = DateTime.Now.AddSeconds(-1000);
                }
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(root, true);
        }

        [TestMethod]
        public void TestGCOutdatedFiles()
        {
            cache.GC(600, 1024 * 1024 * 1024);

            Assert.AreEqual(true, File.Exists(GetCachedFileWithIndex(0)));
            for (var i = 1; i < 4; i++)
            {
                Assert.AreEqual(false, File.Exists(GetCachedFileWithIndex(i)), $"faild: {GetCachedFileWithIndex(i)}");
            }
        }

        [TestMethod]
        public void TestGCSubfolderFiles()
        {
            var subfolderCache = new CacheFileSystem(root, IONull.That, null);
            var path = Path.Combine(root, "foo/bar.zip");
            Directory.CreateDirectory(Path.Combine(root, "foo"));

            File.WriteAllText(path, zero);
            Assert.AreEqual(zero, subfolderCache.Read("foo/bar.zip").ToText());

            var file = new FileInfo(path);
            file.LastAccessTime = DateTime.Now.AddSeconds(-1000);

            cache.GC(600, 1024 * 1024 * 1024);
            Assert.AreEqual(false, subfolderCache.Contains("foo/bar.zip"));
        }

        [TestMethod]
        public void TestGCWhenCacheIsTooLarge()
        {
            cache.GC(int.MaxValue, 1500);

            Assert.AreEqual(true, File.Exists(GetCachedFileWithIndex(0)));
            for (var i = 1; i < 4; i++)
            {
                Assert.AreEqual(false, File.Exists(GetCachedFileWithIndex(i)), $"faild: {GetCachedFileWithIndex(i)}");
            }
        }

        [TestMethod]
        public void TestClearCache()
        {
            cache.Clear();

            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(false, File.Exists(GetCachedFileWithIndex(i)), $"faild: {GetCachedFileWithIndex(i)}");
            }
        }

        [TestMethod]
        public void TestWriteCache()
        {
            cache.Write("foo", ToStream("foo"));
            Assert.AreEqual("foo", cache.Read("foo").ToText());
        }

        [TestMethod]
        public void TestReadCacheNotTouch()
        {
            Assert.AreEqual(zero, cache.Read("cached.file1.zip", false).ToText());

            cache.GC(600, 1024 * 1024 * 1024);
            Assert.AreEqual(true, File.Exists(GetCachedFileWithIndex(0)));
            for (var i = 1; i < 4; i++)
            {
                Assert.AreEqual(false, File.Exists(GetCachedFileWithIndex(i)), $"faild: {GetCachedFileWithIndex(i)}");
            }
        }

        [TestMethod]
        public void TestTouchCacheTouch()
        {
            Assert.AreEqual(zero, cache.Read("cached.file1.zip").ToText());

            cache.GC(600, 1024 * 1024 * 1024);
            Assert.AreEqual(true, File.Exists(GetCachedFileWithIndex(0)));
            for (var i = 2; i < 4; i++)
            {
                Assert.AreEqual(false, File.Exists(GetCachedFileWithIndex(i)), $"faild: {GetCachedFileWithIndex(i)}");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileSystemException))]
        public void TestReadCacheNotExists()
        {
            cache.Read("cache-not-exists");
        }

        [TestMethod]
        public void TestTryRead()
        {
            cache.Write("foo", ToStream("foo"));

            Assert.AreEqual(true, cache.TryRead("foo", out Stream stream));
            using (stream)
            {
                Assert.AreEqual("foo", stream.ToText());
            }
        }

        [TestMethod]
        public void TestTryReadNotExists()
        {
            Assert.AreEqual(false, cache.TryRead("cache-not-exists", out Stream stream));
        }

        [TestMethod]
        public void TestContains()
        {
            Assert.AreEqual(false, cache.Contains("foo"));
            cache.Write("foo", ToStream("foo"));
            Assert.AreEqual(true, cache.Contains("foo"));
        }

        [TestMethod]
        public void TestDeleteCache()
        {
            Assert.AreEqual(zero, cache.Read("cached.file1.zip").ToText());
            cache.Delete("cached.file1.zip");
            Assert.AreEqual(false, cache.Contains("cached.file1.zip"));
        }

        private static Stream ToStream(string content)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private string GetCachedFileWithIndex(int index)
        {
            return Path.Combine(root, $"cached.file{index}.zip");
        }
    }
}

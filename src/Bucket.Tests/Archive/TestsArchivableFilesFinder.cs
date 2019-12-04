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

using Bucket.Archive;
using Bucket.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace Bucket.Tests.Archive
{
    [TestClass]
    public class TestsArchivableFilesFinder
    {
        [TestMethod]
        public void TestNormalFinderFiles()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            var finder = new ArchivableFilesFinder(
                "/path", new[] { "foo/", "bar/*.txt" }, fileSystem: fileSystemMock.Object);

            fileSystemMock.Setup((o) => o.GetContents("/path"))
                .Returns(new DirectoryContents(new[] { "foo", "bar", "baz" }, new[] { "file-1", "file-2" }));

            fileSystemMock.Setup((o) => o.GetContents("foo"))
                .Returns(new DirectoryContents(null, new[] { "foo/foo-1", "foo/foo-2.txt" }));

            fileSystemMock.Setup((o) => o.GetContents("bar"))
                .Returns(new DirectoryContents(null, new[] { "bar/bar-1", "bar/bar-2.txt" }));

            fileSystemMock.Setup((o) => o.GetContents("baz"))
                .Returns(new DirectoryContents(null, new[] { "baz/baz-1", "baz/baz-2.txt" }));

            var actual = finder.ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "foo/",
                    "foo/foo-1",
                    "foo/foo-2.txt",
                    "bar/",
                    "bar/bar-1",
                    "baz/",
                    "baz/baz-1",
                    "baz/baz-2.txt",
                    "file-1",
                    "file-2",
                }, actual);
        }
    }
}

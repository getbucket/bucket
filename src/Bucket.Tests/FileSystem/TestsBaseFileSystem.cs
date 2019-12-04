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

using Bucket.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.FileSystem
{
    [TestClass]
    public class TestsBaseFileSystem
    {
        [TestMethod]
        [DataRow("./bar", "/foo/bar", "/foo/bar", false)]
        [DataRow("./baz", "/foo/bar", "/foo/baz", false)]
        [DataRow("./baz", "/foo/bar/", "/foo/baz", false)]
        [DataRow("./", "/foo/bar", "/foo/bar", true)]
        [DataRow("../baz", "/foo/bar", "/foo/baz", true)]
        [DataRow("../baz", "/foo/bar/", "/foo/baz", true)]
        [DataRow("../baz", "C:/foo/bar/", "c:/foo/baz", true)]
        [DataRow("../vendor/foo/bar/baz", "/foo/bar/baz", "/foo/vendor/foo/bar/baz", false)]
        [DataRow("/bar/baz/foo", "/foo/bar/baz", "/bar/baz/foo", false)]
        [DataRow("/bar/baz/foo", "/foo/bar/baz", "/bar/baz/foo", true)]
        [DataRow("d:/bar/baz/foo", "c:/foo/bar/baz", "d:/bar/baz/foo", true)]
        [DataRow("../vendor/foo/bar/baz", "c:/bar/baz", "c:/vendor/foo/bar/baz", false)]
        [DataRow("../vendor/foo/bar/baz", "c:\\bar\\baz", "c:/vendor/foo/bar/baz", false)]
        [DataRow("d:/vendor/foo/bar/baz", "c:/bar/baz", "d:/vendor/foo/bar/baz", false)]
        [DataRow("d:/vendor/foo/bar/baz", "c:\\bar\\baz", "d:/vendor/foo/bar/baz", false)]
        [DataRow("./", "C:/Temp/test", "C:\\Temp", false)]
        [DataRow("./", "/tmp/test", "/tmp", false)]
        [DataRow("../", "C:/Temp/test/sub", "C:\\Temp", false)]
        [DataRow("../", "/tmp/test/sub", "/tmp", false)]
        [DataRow("../../", "/tmp/test/sub", "/tmp", true)]
        [DataRow("../../", "c:/tmp/test/sub", "c:/tmp", true)]
        [DataRow("test", "/tmp", "/tmp/test", false)]
        [DataRow("test", "C:/Temp", "C:\\Temp\\test", false)]
        [DataRow("test", "C:/Temp", "c:\\Temp\\test", false)]
        [DataRow("./", "/tmp/test/./", "/tmp/test", true)]
        [DataRow("../test", "/tmp/test/../vendor", "/tmp/test", true)]
        [DataRow("../test", "/tmp/test/.././vendor", "/tmp/test", true)]
        [DataRow("../test", "C:/Temp", "c:\\Temp\\..\\..\\test", true)]
        [DataRow("./test", "C:/Temp/../..", "c:\\Temp\\..\\..\\test", true)]
        [DataRow("d:/test", "C:/Temp/../..", "D:\\Temp\\..\\..\\test", true)]
        [DataRow("/test", "/tmp", "/tmp/../../test", true)]
        [DataRow("../bar_vendor", "/foo/bar", "/foo/bar_vendor", true)]
        [DataRow("../bar", "/foo/bar_vendor", "/foo/bar", true)]
        [DataRow("../bar/src", "/foo/bar_vendor", "/foo/bar/src", true)]
        [DataRow("../../bar/src/lib", "/foo/bar_vendor/src2", "/foo/bar/src/lib", true)]
        [DataRow("foo/bar", "C:/", "C:/foo/bar/", true)]
        public void TestGetRelativePath(string expected, string from, string to, bool isDirectory)
        {
            Assert.AreEqual(expected, BaseFileSystem.GetRelativePath(from, to, isDirectory));
        }

        [TestMethod]
        [DataRow("../foo", "../foo")]
        [DataRow("c:/foo/bar", "c:/foo//bar")]
        [DataRow("C:/foo/bar", "C:/foo/./bar")]
        [DataRow("C:/foo/bar", "C://foo//bar")]
        [DataRow("C:/foo/bar", "C:///foo//bar")]
        [DataRow("C:/bar", "C:/foo/../bar")]
        [DataRow("/bar", "/foo/../bar/")]
        [DataRow("foo://c:/Foo", "foo://c:/Foo/Bar/..")]
        [DataRow("foo://c:/Foo", "foo://c:///Foo/Bar/..")]
        [DataRow("foo://c:/", "foo://c:/Foo/Bar/../../../..")]
        [DataRow("/", "/Foo/Bar/../../../..")]
        [DataRow("/", "/")]
        [DataRow("/", "//")]
        [DataRow("/", "///")]
        [DataRow("/Foo", "///Foo")]
        [DataRow("c:/", "c:\\")]
        [DataRow("../src", "Foo/Bar/../../../src")]
        [DataRow("c:../b", "c:.\\..\\a\\..\\b")]
        [DataRow("foo://c:../Foo", "foo://c:../Foo")]
        public void TestGetNormalizePath(string expected, string actual)
        {
            Assert.AreEqual(expected, BaseFileSystem.GetNormalizePath(actual));
        }

        [TestMethod]
        public void TestDiskRootPath()
        {
            var mockFileSystem = new Mock<BaseFileSystem>() { CallBase = true };
            mockFileSystem.Object.SetRootPath(@"D:\\");
            Assert.AreEqual("D:/foo.json", mockFileSystem.Object.ApplyRootPath("./foo.json"));
        }
    }
}

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
using System;
using System.IO;
using System.Text;

namespace Bucket.Tests.FileSystem
{
    [TestClass]
    public class TestsFileSystemLocal
    {
        private FileSystemLocal fileSystem;
        private string root;

        [TestInitialize]
        public void Initialize()
        {
            root = Helper.GetTestFolder<TestsFileSystemLocal>();
            fileSystem = new FileSystemLocal(root);

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            if (File.Exists(root))
            {
                File.Delete(root);
            }

            Directory.CreateDirectory(root);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(root, true);
        }

        [TestMethod]
        public void TestAltDirectorySeparatorChar()
        {
            var foo = new FileSystemLocal("c:/folder/foo");
            Assert.AreNotEqual(null, foo);
        }

        [TestMethod]
        public void TestRead()
        {
            WriteAllText("foo", "foo");
            Assert.AreEqual("foo", Read("foo"));
        }

        [TestMethod]
        public void TestReadSubfolder()
        {
            WriteAllText("foo", "foo");
            WriteAllText("bar/bar", "bar");

            Assert.AreEqual("foo", Read("foo"));
            Assert.AreEqual("bar", Read("bar/bar"));
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void TestUnauthorizedRead()
        {
            fileSystem.Read("../foo");
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void TestReadFileNotExists()
        {
            fileSystem.Read("foo");
        }

        [TestMethod]
        public void TestWrite()
        {
            fileSystem.Write("foo", ToStream("foo"));
            Assert.AreEqual("foo", Read("foo"));
        }

        [TestMethod]
        public void TestWriteWithAppend()
        {
            fileSystem.Write("foo", ToStream("foo"));
            fileSystem.Write("foo", ToStream("bar"), true);

            Assert.AreEqual("foobar", Read("foo"));
        }

        [TestMethod]
        public void TestWithoutAppend()
        {
            fileSystem.Write("foo", ToStream("foo"));
            fileSystem.Write("foo", ToStream("bar"));

            Assert.AreEqual("bar", Read("foo"));
        }

        [TestMethod]
        public void TestWriteWithSubfolder()
        {
            fileSystem.Write("foo/bar", ToStream("foobar"));

            Assert.AreEqual("foobar", Read("foo/bar"));
        }

        [TestMethod]
        [ExpectedException(typeof(FileSystemException))]
        public void TestWriteExistsFolder()
        {
            fileSystem.Write("foo/bar", ToStream("foobar"));
            fileSystem.Write("foo", ToStream("foo"));
        }

        [TestMethod]
        [DataRow("../foo")]
        [DataRow("foo/../../bar")]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void TestUnauthorizedWrite(string path)
        {
            fileSystem.Write(path, ToStream("foo"));
        }

        [TestMethod]
        public void TestWriteStreamNotClosed()
        {
            using (var foo = new MemoryStream(Encoding.UTF8.GetBytes("foo")))
            {
                Assert.AreEqual(true, foo.CanRead);
                Assert.AreEqual(true, foo.CanWrite);

                fileSystem.Write("foo", foo);

                Assert.AreEqual(true, foo.CanRead);
                Assert.AreEqual(true, foo.CanWrite);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestInputWriteStreamIsClosed()
        {
            var foo = new MemoryStream(Encoding.UTF8.GetBytes("foo"));
            foo.Close();

            fileSystem.Write("foo", foo);
        }

        [TestMethod]
        public void TestExists()
        {
            WriteAllText("foo", "foo");
            WriteAllText("bar/bar", "bar");

            Assert.AreEqual(true, fileSystem.Exists("foo"));
            Assert.AreEqual(true, fileSystem.Exists("bar/bar"));
            Assert.AreEqual(true, fileSystem.Exists("bar"));

            Assert.AreEqual(false, fileSystem.Exists("baz"));
            Assert.AreEqual(false, fileSystem.Exists("bar/baz"));

            Assert.AreEqual(false, fileSystem.Exists("bar", FileSystemOptions.File));
            Assert.AreEqual(true, fileSystem.Exists("bar", FileSystemOptions.Directory));

            Assert.AreEqual(true, fileSystem.Exists("foo", FileSystemOptions.File));
            Assert.AreEqual(false, fileSystem.Exists("foo", FileSystemOptions.Directory));
        }

        [TestMethod]
        public void TestMove()
        {
            WriteAllText("foo", "foo");

            fileSystem.Move("foo", "bar");

            Assert.AreEqual(false, fileSystem.Exists("foo"));
            Assert.AreEqual("foo", Read("bar"));
        }

        [TestMethod]
        public void TestMoveFolder()
        {
            WriteAllText("bar/foo", "foo");
            WriteAllText("bar/bar", "bar");

            fileSystem.Move("bar", "baz");

            Assert.AreEqual(false, fileSystem.Exists("bar"));
            Assert.AreEqual(true, fileSystem.Exists("baz/foo"));
            Assert.AreEqual(true, fileSystem.Exists("baz/bar"));
        }

        [TestMethod]
        [ExpectedException(typeof(FileSystemException))]
        public void TestMoveFolderToExistsFile()
        {
            WriteAllText("bar/foo", "foo");
            WriteAllText("bar/bar", "bar");
            WriteAllText("baz", "baz");

            fileSystem.Move("bar", "baz");
        }

        [TestMethod]
        [ExpectedException(typeof(FileSystemException))]
        public void TestMoveFileToExistsFolder()
        {
            WriteAllText("bar/foo", "foo");
            WriteAllText("bar/bar", "bar");
            WriteAllText("baz", "baz");

            fileSystem.Move("baz", "bar");
        }

        [TestMethod]
        [ExpectedException(typeof(FileSystemException))]
        public void TestMoveFileToExistsFile()
        {
            WriteAllText("foo", "foo");
            WriteAllText("bar", "bar");

            fileSystem.Move("foo", "bar");
        }

        [TestMethod]
        [ExpectedException(typeof(FileSystemException))]
        public void TestMoveFolderToExistsFolder()
        {
            WriteAllText("foo/bar", "foo");
            WriteAllText("bar/foo", "bar");

            fileSystem.Move("foo", "bar");
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void TestUnauthorizedMove()
        {
            fileSystem.Move("foo", "../bar");
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void TestMoveNotExistsFile()
        {
            fileSystem.Move("foo", "bar");
        }

        [TestMethod]
        public void TestSubfolderMove()
        {
            WriteAllText("foo/bar/baz", "foobarbaz");
            fileSystem.Move("foo/bar/baz", "foo/baz");

            Assert.AreEqual(true, fileSystem.Exists("foo/baz"));
            Assert.AreEqual("foobarbaz", Read("foo/baz"));
        }

        [TestMethod]
        public void TestCopy()
        {
            WriteAllText("foo", "foo");

            fileSystem.Copy("foo", "bar");

            Assert.AreEqual("foo", Read("foo"));
            Assert.AreEqual("foo", Read("bar"));
        }

        [TestMethod]
        public void TestCopyOverwrite()
        {
            WriteAllText("foo", "foo");
            WriteAllText("bar", "bar");

            fileSystem.Copy("foo", "bar");
            Assert.AreEqual("foo", Read("foo"));
            Assert.AreEqual("foo", Read("bar"));
        }

        [TestMethod]
        [ExpectedException(typeof(FileSystemException))]
        public void TestCopyWithoutOverwrite()
        {
            WriteAllText("foo", "foo");
            WriteAllText("bar", "bar");

            fileSystem.Copy("foo", "bar", false);
        }

        [TestMethod]
        public void TestFolderCopyOverwrite()
        {
            WriteAllText("foo/bar", "bar");
            WriteAllText("foo/baz", "baz");
            WriteAllText("bar/foo", "barfoo");
            WriteAllText("bar/baz", "barbaz");

            fileSystem.Copy("foo", "bar", true);

            Assert.AreEqual("barfoo", Read("bar/foo"));
            Assert.AreEqual("baz", Read("bar/baz"));
            Assert.AreEqual("bar", Read("bar/bar"));
        }

        [TestMethod]
        [DataRow("foo", "../bar")]
        [DataRow("../foo", "bar")]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void TestUnauthorizedCopy(string source, string target)
        {
            fileSystem.Copy(source, target);
        }

        [TestMethod]
        public void TestGetContents()
        {
            WriteAllText("foo/bar", "bar");
            WriteAllText("foo/baz", "baz");
            WriteAllText("bar/foo", "barfoo");
            WriteAllText("bar/baz", "barbaz");

            var contents = fileSystem.GetContents();

            Assert.AreEqual(2, contents.GetDirectories().Length);
            Assert.AreEqual(0, contents.GetFiles().Length);

            CollectionAssert.Contains(contents.GetDirectories(), "foo");
            CollectionAssert.Contains(contents.GetDirectories(), "bar");
        }

        [TestMethod]
        public void TestGetContentsFile()
        {
            WriteAllText("foo/bar/baz", "foo/bar/baz");
            WriteAllText("baz", "baz");

            var contents = fileSystem.GetContents();
            Assert.AreEqual(1, contents.GetDirectories().Length);
            CollectionAssert.Contains(contents.GetFiles(), "baz");

            contents = fileSystem.GetContents("foo/bar");
            Assert.AreEqual(0, contents.GetDirectories().Length);
            CollectionAssert.Contains(contents.GetFiles(), "foo/bar/baz");
        }

        [TestMethod]
        public void TestGetContentsWithFile()
        {
            WriteAllText("baz", "baz");
            var contents = fileSystem.GetContents("baz");
            Assert.AreEqual(0, contents.GetDirectories().Length);
            CollectionAssert.Contains(contents.GetFiles(), "baz");
        }

        [TestMethod]
        public void TestGetContentsWithNotExistsFile()
        {
            var contents = fileSystem.GetContents("baz");
            Assert.AreEqual(0, contents.GetDirectories().Length);
            Assert.AreEqual(0, contents.GetFiles().Length);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void TestUnauthorizedGetContents()
        {
            fileSystem.GetContents("../foo");
        }

        [TestMethod]
        public void TestGetMetaData()
        {
            WriteAllText("foo", "foo");
            WriteAllText("bar/baz", "barbaz");

            Assert.AreNotEqual(null, fileSystem.GetMetaData("foo"));
            Assert.AreNotEqual(null, fileSystem.GetMetaData("bar/baz"));
            Assert.AreNotEqual(null, fileSystem.GetMetaData("bar"));
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void TestGetMetaDataFileNotExists()
        {
            fileSystem.GetMetaData("foo");
        }

        [TestMethod]
        public void TestGetMetaDataInfoWithFile()
        {
            WriteAllText("foo", "foo");

            var meta = fileSystem.GetMetaData("foo");

            Assert.AreEqual(3, meta.Size);
            Assert.AreEqual(false, meta.IsDirectory);
            Assert.AreEqual("application/octet-stream", meta.MimeType);
            Assert.AreEqual("foo", meta.Name);
            Assert.AreEqual(string.Empty, meta.ParentDirectory);
            Assert.AreEqual("foo", meta.Path);
        }

        [TestMethod]
        public void TestGetMetaDataInfoWithFolder()
        {
            WriteAllText("foo/bar", "bar");
            WriteAllText("foo/baz", "baz");

            var meta = fileSystem.GetMetaData("foo");

            Assert.AreEqual(6, meta.Size);
            Assert.AreEqual(true, meta.IsDirectory);
            Assert.AreEqual(string.Empty, meta.MimeType);
            Assert.AreEqual("foo", meta.Name);
            Assert.AreEqual(string.Empty, meta.ParentDirectory);
            Assert.AreEqual("foo", meta.Path);
        }

        [TestMethod]
        public void TestGetMetaDataInfoWithSubfolder()
        {
            WriteAllText("foo/bar/baz", "baz");
            WriteAllText("foo/bar/bar", "bar");

            var meta = fileSystem.GetMetaData("foo/bar");

            Assert.AreEqual(6, meta.Size);
            Assert.AreEqual(true, meta.IsDirectory);
            Assert.AreEqual(string.Empty, meta.MimeType);
            Assert.AreEqual("bar", meta.Name);
            Assert.AreEqual("foo", meta.ParentDirectory);
            Assert.AreEqual("foo/bar", meta.Path);
        }

        [TestMethod]
        public void TestSetAccessTime()
        {
            WriteAllText("foo", "foo");

            var meta = fileSystem.GetMetaData("foo");
            var expected = DateTime.Now.AddDays(-1);

            meta.LastAccessTime = expected;

            meta = fileSystem.GetMetaData("foo");
            Assert.AreEqual(expected, meta.LastAccessTime);
        }

        [TestMethod]
        [DataRow(true, "file://c:/path/to/foo.txt")]
        [DataRow(true, "file:///path/to/foo.txt")]
        [DataRow(true, "C:/path/to/foo.txt")]
        [DataRow(true, "C:\\path\\to\\foo.txt")]
        [DataRow(true, "/C:/path/to/foo.txt")]
        [DataRow(true, "/path/to/foo.txt")]
        [DataRow(true, "../path/to/foo.txt")]
        [DataRow(false, "file:////path/to/foo")]
        [DataRow(false, "//path/to/foo.txt")]
        [DataRow(false, "git@github.com:foo/bar.git")]
        [DataRow(false, "https://github.com/foo/bar.git")]
        public void TestIsLocalPath(bool expected, string path)
        {
            Assert.AreEqual(expected, FileSystemLocal.IsLocalPath(path));
        }

        private static Stream ToStream(string content)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private string ApplyRoot(string path)
        {
            return Path.Combine(root, path);
        }

        private void WriteAllText(string path, string content)
        {
            path = ApplyRoot(path);
            var parentPath = Directory.GetParent(path).FullName;
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            File.WriteAllText(path, content);
        }

        private string Read(string path)
        {
            using (var stream = fileSystem.Read(path))
            {
                return stream.ToText();
            }
        }
    }
}

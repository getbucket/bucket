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
using Bucket.Installer;
using Bucket.IO;
using Bucket.Package;
using Bucket.Tester;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;

namespace Bucket.Tests.Installer
{
    [TestClass]
    public class TestsInstallerBinary
    {
        private Mock<IFileSystem> fileSystemMock;
        private InstallerBinary installer;
        private TesterIOConsole tester;
        private IIO io;

        [TestInitialize]
        public void Initialize()
        {
            fileSystemMock = new Mock<IFileSystem>();
            tester = new TesterIOConsole();
            io = tester.Mock();
            installer = new InstallerBinary(io, "vendor/bin", "auto", fileSystemMock.Object);
        }

        [TestMethod]
        public void TestInstall()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetBinaries()).Returns(new[] { "foo/bar.bat", "foo/baz" });

            var expected = GenerateBin("foo/bar.bat");
            fileSystemMock.Setup((o) => o.Exists(expected, FileSystemOptions.File))
                .Returns(true);
            expected = GenerateBin("foo/baz");
            fileSystemMock.Setup((o) => o.Exists(expected, FileSystemOptions.File))
                .Returns(true);
            expected = GenerateLink("foo/bar.bat");
            fileSystemMock.Setup((o) => o.Exists(expected, FileSystemOptions.File))
                .Returns(false);
            fileSystemMock.Setup((o) => o.Write(expected, It.IsAny<Stream>(), false));
            expected = GenerateLink("foo/baz");
            fileSystemMock.Setup((o) => o.Exists(expected, FileSystemOptions.File))
                .Returns(false);

            installer.Install(packageMock.Object, "vendor/package");

            fileSystemMock.Verify((o) => o.Write(expected, It.IsAny<Stream>(), false));
            fileSystemMock.Verify((o) => o.Write(expected + ".bat", It.IsAny<Stream>(), false));

            expected = GenerateLink("foo/bar.bat");
            fileSystemMock.Verify((o) => o.Write(expected, It.IsAny<Stream>(), false));
            fileSystemMock.Verify((o) => o.Write(expected + ".bat", It.IsAny<Stream>(), false), Times.Never);
        }

        [TestMethod]
        public void TestRemove()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetBinaries()).Returns(new[] { "foo/bar.bat", "foo/baz" });

            fileSystemMock.Setup((o) => o.Exists(It.IsAny<string>(), It.IsAny<FileSystemOptions>()))
                .Returns(true);

            installer.Remove(packageMock.Object);

            var expected = GenerateLink("foo/bar.bat");
            fileSystemMock.Verify((o) => o.Delete(expected));
            fileSystemMock.Verify((o) => o.Delete(expected + ".bat"));

            expected = GenerateLink("foo/baz");
            fileSystemMock.Verify((o) => o.Delete(expected));
            fileSystemMock.Verify((o) => o.Delete(expected + ".bat"));

            expected = Path.Combine(Environment.CurrentDirectory, GetBinDir());
            fileSystemMock.Verify((o) => o.Delete(expected));
        }

        [TestMethod]
        public void TestDetermineBinaryCaller()
        {
            Assert.AreEqual("call", InstallerBinary.DetermineBinaryCaller("foo.exe"));
            Assert.AreEqual("call", InstallerBinary.DetermineBinaryCaller("foo.bat"));
            Assert.AreEqual("dotnet", InstallerBinary.DetermineBinaryCaller("foo.bar"));
        }

        [TestMethod]
        public void TestDetermineBinaryCallerWithUsrBinEnv()
        {
            fileSystemMock.Setup((o) => o.Read("foo"))
                .Returns(() => "#!/usr/bin/env sh".ToStream());

            Assert.AreEqual("sh", InstallerBinary.DetermineBinaryCaller("foo", fileSystemMock.Object));
        }

        private static string GetBinDir()
        {
            return "vendor/bin";
        }

        private string GenerateBin(string bin, string package = "vendor/package")
        {
            return Path.Combine(Environment.CurrentDirectory, package, bin);
        }

        private string GenerateLink(string bin, string binDir = null)
        {
            return Path.Combine(Path.Combine(Environment.CurrentDirectory, binDir ?? GetBinDir()).TrimEnd('/', '\\'), Path.GetFileName(bin));
        }
    }
}

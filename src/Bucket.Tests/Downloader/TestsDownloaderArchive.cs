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

using Bucket.Configuration;
using Bucket.Downloader;
using Bucket.Downloader.Transport;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.Package;
using Bucket.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;

namespace Bucket.Tests.Downloader
{
    [TestClass]
    public class TestsDownloaderArchive
    {
        private Mock<ITransport> transport;
        private Mock<Config> config;
        private Mock<IFileSystem> fileSystem;
        private Mock<DownloaderArchive> downloader;
        private TesterIOConsole tester;
        private string root;

        [TestInitialize]
        public void Initialize()
        {
            transport = new Mock<ITransport>();
            config = new Mock<Config>(true, null);
            fileSystem = new Mock<IFileSystem>();
            tester = new TesterIOConsole();
            downloader = new Mock<DownloaderArchive>(
                tester.Mock(),
                config.Object,
                transport.Object,
                null, null, fileSystem.Object)
            { CallBase = true };

            root = Helper.GetTestFolder<TestsDownloaderArchive>();
            config.Setup((o) => o.Get(It.IsIn(Settings.VendorDir), ConfigOptions.None))
                .Returns(root);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void TestInstall()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.ToString())
                .Returns("foo/bar-1.2.0");
            packageMock.Setup((o) => o.GetName())
               .Returns("foo/bar");
            packageMock.Setup((o) => o.GetVersionPrettyFull(true))
                .Returns("1.2.0 1234567");
            packageMock.Setup((o) => o.GetDistUri())
                .Returns("https://example.com/foo/bar.zip");
            packageMock.Setup((o) => o.GetDistReference())
                .Returns("1234567890123456789012345678901234567890");

            fileSystem.SetupSequence((o) => o.GetContents(It.IsAny<string>()))
                .Returns(new DirectoryContents(new[] { "vendor" }, new[] { ".DS_Store" }))
                .Returns(new DirectoryContents(new[] { "vendor/srcs" }, new[] { "vendor/SAMPLE.md" }));

            downloader.Object.Install(packageMock.Object, "/path", true);

            fileSystem.Verify((o) => o.Delete("/path"), Times.Once);
            fileSystem.Verify((o) => o.Move(It.IsRegex(@"vendor$"), "/path"));

            StringAssert.Contains(
                tester.GetDisplay(),
                "  - Installing foo/bar (1.2.0 1234567): Extracting archive");
        }

        [TestMethod]
        public void TestOnlySingleFileInDirectionary()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetDistUri())
                .Returns("https://example.com/foo/bar.zip");

            fileSystem.SetupSequence((o) => o.GetContents(It.IsAny<string>()))
                .Returns(new DirectoryContents(Array.Empty<string>(), new[] { "bucket.json" }));

            downloader.Object.Install(packageMock.Object, "/path", true);
            fileSystem.Verify((o) => o.Move(It.IsRegex(@"[a-zA-Z0-9]{7}$"), "/path"));
        }

        [TestMethod]
        public void TestExtractFaildClearLastCache()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.ToString())
                .Returns("foo/bar-1.2.0");
            packageMock.Setup((o) => o.GetDistUri())
                .Returns("https://example.com/foo/bar.zip");

            downloader.Setup((o) => o.Extract(packageMock.Object, It.IsAny<string>(), It.IsAny<string>()))
                .Throws<RuntimeException>();

            Assert.ThrowsException<RuntimeException>(() =>
            {
                downloader.Object.Install(packageMock.Object, "/path", true);
            });

            downloader.Verify((o) => o.ClearLastCacheWrite(packageMock.Object), Times.Once);
            fileSystem.Verify((o) => o.Delete("/path"), Times.Exactly(2));
        }
    }
}

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
using Bucket.Configuration;
using Bucket.Downloader;
using Bucket.Downloader.Transport;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.Package;
using Bucket.Tester;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Bucket.Tests.Downloader
{
    [TestClass]
    public class TestsDownloaderFile
    {
        private Mock<ITransport> transport;
        private Mock<Config> config;
        private DownloaderFile downloader;
        private TesterIOConsole tester;
        private Mock<IFileSystem> fileSystem;
        private Mock<ICache> cache;

        [TestInitialize]
        public void Initialize()
        {
            tester = new TesterIOConsole();
            transport = new Mock<ITransport>();
            config = new Mock<Config>(true, null);
            fileSystem = new Mock<IFileSystem>();
            cache = new Mock<ICache>();
            downloader = new DownloaderFile(tester.Mock(), config.Object, transport.Object, null, cache.Object, fileSystem.Object);

            config.Setup((o) => o.Get(It.IsIn(Settings.VendorDir), ConfigOptions.None))
                .Returns($"/vendor");
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "The given package is missing url information.")]
        public void TestDownloadForPackageWithoutDistUri()
        {
            var packageMock = new Mock<IPackage>();
            downloader.Download(packageMock.Object, "/path");
        }

        [TestMethod]
        public void TestGetDownloadedFilePath()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.ToString()).Returns("foo/bar-1.2.0");
            packageMock.Setup((o) => o.GetDistUri()).Returns("https://example.com/foo/bar.zip");
            packageMock.Setup((o) => o.GetDistReference()).Returns("abc123");
            packageMock.Setup((o) => o.GetDistShasum()).Returns("abc123");

            var package = packageMock.Object;
            var filename = Security.Md5($"/path{package}{package.GetDistReference()}{package.GetDistShasum()}");
            StringAssert.EndsWith(downloader.GetDownloadedFilePath(package, "/path"), $"{filename}.zip");
        }

        [TestMethod]
        [ExpectedException(typeof(UnexpectedException), "* could not be saved to *, make sure the directory is writable and you have internet connectivity.")]
        public async Task TestDownloadButFileCouldNotSaved()
        {
            var packageMock = new Mock<IPackage>();
            var distUri = "https://example.com/foo/bar.zip";
            packageMock.Setup((o) => o.GetName()).Returns("foo/bar");
            packageMock.Setup((o) => o.GetDistUri()).Returns(distUri);
            packageMock.Setup((o) => o.GetDistUris()).Returns(new[] { distUri });

            var target = downloader.GetDownloadedFilePath(packageMock.Object, "/path");
            transport.Setup((o) => o.Copy("https://example.com/foo/bar.zip", It.IsIn(target), It.IsAny<IProgress<ProgressChanged>>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Verifiable();

            try
            {
                await downloader.Download(packageMock.Object, "/path");
            }
            finally
            {
                Moq.Mock.VerifyAll(transport);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(UnexpectedException), "The checksum verification of the file failed (downloaded from *)")]
        public async Task TestDownloadFileWithInvalidChecksum()
        {
            var packageMock = new Mock<IPackage>();
            var distUri = "https://example.com/foo/bar.zip";
            packageMock.Setup((o) => o.GetName()).Returns("foo/bar");
            packageMock.Setup((o) => o.GetDistUri()).Returns(distUri);
            packageMock.Setup((o) => o.GetDistUris()).Returns(new[] { distUri });
            packageMock.Setup((o) => o.GetDistShasum()).Returns("invalid");

            var downloadedFilePath = downloader.GetDownloadedFilePath(packageMock.Object, "/path");
            fileSystem.Setup((o) => o.Exists(It.IsIn(downloadedFilePath), FileSystemOptions.File))
                .Returns(true);

            fileSystem.Setup((o) => o.Read(It.IsIn(downloadedFilePath)))
                .Returns(new MemoryStream());

            await downloader.Download(packageMock.Object, "/path");
        }

        [TestMethod]
        public void TestShowsAppropriateMessage()
        {
            var initialMock = new Mock<IPackage>();
            initialMock.Setup((o) => o.GetVersion()).Returns("1.2.0.0");
            initialMock.Setup((o) => o.GetVersionPrettyFull(true)).Returns("1.2.0");

            var targetMock = new Mock<IPackage>();
            var distUri = "https://example.com/foo/bar.zip";
            targetMock.Setup((o) => o.GetName()).Returns("foo/bar");
            targetMock.Setup((o) => o.GetVersion()).Returns("1.6.0.0");
            targetMock.Setup((o) => o.GetVersionPrettyFull(true)).Returns("1.6.0");
            targetMock.Setup((o) => o.GetDistUri()).Returns(distUri);
            targetMock.Setup((o) => o.GetDistUris()).Returns(new[] { distUri });

            var downloadedFilePath = downloader.GetDownloadedFilePath(targetMock.Object, "/path");
            fileSystem.Setup((o) => o.Delete("/path"))
                .Verifiable();
            fileSystem.Setup((o) => o.Move(It.IsIn(downloadedFilePath), It.IsIn(Path.Combine("/path", "bar.zip"))))
                .Verifiable();

            downloader.Update(initialMock.Object, targetMock.Object, "/path");

            Moq.Mock.VerifyAll(fileSystem);
            StringAssert.Contains(tester.GetDisplay(), "  - Updating foo/bar (1.2.0 => 1.6.0):");
        }
    }
}

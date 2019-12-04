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
using Bucket.FileSystem;
using Bucket.Installer;
using Bucket.IO;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Tester;
using GameBox.Console.Exception;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Bucket.Tests.Installer
{
    [TestClass]
    public class TestsInstallerLibrary
    {
        private Bucket bucket;
        private Config config;
        private string root;
        private string vendorDir;
        private string binDir;
        private Mock<DownloadManager> downloadManager;
        private Mock<IRepositoryInstalled> repository;
        private TesterIOConsole tester;
        private IIO io;

        [TestInitialize]
        public void Initialize()
        {
            bucket = new Bucket();
            config = new Config();

            bucket.SetConfig(config);

            root = Helper.GetTestFolder<TestsInstallerLibrary>();
            vendorDir = Path.Combine(root, "vendor");
            binDir = Path.Combine(root, "bin");

            var merged = new JObject() { { "config", new JObject() } };
            merged["config"][Settings.VendorDir] = vendorDir;
            merged["config"][Settings.BinDir] = binDir;

            config.Merge(merged);

            downloadManager = new Mock<DownloadManager>(null, false);
            bucket.SetDownloadManager(downloadManager.Object);

            repository = new Mock<IRepositoryInstalled>();
            tester = new TesterIOConsole();
            io = tester.Mock();

            Cleanup();

            Directory.CreateDirectory(vendorDir);
            Directory.CreateDirectory(binDir);
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
        public void TestInstallerCreationShouldNotCreateVendorDirectory()
        {
            Directory.Delete(vendorDir);

            _ = new InstallerLibrary(io, bucket);
            Assert.IsFalse(Directory.Exists(vendorDir));
        }

        [TestMethod]
        public void TestInstallerCreationShouldNotCreateBinDirectory()
        {
            Directory.Delete(binDir);

            _ = new InstallerLibrary(io, bucket);
            Assert.IsFalse(Directory.Exists(binDir));
        }

        [TestMethod]
        public void TestIsInstalled()
        {
            var installer = new InstallerLibrary(io, bucket);
            var packageMock = new Mock<IPackage>();

            repository.SetupSequence((o) => o.HasPackage(packageMock.Object))
                .Returns(true)
                .Returns(false);

            Assert.IsTrue(installer.IsInstalled(repository.Object, packageMock.Object));
            Assert.IsFalse(installer.IsInstalled(repository.Object, packageMock.Object));
        }

        [TestMethod]
        public void TestInstall()
        {
            var installer = new InstallerLibrary(io, bucket);
            var packageMock = new Mock<IPackage>();

            packageMock.Setup((o) => o.GetNamePretty()).Returns("foo/bar");
            packageMock.Setup((o) => o.Clone()).Returns(packageMock.Object);

            installer.Install(repository.Object, packageMock.Object);

            downloadManager.Verify((o) => o.Install(packageMock.Object, vendorDir + "/foo/bar"));
            repository.Verify((o) => o.AddPackage(packageMock.Object));
        }

        [TestMethod]
        public void TestUpdate()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            var installer = new InstallerLibrary(io, bucket, fileSystem: fileSystemMock.Object);
            var initial = new Mock<IPackage>();
            var target = new Mock<IPackage>();

            initial.Setup((o) => o.GetNamePretty()).Returns("foo/old");
            target.Setup((o) => o.GetNamePretty()).Returns("foo/new");
            target.Setup((o) => o.Clone()).Returns(target.Object);

            repository.SetupSequence((o) => o.HasPackage(It.IsAny<IPackage>()))
                .Returns(true).Returns(false);

            installer.Update(repository.Object, initial.Object, target.Object);

            downloadManager.Verify((o) => o.Update(initial.Object, target.Object, vendorDir + "/foo/new"));
            fileSystemMock.Verify((o) => o.Move(vendorDir + "/foo/old", vendorDir + "/foo/new"));
            repository.Verify((o) => o.RemovePackage(initial.Object));
            repository.Verify((o) => o.AddPackage(target.Object));
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidArgumentException), "Package is not installed")]
        public void TestUpdatePackageNotInstalled()
        {
            var installer = new InstallerLibrary(io, bucket);
            repository.Setup((o) => o.HasPackage(It.IsAny<IPackage>())).Returns(false);

            var initial = new Mock<IPackage>();
            var target = new Mock<IPackage>();

            installer.Update(repository.Object, initial.Object, target.Object);
        }

        [TestMethod]
        public void TestUninstall()
        {
            var installer = new InstallerLibrary(io, bucket);
            var packageMock = new Mock<IPackage>();

            packageMock.Setup((o) => o.GetName()).Returns("foo");
            packageMock.Setup((o) => o.GetNamePretty()).Returns("foo");
            repository.SetupSequence((o) => o.HasPackage(packageMock.Object))
                .Returns(true).Returns(false);

            installer.Uninstall(repository.Object, packageMock.Object);

            downloadManager.Verify((o) => o.Remove(packageMock.Object, vendorDir + "/foo"));
            repository.Verify((o) => o.RemovePackage(packageMock.Object));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException), "Package is not installed")]
        public void TestUninstallNotInstalledPackage()
        {
            var installer = new InstallerLibrary(io, bucket);
            var packageMock = new Mock<IPackage>();
            repository.Setup((o) => o.HasPackage(packageMock.Object)).Returns(false);

            installer.Uninstall(repository.Object, packageMock.Object);
        }

        [TestMethod]
        public void TestUninstallDeleteVendorDirIfDirIsEmpty()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            var installer = new InstallerLibrary(io, bucket, fileSystem: fileSystemMock.Object);
            var packageMock = new Mock<IPackage>();
            repository.Setup((o) => o.HasPackage(packageMock.Object)).Returns(true);
            packageMock.Setup((o) => o.GetName()).Returns("foo/bar");

            var fullVendorDir = Path.GetDirectoryName(installer.GetInstallPath(packageMock.Object));
            fileSystemMock.Setup((o) => o.Exists(fullVendorDir, FileSystemOptions.Directory)).Returns(true);

            installer.Uninstall(repository.Object, packageMock.Object);

            fileSystemMock.Verify((o) => o.Delete(fullVendorDir));
        }

        [TestMethod]
        public void TestGetInstallPath()
        {
            var installer = new InstallerLibrary(io, bucket);
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetNamePretty()).Returns("foo");

            Assert.AreEqual(
                vendorDir + "/" + packageMock.Object.GetNamePretty(),
                installer.GetInstallPath(packageMock.Object));
        }

        [TestMethod]
        public void TestEnsureBinariesInstalled()
        {
            var binaryInstallerMock = new Mock<InstallerBinary>(IONull.That, "vendor/bin", "auto", null);
            var installer = new InstallerLibrary(io, bucket, installerBinary: binaryInstallerMock.Object);
            var packageMock = new Mock<IPackage>();

            installer.EnsureBinariesPresence(packageMock.Object);

            binaryInstallerMock.Verify((o) => o.Remove(It.IsAny<IPackage>()), Times.Never);
            binaryInstallerMock.Verify((o) =>
                o.Install(packageMock.Object, installer.GetInstallPath(packageMock.Object), false));
        }

        [TestMethod]
        public void TestIsSupports()
        {
            var installer = new InstallerLibrary(io, bucket);
            Assert.IsTrue(installer.IsSupports("library"));
            Assert.IsFalse(installer.IsSupports(null));
            Assert.IsFalse(installer.IsSupports("foo"));
        }
    }
}

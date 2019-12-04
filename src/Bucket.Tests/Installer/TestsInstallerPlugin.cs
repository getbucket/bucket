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
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.Installer;
using Bucket.IO;
using Bucket.Package;
using Bucket.Plugin;
using Bucket.Repository;
using Bucket.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.Installer
{
    [TestClass]
    public class TestsInstallerPlugin
    {
        private Bucket bucket;
        private TesterIOConsole tester;
        private IIO io;
        private InstallerPlugin installer;
        private Mock<InstallationManager> installationManager;
        private Mock<InstallerBinary> installerBinary;
        private Mock<IFileSystem> fileSystem;
        private Mock<PluginManager> pluginManager;
        private Mock<DownloadManager> downloadManager;

        [TestInitialize]
        public void Initialize()
        {
            bucket = new Bucket();
            tester = new TesterIOConsole();
            io = tester.Mock();
            installationManager = new Mock<InstallationManager>();
            pluginManager = new Mock<PluginManager>(io, bucket, null, false);
            downloadManager = new Mock<DownloadManager>(null, false);
            bucket.SetInstallationManager(installationManager.Object);
            bucket.SetPluginManager(pluginManager.Object);
            bucket.SetDownloadManager(downloadManager.Object);
            bucket.SetConfig(new Config());
            fileSystem = new Mock<IFileSystem>();
            installerBinary = new Mock<InstallerBinary>(io, string.Empty, "auto", fileSystem.Object);
            installer = new InstallerPlugin(io, bucket, fileSystem.Object, installerBinary.Object);
        }

        [TestMethod]
        [DataRow(true, PluginManager.PluginType)]
        [DataRow(false, "")]
        [DataRow(false, null)]
        [DataRow(false, "library")]
        public void TestIsSupport(bool expected, string input)
        {
            Assert.AreEqual(expected, installer.IsSupports(input));
        }

        [TestMethod]
        public void TestInstall()
        {
            var repositoryInstalled = new Mock<IRepositoryInstalled>();
            var package = new Mock<IPackage>();

            installer.Install(repositoryInstalled.Object, package.Object);

            repositoryInstalled.Verify((o) => o.AddPackage(It.IsAny<IPackage>()), Times.Once);
            pluginManager.Verify((o) => o.ActivatePackages(package.Object, true), Times.Once);
        }

        [TestMethod]
        public void TestInstallAndRollback()
        {
            var repositoryInstalled = new Mock<IRepositoryInstalled>();
            var package = new Mock<IPackage>();

            package.Setup((o) => o.GetName()).Returns("foo");
            repositoryInstalled.Setup((o) => o.HasPackage(package.Object)).Returns(true).Verifiable();

            pluginManager.Setup((o) => o.ActivatePackages(package.Object, true)).Throws<RuntimeException>().Verifiable();

            Assert.ThrowsException<RuntimeException>(() =>
            {
                installer.Install(repositoryInstalled.Object, package.Object);
            });

            StringAssert.Contains(tester.GetDisplay(), "Plugin installation failed, rolling back.");
            repositoryInstalled.Verify();
            pluginManager.Verify();
        }

        [TestMethod]
        public void TestUpdate()
        {
            var repositoryInstalled = new Mock<IRepositoryInstalled>();
            var initial = new Mock<IPackage>();
            var target = new Mock<IPackage>();

            repositoryInstalled.Setup((o) => o.HasPackage(initial.Object)).Returns(true).Verifiable();
            installer.Update(repositoryInstalled.Object, initial.Object, target.Object);
            repositoryInstalled.Verify();
            pluginManager.Verify((o) => o.ActivatePackages(target.Object, true), Times.Once);
        }
    }
}

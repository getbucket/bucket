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

using Bucket.Downloader;
using Bucket.Exception;
using Bucket.Package;
using Bucket.Tester;
using Bucket.Util;
using GameBox.Console.Exception;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bucket.Tests.Downloader
{
    [TestClass]
    public class TestsDownloadManager
    {
        private TesterIOConsole tester;
        private DownloadManager manager;

        [TestInitialize]
        public void Initialize()
        {
            tester = new TesterIOConsole();
            manager = new DownloadManager(tester.Mock());
        }

        [TestMethod]
        public void TestGetSetDownloader()
        {
            var downloaderMock = new Mock<IDownloader>();

            manager.SetDownloader("foo", downloaderMock.Object);
            Assert.AreSame(downloaderMock.Object, manager.GetDownloader("foo"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestGetNotFoundDownloader()
        {
            manager.GetDownloader("not-found");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestGetDownloaderForIncorrectlyInstalledPackage()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns((InstallationSource)999);

            manager.GetDownloaderForPackage(packageMock.Object);
        }

        [TestMethod]
        public void TestGetDownloaderForCorrectlyInstalledDistPackage()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("foo", downloaderMock.Object);

            Assert.AreSame(downloaderMock.Object, manager.GetDownloaderForPackage(packageMock.Object));
        }

        [TestMethod]
        [ExpectedException(typeof(RuntimeException))]
        public void TestGetDownloaderForIncorrectlyInstalledDistPackage()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Source);

            manager.SetDownloader("foo", downloaderMock.Object);
            manager.GetDownloaderForPackage(packageMock.Object);
        }

        [TestMethod]
        public void TestDownload()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");
            packageMock.Setup((o) => o.GetSourceType()).Returns("bar");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("foo", downloaderMock.Object);

            manager.Download(packageMock.Object, "cwd");

            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Dist), Times.Once);
            downloaderMock.Verify((o) => o.Download(packageMock.Object, "cwd"), Times.Once);
        }

        [TestMethod]
        public async Task TestDownloadFailover()
        {
            InstallationSource? installation = (InstallationSource)999;
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetNamePretty()).Returns("pretty package");
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");
            packageMock.Setup((o) => o.GetSourceType()).Returns("bar");
            packageMock.Setup((o) => o.SetInstallationSource(It.IsAny<InstallationSource?>()))
                .Callback<InstallationSource?>((newInstallation) =>
            {
                installation = newInstallation;
            });
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(() => installation);

            var downloaderFailMock = new Mock<IDownloader>();
            downloaderFailMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);
            downloaderFailMock.Setup((o) => o.Download(packageMock.Object, "cwd")).Returns(() =>
            {
                return Task.Run(() =>
                {
                    Thread.Sleep(500);
                    throw new RuntimeException("foo");
                });
            });

            var downloaderSuccessMock = new Mock<IDownloader>();
            downloaderSuccessMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Source);

            manager.SetDownloader("foo", downloaderFailMock.Object);
            manager.SetDownloader("bar", downloaderSuccessMock.Object);

            await manager.Download(packageMock.Object, "cwd").ConfigureAwait(true);

            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Dist), Times.Once);
            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Source), Times.Once);
            downloaderSuccessMock.Verify((o) => o.Download(packageMock.Object, "cwd"), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestBadPackageDownload()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetDistType()).Returns(() => null);
            packageMock.Setup((o) => o.GetSourceType()).Returns(() => null);

            manager.Download(packageMock.Object, "cwd");
        }

        [TestMethod]
        public void TestDistOnlyPackageDownload()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");
            packageMock.Setup((o) => o.GetSourceType()).Returns(() => null);

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("foo", downloaderMock.Object);

            manager.Download(packageMock.Object, "cwd");

            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Dist), Times.Once);
            downloaderMock.Verify((o) => o.Download(packageMock.Object, "cwd"), Times.Once);
        }

        [TestMethod]
        public void TestSourceOnlyPackageDownload()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Source);
            packageMock.Setup((o) => o.GetDistType()).Returns(() => null);
            packageMock.Setup((o) => o.GetSourceType()).Returns("foo");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Source);

            manager.SetDownloader("foo", downloaderMock.Object);

            manager.Download(packageMock.Object, "cwd");

            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Dist), Times.Never);
            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Source), Times.Once);
            downloaderMock.Verify((o) => o.Download(packageMock.Object, "cwd"), Times.Once);
        }

        [TestMethod]
        public void TestDownloadWithSourcePreferred()
        {
            manager.SetPreferSource();

            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Source);
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");
            packageMock.Setup((o) => o.GetSourceType()).Returns("bar");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Source);

            manager.SetDownloader("bar", downloaderMock.Object);

            manager.Download(packageMock.Object, "cwd");

            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Dist), Times.Never);
            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Source), Times.Once);
            downloaderMock.Verify((o) => o.Download(packageMock.Object, "cwd"), Times.Once);
        }

        [TestMethod]
        public void TestDownloadWithDistPrefereed()
        {
            manager.SetPreferDist();

            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");
            packageMock.Setup((o) => o.GetSourceType()).Returns("bar");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("foo", downloaderMock.Object);

            manager.Download(packageMock.Object, "cwd");

            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Dist), Times.Once);
            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Source), Times.Never);
            downloaderMock.Verify((o) => o.Download(packageMock.Object, "cwd"), Times.Once);
        }

        [TestMethod]
        public void TestDistOnlyPackageDownloadWithSourcePreferred()
        {
            manager.SetPreferSource();

            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");
            packageMock.Setup((o) => o.GetSourceType()).Returns(() => null);

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("foo", downloaderMock.Object);

            manager.Download(packageMock.Object, "cwd");

            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Dist), Times.Once);
            packageMock.Verify((o) => o.SetInstallationSource(InstallationSource.Source), Times.Never);
            downloaderMock.Verify((o) => o.Download(packageMock.Object, "cwd"), Times.Once);
        }

        [TestMethod]
        public void TestUpdateDistWithEqualTypes()
        {
            var packageInitialMock = new Mock<IPackage>();
            packageInitialMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageInitialMock.Setup((o) => o.GetDistType()).Returns("foo");

            var packageTargetMock = new Mock<IPackage>();
            packageTargetMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageTargetMock.Setup((o) => o.GetDistType()).Returns("foo");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("foo", downloaderMock.Object);
            manager.Update(packageInitialMock.Object, packageTargetMock.Object, "cwd");

            downloaderMock.Verify((o) => o.Update(packageInitialMock.Object, packageTargetMock.Object, "cwd"));
            downloaderMock.VerifyAll();
        }

        [TestMethod]
        public void TestUpdateDistWithNotEqualTypes()
        {
            var packageInitialMock = new Mock<IPackage>();
            packageInitialMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageInitialMock.Setup((o) => o.GetDistType()).Returns("foo");

            var packageTargetMock = new Mock<IPackage>();
            packageTargetMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageTargetMock.Setup((o) => o.GetDistType()).Returns("bar");

            var downloaderFooMock = new Mock<IDownloader>();
            downloaderFooMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            var downloaderBarMock = new Mock<IDownloader>();
            downloaderBarMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("foo", downloaderFooMock.Object);
            manager.SetDownloader("bar", downloaderBarMock.Object);

            manager.Update(packageInitialMock.Object, packageTargetMock.Object, "cwd");

            downloaderBarMock.Verify((o) => o.Update(packageInitialMock.Object, packageTargetMock.Object, "cwd"), Times.Never);
            downloaderFooMock.Verify((o) => o.Remove(packageInitialMock.Object, "cwd"));
            downloaderBarMock.Verify((o) => o.Install(packageTargetMock.Object, "cwd"));
        }

        [TestMethod]
        public void TestUpdateThrowExceptionThenInteractive()
        {
            tester.SetInputs(new[] { "y" });
            manager = new DownloadManager(tester.Mock());

            var packageInitialMock = new Mock<IPackage>();
            packageInitialMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageInitialMock.Setup((o) => o.GetDistType()).Returns("foo");

            var packageTargetMock = new Mock<IPackage>();
            packageTargetMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageTargetMock.Setup((o) => o.GetDistType()).Returns("foo");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);
            downloaderMock.Setup((o) => o.Update(packageInitialMock.Object, packageTargetMock.Object, "cwd"))
                .Throws(new RuntimeException("want interactive"));

            manager.SetDownloader("foo", downloaderMock.Object);

            manager.Update(packageInitialMock.Object, packageTargetMock.Object, "cwd");

            downloaderMock.Verify((o) => o.Remove(packageInitialMock.Object, "cwd"));
            downloaderMock.Verify((o) => o.Install(packageTargetMock.Object, "cwd"));
        }

        [TestMethod]
        [ExpectedException(typeof(RuntimeException))]
        public void TestUpdateThrowExceptionThenInteractiveRefuse()
        {
            tester.SetInputs(new[] { "n" });
            manager = new DownloadManager(tester.Mock());

            var packageInitialMock = new Mock<IPackage>();
            packageInitialMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageInitialMock.Setup((o) => o.GetDistType()).Returns("foo");

            var packageTargetMock = new Mock<IPackage>();
            packageTargetMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageTargetMock.Setup((o) => o.GetDistType()).Returns("foo");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);
            downloaderMock.Setup((o) => o.Update(packageInitialMock.Object, packageTargetMock.Object, "cwd"))
                .Throws(new RuntimeException("want interactive"));

            manager.SetDownloader("foo", downloaderMock.Object);

            manager.Update(packageInitialMock.Object, packageTargetMock.Object, "cwd");
        }

        [TestMethod]
        [DataRow(InstallationSource.Source, false, new[] { "source", "dist" }, false, new[] { "source", "dist" })]
        [DataRow(InstallationSource.Dist, false, new[] { "source", "dist" }, false, new[] { "dist", "source" })]
        [DataRow(InstallationSource.Source, false, new[] { "dist" }, false, new[] { "dist" })]
        [DataRow(InstallationSource.Dist, false, new[] { "source" }, false, new[] { "source" })]
        [DataRow(InstallationSource.Source, false, new[] { "source", "dist" }, true, new[] { "source", "dist" })]
        [DataRow(InstallationSource.Dist, false, new[] { "source", "dist" }, true, new[] { "source", "dist" })]
        [DataRow(null, null, new[] { "source", "dist" }, true, new[] { "source", "dist" })]
        [DataRow(null, null, new[] { "dist" }, true, new[] { "dist" })]
        [DataRow(null, null, new[] { "source" }, true, new[] { "source" })]
        [DataRow(null, null, new[] { "source", "dist" }, false, new[] { "dist", "source" })]
        [DataRow(null, null, new[] { "dist" }, false, new[] { "dist" })]
        [DataRow(null, null, new[] { "source" }, false, new[] { "source" })]
        public void TestGetAvailableSourcesUpdateSticksToSameSource(
            InstallationSource? prevPackageSource,
            bool? prevPackageIsDev,
            string[] targetAvailable,
            bool targetIsDev,
            string[] expected)
        {
            Mock<IPackage> packageInitialMock = null;
            if (prevPackageSource != null)
            {
                packageInitialMock = new Mock<IPackage>();
                packageInitialMock.Setup((o) => o.GetInstallationSource())
                    .Returns(prevPackageSource.Value);
                packageInitialMock.Setup((o) => o.IsDev)
                    .Returns(prevPackageIsDev.Value);
            }

            var packageTargetMock = new Mock<IPackage>();
            packageTargetMock.Setup((o) => o.IsDev)
                    .Returns(targetIsDev);
            packageTargetMock.Setup((o) => o.GetDistType())
                    .Returns(() => Array.Exists(targetAvailable, (available) => available == "dist") ? "zip" : null);
            packageTargetMock.Setup((o) => o.GetSourceType())
                    .Returns(() => Array.Exists(targetAvailable, (available) => available == "source") ? "git" : null);

            var downloaderSourceMock = new Mock<IDownloader>();
            downloaderSourceMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Source);

            var downloaderDistMock = new Mock<IDownloader>();
            downloaderDistMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("git", downloaderSourceMock.Object);
            manager.SetDownloader("zip", downloaderDistMock.Object);

            var method = typeof(DownloadManager).GetMethod(
                "GetAvailableSources",
                BindingFlags.NonPublic | BindingFlags.Instance);

            CollectionAssert.AreEqual(
                Arr.Map(expected, (item) =>
                {
                    if (item == "source")
                    {
                        return InstallationSource.Source;
                    }

                    if (item == "dist")
                    {
                        return InstallationSource.Dist;
                    }

                    throw new UnexpectedException($"Should not be wrong. Invalid expected: \"{item}\"");
                }),
                (InstallationSource[])method.Invoke(
                    manager,
                    new object[] { packageTargetMock.Object, packageInitialMock?.Object }));
        }

        [TestMethod]
        [DataRow("foo*", InstallationSource.Dist, "foobar", false, InstallationSource.Dist)]
        [DataRow("foo*", InstallationSource.Auto, "foobar", true, InstallationSource.Source)]
        [DataRow("foo*", InstallationSource.Auto, "foobar", false, InstallationSource.Dist)]
        [DataRow("foo*", InstallationSource.Source, "foobar", false, InstallationSource.Source)]
        [DataRow("foo*", InstallationSource.Source, "foobar", true, InstallationSource.Source)]
        [DataRow("foobar*", InstallationSource.Dist, "foo", true, InstallationSource.Source)]
        [DataRow("foobar*", InstallationSource.Dist, "foo", false, InstallationSource.Dist)]
        public void TestResolvePackageInstallPreference(
            string pattern,
            InstallationSource preference,
            string packageName,
            bool packageIsDev,
            InstallationSource expected)
        {
            manager.SetPreferences(new[] { (pattern, preference) });

            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetName()).Returns(packageName);
            packageMock.Setup((o) => o.IsDev).Returns(packageIsDev);

            var method = typeof(DownloadManager).GetMethod(
                "ResolvePackageInstallPreference",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.AreEqual(expected, method.Invoke(manager, new object[] { packageMock.Object }));
        }

        [TestMethod]
        public void TestRemove()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetInstallationSource()).Returns(InstallationSource.Dist);
            packageMock.Setup((o) => o.GetDistType()).Returns("foo");

            var downloaderMock = new Mock<IDownloader>();
            downloaderMock.Setup((o) => o.InstallationSource).Returns(InstallationSource.Dist);

            manager.SetDownloader("foo", downloaderMock.Object);
            manager.Remove(packageMock.Object, "cwd");

            downloaderMock.Verify((o) => o.Remove(packageMock.Object, "cwd"));
        }
    }
}

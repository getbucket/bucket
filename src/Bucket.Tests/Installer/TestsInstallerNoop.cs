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

using Bucket.Installer;
using Bucket.Package;
using Bucket.Repository;
using GameBox.Console.Exception;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace Bucket.Tests.Installer
{
    [TestClass]
    public class TestsInstallerNoop
    {
        private InstallerNoop installer;
        private Mock<IRepositoryInstalled> repository;

        [TestInitialize]
        public void Initialize()
        {
            installer = new InstallerNoop();
            repository = new Mock<IRepositoryInstalled>();
        }

        [TestMethod]
        public async Task TestDownload()
        {
            var packageMock = new Mock<IPackage>();
            await installer.Download(packageMock.Object, packageMock.Object).ConfigureAwait(false);
        }

        [TestMethod]
        public void TestGetInstallPath()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetNamePretty()).Returns("foo/bar");
            Assert.AreEqual("foo/bar", installer.GetInstallPath(packageMock.Object));
        }

        [TestMethod]
        public void TestInstall()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.Clone()).Returns(packageMock.Object);
            repository.SetupSequence((o) => o.HasPackage(packageMock.Object))
                .Returns(false).Returns(true);

            installer.Install(repository.Object, packageMock.Object);
            installer.Install(repository.Object, packageMock.Object);

            repository.Verify((o) => o.AddPackage(packageMock.Object), Times.Once);
        }

        [TestMethod]
        public void TestIsInstalled()
        {
            var packageMock = new Mock<IPackage>();
            repository.SetupSequence((o) => o.HasPackage(packageMock.Object))
                .Returns(false).Returns(true);

            Assert.IsFalse(installer.IsInstalled(repository.Object, packageMock.Object));
            Assert.IsTrue(installer.IsInstalled(repository.Object, packageMock.Object));
        }

        [TestMethod]
        public void TestIsSupports()
        {
            Assert.IsTrue(installer.IsSupports("foo"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestUninstall()
        {
            var packageMock = new Mock<IPackage>();
            repository.SetupSequence((o) => o.HasPackage(packageMock.Object))
                .Returns(true).Returns(false);

            installer.Uninstall(repository.Object, packageMock.Object);
            repository.Verify((o) => o.RemovePackage(packageMock.Object), Times.Once);

            installer.Uninstall(repository.Object, packageMock.Object);
        }
    }
}

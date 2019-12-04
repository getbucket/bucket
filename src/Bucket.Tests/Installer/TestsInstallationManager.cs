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

using Bucket.DependencyResolver.Operation;
using Bucket.Installer;
using Bucket.Package;
using Bucket.Repository;
using GameBox.Console.Exception;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Bucket.Tests.Installer
{
    [TestClass]
    public class TestsInstallationManager
    {
        private Mock<IRepositoryInstalled> repository;
        private Mock<IInstaller> installer;
        private Mock<IPackage> package;
        private InstallationManager manager;

        [TestInitialize]
        public void Initialize()
        {
            repository = new Mock<IRepositoryInstalled>();
            installer = new Mock<IInstaller>();
            package = new Mock<IPackage>();
            package.Setup((o) => o.Clone()).Returns(package.Object);
            manager = new InstallationManager();
        }

        [TestMethod]
        public void TestAddGetInstaller()
        {
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            manager.AddInstaller(installer.Object);

            Assert.AreSame(installer.Object, manager.GetInstaller("foo"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestAddGetInstallerNotResister()
        {
            manager.GetInstaller("foo");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestAllInstallerNotFound()
        {
            installer.Setup((o) => o.IsSupports("foo")).Returns(false);
            manager.AddInstaller(installer.Object);
            manager.GetInstaller("bar");
        }

        [TestMethod]
        public void TestAddRemoveInstaller()
        {
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            manager.AddInstaller(installer.Object);

            var installerBar = new Mock<IInstaller>();
            installerBar.Setup((o) => o.IsSupports("bar")).Returns(true);
            manager.AddInstaller(installerBar.Object);

            var installerFoo = new Mock<IInstaller>();
            installerFoo.Setup((o) => o.IsSupports("foo")).Returns(true);
            manager.AddInstaller(installerFoo.Object);

            Assert.AreSame(installerFoo.Object, manager.GetInstaller("foo"));
            Assert.AreSame(installerBar.Object, manager.GetInstaller("bar"));

            manager.RemoveInstaller(installer.Object);

            Assert.AreSame(installerBar.Object, manager.GetInstaller("bar"));
            Assert.AreSame(installerFoo.Object, manager.GetInstaller("foo"));

            manager.RemoveInstaller(installerFoo.Object);

            Assert.ThrowsException<InvalidArgumentException>(() =>
            {
                manager.GetInstaller("foo");
            });
        }

        [TestMethod]
        public void TestExecute()
        {
            var managerMock = new Mock<InstallationManager>
            {
                CallBase = true,
            };

            manager = managerMock.Object;

            var operationInstall = new OperationInstall(package.Object);
            var operationUninstall = new OperationUninstall(package.Object);
            var operationUpdate = new OperationUpdate(package.Object, package.Object);

            package.Setup((o) => o.GetPackageType()).Returns("foo");
            repository.SetupSequence((o) => o.HasPackage(package.Object))
                .Returns(false).Returns(true).Returns(true).Returns(false);
            manager.AddInstaller(new InstallerNoop());

            manager.Execute(repository.Object, operationInstall);
            manager.Execute(repository.Object, operationUninstall);
            manager.Execute(repository.Object, operationUpdate);

            managerMock.Protected().Verify("Install", Times.Once(), repository.Object, operationInstall);
            managerMock.Protected().Verify("Uninstall", Times.Once(), repository.Object, operationUninstall);
            managerMock.Protected().Verify("Update", Times.Once(), repository.Object, operationUpdate);
        }

        [TestMethod]
        public void TestInstall()
        {
            var operationInstall = new OperationInstall(package.Object);
            package.Setup((o) => o.GetPackageType()).Returns("foo");
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            manager.AddInstaller(installer.Object);

            manager.Execute(repository.Object, operationInstall);

            installer.Verify((o) => o.Install(repository.Object, package.Object));
        }

        [TestMethod]
        public void TestUpdate()
        {
            var initial = new Mock<IPackage>();
            var target = new Mock<IPackage>();
            var operationUpdate = new OperationUpdate(initial.Object, target.Object);
            initial.Setup((o) => o.GetPackageType()).Returns("foo");
            target.Setup((o) => o.GetPackageType()).Returns("foo");
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            manager.AddInstaller(installer.Object);

            manager.Execute(repository.Object, operationUpdate);

            installer.Verify((o) => o.Download(target.Object, initial.Object));
            installer.Verify((o) => o.Update(repository.Object, initial.Object, target.Object));
        }

        [TestMethod]
        public void TestUpdateWithNotEqualPackageType()
        {
            var initial = new Mock<IPackage>();
            initial.Setup((o) => o.GetPackageType()).Returns("foo");
            var target = new Mock<IPackage>();
            target.Setup((o) => o.GetPackageType()).Returns("bar");

            var installerFoo = new Mock<IInstaller>();
            installerFoo.Setup((o) => o.IsSupports("foo")).Returns(true);
            var installerBar = new Mock<IInstaller>();
            installerBar.Setup((o) => o.IsSupports("bar")).Returns(true);

            manager.AddInstaller(installerFoo.Object);
            manager.AddInstaller(installerBar.Object);

            var operationUpdate = new OperationUpdate(initial.Object, target.Object);

            manager.Execute(repository.Object, operationUpdate);

            installerFoo.Verify((o) => o.Uninstall(repository.Object, initial.Object));
            installerBar.Verify((o) => o.Install(repository.Object, target.Object));
        }

        [TestMethod]
        public void TestUninstall()
        {
            var operationUninstall = new OperationUninstall(package.Object);
            package.Setup((o) => o.GetPackageType()).Returns("foo");
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            manager.AddInstaller(installer.Object);

            manager.Execute(repository.Object, operationUninstall);

            installer.Verify((o) => o.Uninstall(repository.Object, package.Object));
        }

        [TestMethod]
        public void TestMarkAliasInstalled()
        {
            package.Setup((o) => o.GetPackageType()).Returns("foo");
            package.Setup((o) => o.GetName()).Returns("foobar");
            var packageAlias = new PackageAlias(package.Object, "1.2.0.0", "1.2");
            var operationAliasInstalled = new OperationMarkPackageAliasInstalled(packageAlias);
            repository.Setup((o) => o.HasPackage(packageAlias)).Returns(false);

            manager.Execute(repository.Object, operationAliasInstalled);
            repository.Verify((o) => o.AddPackage(It.IsAny<IPackage>()));
        }

        [TestMethod]
        public void TestMarkAliasUninstall()
        {
            package.Setup((o) => o.GetPackageType()).Returns("foo");
            package.Setup((o) => o.GetName()).Returns("foobar");
            var packageAlias = new PackageAlias(package.Object, "1.2.0.0", "1.2");
            var operationAliasUninstalled = new OperationMarkPackageAliasUninstall(packageAlias);

            manager.Execute(repository.Object, operationAliasUninstalled);
            repository.Verify((o) => o.RemovePackage(It.IsAny<IPackage>()));
        }

        [TestMethod]
        public void TestEnsureBinariesPresence()
        {
            package.Setup((o) => o.GetPackageType()).Returns("foo");
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            installer.As<IBinaryPresence>().Setup((o) => o.EnsureBinariesPresence(package.Object));
            manager.AddInstaller(installer.Object);

            manager.EnsureBinariesPresence(package.Object);

            installer.As<IBinaryPresence>().Verify((o) => o.EnsureBinariesPresence(package.Object));
        }

        [TestMethod]
        public void TestGetInstalledPath()
        {
            package.Setup((o) => o.GetPackageType()).Returns("foo");
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            installer.Setup((o) => o.GetInstallPath(package.Object)).Returns("bar");
            manager.AddInstaller(installer.Object);

            Assert.AreEqual("bar", manager.GetInstalledPath(package.Object));
        }

        [TestMethod]
        public void TestIsPackageInstalled()
        {
            package.Setup(o => o.GetPackageType()).Returns("foo");
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            installer.Setup((o) => o.IsInstalled(repository.Object, package.Object))
                .Returns(true);
            repository.Setup((o) => o.HasPackage(package.Object)).Returns(true);
            manager.AddInstaller(installer.Object);

            Assert.AreEqual(true, manager.IsPackageInstalled(repository.Object, package.Object));
        }

        [TestMethod]
        public void TestIsPackageInstalledWithPackageAlias()
        {
            var managerMock = new Mock<InstallationManager>()
            {
                CallBase = true,
            };

            manager = managerMock.Object;
            package.Setup((o) => o.GetPackageType()).Returns("foo");
            package.Setup((o) => o.GetName()).Returns("foobar");
            var packageAlias = new PackageAlias(package.Object, "1.2.0.0", "1.2");
            repository.Setup((o) => o.HasPackage(packageAlias)).Returns(true);
            repository.Setup((o) => o.HasPackage(package.Object)).Returns(true);
            installer.Setup((o) => o.IsSupports("foo")).Returns(true);
            installer.Setup((o) => o.IsInstalled(repository.Object, package.Object))
                .Returns(true);
            manager.AddInstaller(installer.Object);

            Assert.AreEqual(true, manager.IsPackageInstalled(repository.Object, packageAlias));
            managerMock.Verify((o) => o.IsPackageInstalled(repository.Object, package.Object));
        }
    }
}

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
using Bucket.IO;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Tester;
using GameBox.Console.Exception;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.Installer
{
    [TestClass]
    public class TestsInstallerMetaPackage
    {
        private Mock<IRepositoryInstalled> repository;
        private TesterIOConsole tester;
        private IIO io;
        private InstallerMetaPackage installer;
        private Mock<IPackage> package;

        [TestInitialize]
        public void Initialize()
        {
            repository = new Mock<IRepositoryInstalled>();
            tester = new TesterIOConsole();
            io = tester.Mock(AbstractTester.OptionStdErrorSeparately(true));
            installer = new InstallerMetaPackage(io);
            package = new Mock<IPackage>();
            package.Setup((o) => o.Clone()).Returns(package.Object);
        }

        [TestMethod]
        public void TestInstall()
        {
            package.Setup((o) => o.GetName()).Returns("foo");
            package.Setup((o) => o.GetVersionPrettyFull(true)).Returns("1.2");

            installer.Install(repository.Object, package.Object);
            repository.Verify((o) => o.AddPackage(package.Object));
            StringAssert.Contains(tester.GetDisplayError(), "  - Installing foo (1.2)");
        }

        [TestMethod]
        public void TestUpdate()
        {
            var initial = new Mock<IPackage>();
            var target = new Mock<IPackage>();

            initial.Setup((o) => o.GetName()).Returns("foo/old");
            initial.Setup((o) => o.GetVersion()).Returns("1.0");
            initial.Setup((o) => o.GetVersionPrettyFull(true)).Returns("1.0");

            target.Setup((o) => o.GetName()).Returns("foo/new");
            target.Setup((o) => o.GetVersion()).Returns("1.2");
            target.Setup((o) => o.GetVersionPrettyFull(true)).Returns("1.2");
            target.Setup((o) => o.Clone()).Returns(target.Object);

            repository.Setup((o) => o.HasPackage(initial.Object)).Returns(true);

            installer.Update(repository.Object, initial.Object, target.Object);

            repository.Verify((o) => o.RemovePackage(initial.Object));
            repository.Verify((o) => o.AddPackage(target.Object));
            StringAssert.Contains(tester.GetDisplayError(), "  - Updating foo/new (1.0 => 1.2)");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestUpdatePackageNotFound()
        {
            var initial = new Mock<IPackage>();
            var target = new Mock<IPackage>();

            repository.Setup((o) => o.HasPackage(initial.Object)).Returns(false);

            installer.Update(repository.Object, initial.Object, target.Object);
        }

        [TestMethod]
        public void TestUninstall()
        {
            package.Setup((o) => o.GetName()).Returns("foo");
            package.Setup((o) => o.GetVersionPrettyFull(true)).Returns("1.0");
            repository.Setup((o) => o.HasPackage(package.Object)).Returns(true);

            installer.Uninstall(repository.Object, package.Object);

            repository.Verify((o) => o.RemovePackage(package.Object));

            System.Console.WriteLine(tester.GetDisplayError());
            StringAssert.Contains(tester.GetDisplayError(), "  - Removing foo (1.0)");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestUninstallPackageNotFound()
        {
            repository.Setup((o) => o.HasPackage(package.Object)).Returns(false);
            installer.Uninstall(repository.Object, package.Object);
        }
    }
}

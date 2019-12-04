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
using Bucket.IO;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using GameBox.Console.EventDispatcher;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryManager
    {
        private RepositoryManager manager;
        private Config config;

        [TestInitialize]
        public void Initialize()
        {
            config = new Config();
            manager = new RepositoryManager(IONull.That, config);
        }

        [TestMethod]
        public void TestAddRepository()
        {
            var foo = new Mock<IRepository>().Object;
            var bar = new Mock<IRepository>().Object;

            manager.AddRepository(bar);
            manager.AddRepository(foo, true);

            CollectionAssert.AreEqual(new[] { foo, bar }, manager.GetRepositories());
        }

        [TestMethod]
        public void TestCreateRepository()
        {
            var mockRepository = new Mock<IRepository>();
            var mockCreater = new Mock<RepositoryManager.RepositoryCreater>();

            mockCreater.Setup((o) => o.Invoke(It.IsAny<ConfigRepository>(), It.IsAny<IIO>(), It.IsAny<Config>(), It.IsAny<IEventDispatcher>(), It.IsAny<IVersionParser>()))
                .Returns(() => mockRepository.Object);

            manager.RegisterRepository("git", mockCreater.Object);

            var actual = manager.CreateRepository(new ConfigRepositoryVcs()
            {
                Type = "git",
                Name = "foo",
            });

            Assert.AreEqual(mockRepository.Object, actual);
        }

        [TestMethod]
        public void TestFindPackage()
        {
            var mockPackage = new Mock<IPackage>();
            var mockRepository1 = new Mock<IRepository>();
            mockRepository1.Setup((o) => o.FindPackage(It.IsAny<string>(), It.IsAny<IConstraint>()))
                .Returns(() => null);

            var mockRepository2 = new Mock<IRepository>();
            mockRepository2.Setup((o) => o.FindPackage(It.IsAny<string>(), It.IsAny<IConstraint>()))
                .Returns(() => mockPackage.Object);

            manager.AddRepository(mockRepository1.Object);
            manager.AddRepository(mockRepository2.Object);

            var actual = manager.FindPackage("foo", "1.0.0");

            Assert.AreEqual(mockPackage.Object, actual);
            mockRepository1.Verify((o) => o.FindPackage(It.IsAny<string>(), It.IsAny<IConstraint>()), Times.Once);
            mockRepository2.Verify((o) => o.FindPackage(It.IsAny<string>(), It.IsAny<IConstraint>()), Times.Once);
        }

        [TestMethod]
        public void TestFindPackages()
        {
            var mockPackage1 = new Mock<IPackage>();
            var mockPackage2 = new Mock<IPackage>();
            var mockRepository1 = new Mock<IRepository>();
            mockRepository1.Setup((o) => o.FindPackages(It.IsAny<string>(), It.IsAny<IConstraint>()))
                .Returns(() => new[] { mockPackage1.Object });

            var mockRepository2 = new Mock<IRepository>();
            mockRepository2.Setup((o) => o.FindPackages(It.IsAny<string>(), It.IsAny<IConstraint>()))
                .Returns(() => new[] { mockPackage2.Object });

            manager.AddRepository(mockRepository1.Object);
            manager.AddRepository(mockRepository2.Object);

            var actual = manager.FindPackages("foo", "1.0.0");
            CollectionAssert.AreEqual(new[] { mockPackage1.Object, mockPackage2.Object }, actual);
        }

        [TestMethod]
        public void TestSetLocalInstalledRepository()
        {
            var repository = new Mock<IRepositoryInstalled>();
            manager.SetLocalInstalledRepository(repository.Object);

            Assert.AreEqual(repository.Object, manager.GetLocalInstalledRepository());
            CollectionAssert.AreEqual(Array.Empty<IRepository>(), manager.GetRepositories(), "Local repository should not return.");
        }
    }
}

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
using Bucket.Json;
using Bucket.Package;
using Bucket.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryFileSystem
    {
        private Mock<JsonFile> json;
        private RepositoryFileSystem repository;

        [TestInitialize]
        public void Initialize()
        {
            json = new Mock<JsonFile>(string.Empty, null, null);
            repository = new RepositoryFileSystem(json.Object);
        }

        [TestMethod]
        public void TestRepositoryInitialize()
        {
            json.Setup((o) => o.Read<ConfigInstalled>()).Returns(new ConfigInstalled
            {
                Packages = new[]
                {
                    new ConfigInstalledPackage
                    {
                        Name = "foo",
                        Version = "1.2.0-beta",
                        PackageType = "library",
                    },
                },
            });

            json.Setup((o) => o.Exists()).Returns(true);

            var packages = repository.GetPackages();

            Assert.AreEqual(1, packages.Length);
            Assert.AreEqual("foo", packages[0].GetName());
            Assert.AreEqual("1.2.0.0-beta", packages[0].GetVersion());
            Assert.AreEqual("library", packages[0].GetPackageType());
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidRepositoryException), "Invalid repository data in")]
        public void TestCorruptedRepositoryFile()
        {
            json.Setup((o) => o.Read<ConfigInstalled>()).Returns(new ConfigInstalled
            {
                Packages = null,
            });

            json.Setup((o) => o.Exists()).Returns(true);
            repository.GetPackages();
        }

        [TestMethod]
        public void TestNotExistRepositoryFile()
        {
            json.Setup((o) => o.Exists()).Returns(false);
            var packages = repository.GetPackages();
            Assert.AreEqual(0, packages.Length);
        }

        [TestMethod]
        public void TestRepositoryWrite()
        {
            var package = new Mock<IPackage>();
            json.Setup((o) => o.Exists()).Returns(false);
            package.Setup((o) => o.GetName()).Returns("foo");
            package.Setup((o) => o.GetNamePretty()).Returns("foo");
            package.Setup((o) => o.GetVersionPretty()).Returns("1.2.0");
            package.Setup((o) => o.GetVersion()).Returns("1.2.0.0");
            package.Setup((o) => o.GetPackageType()).Returns("library");
            repository.AddPackage(package.Object);

            json.Setup((o) => o.Write(It.IsAny<object>())).Callback((object content) =>
            {
                var data = (ConfigInstalled)content;
                Assert.AreEqual(1, data.Packages.Length);
                Assert.AreEqual("foo", data.Packages[0].Name);
                Assert.AreEqual("1.2.0", data.Packages[0].Version);
                Assert.AreEqual("1.2.0.0", data.Packages[0].VersionNormalized);
                Assert.AreEqual("library", data.Packages[0].PackageType);
            }).Verifiable();

            repository.Write();
            Moq.Mock.Verify(json);
        }

        [TestMethod]
        public void TestReload()
        {
            var foo = new Mock<IPackage>();
            var bar = new Mock<IPackage>();

            repository.AddPackage(foo.Object);
            repository.AddPackage(bar.Object);

            json.Setup((o) => o.Read<ConfigInstalled>()).Returns(new ConfigInstalled
            {
                Packages = new[]
                {
                    new ConfigInstalledPackage
                    {
                        Name = "foo",
                        Version = "1.2.0-beta",
                        PackageType = "library",
                    },
                },
            });
            json.Setup((o) => o.Exists()).Returns(true);

            Assert.AreEqual(2, repository.GetPackages().Length);

            repository.Reload();

            var packages = repository.GetPackages();
            Assert.AreEqual(1, packages.Length);
            Assert.AreEqual("foo", packages[0].GetName());
            Assert.AreEqual("1.2.0.0-beta", packages[0].GetVersion());
            Assert.AreEqual("library", packages[0].GetPackageType());
        }
    }
}

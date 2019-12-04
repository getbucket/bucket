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

using Bucket.Package;
using Bucket.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryArrayWriteable
    {
        private IRepositoryWriteable repository;

        [TestInitialize]
        public void Initialize()
        {
            repository = new RepositoryArrayWriteable();
        }

        [TestMethod]
        public void TestGetCanonicalPackages()
        {
            var foo1 = new Mock<IPackage>();
            foo1.Setup((o) => o.GetName()).Returns("foo");
            foo1.Setup((o) => o.GetVersion()).Returns("1.0.0");

            var foo2 = new Mock<IPackage>();
            foo2.Setup((o) => o.GetName()).Returns("foo");
            foo2.Setup((o) => o.GetVersion()).Returns("2.0.0");

            var bar = new Mock<IPackage>();
            bar.Setup((o) => o.GetName()).Returns("bar");

            var baz = new Mock<IPackage>();
            baz.Setup((o) => o.GetName()).Returns("baz");

            var aliasPackage = Helper.MockPackageAlias(baz.Object, "2.0.0");

            repository.AddPackage(foo1.Object);
            repository.AddPackage(foo2.Object);
            repository.AddPackage(bar.Object);
            repository.AddPackage(baz.Object);
            repository.AddPackage(aliasPackage);

            var actual = repository.GetCanonicalPackages();

            CollectionAssert.Contains(actual, foo1.Object);
            CollectionAssert.Contains(actual, bar.Object);
            CollectionAssert.Contains(actual, baz.Object);
            Assert.AreEqual(3, actual.Length);
        }
    }
}

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

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryArray
    {
        [TestMethod]
        public void TestAddPackage()
        {
            var repository = new RepositoryArray();
            repository.AddPackage(Helper.MockPackage("foo", "1"));
            Assert.AreEqual(1, repository.Count);
        }

        [TestMethod]
        public void TestRemovePackage()
        {
            var packageFoo = Helper.MockPackage("foo", "1");
            var packageBar = Helper.MockPackage("bar", "2");
            var repository = new RepositoryArray();

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            Assert.AreEqual(2, repository.Count);

            repository.RemovePackage(packageFoo);

            Assert.AreEqual(1, repository.Count);
            CollectionAssert.AreEqual(new[] { packageBar }, repository.GetPackages());
        }

        [TestMethod]
        public void TestHasPackage()
        {
            var packageFoo = Helper.MockPackage("foo", "1");
            var packageBar = Helper.MockPackage("bar", "2");
            var repository = new RepositoryArray();

            repository.AddPackage(packageFoo);
            repository.AddPackage(Helper.MockPackage("bar", "3"));

            Assert.IsTrue(repository.HasPackage(packageFoo));
            Assert.IsFalse(repository.HasPackage(packageBar));
            Assert.IsTrue(repository.HasPackage(Helper.MockPackage("bar", "3")));
        }

        [TestMethod]
        public void TestFindPackages()
        {
            var repository = new RepositoryArray();

            repository.AddPackage(Helper.MockPackage("foo", "1"));
            repository.AddPackage(Helper.MockPackage("bar", "2"));
            repository.AddPackage(Helper.MockPackage("bar", "3"));

            var foo = repository.FindPackages("foo");

            Assert.AreEqual(1, foo.Length);
            Assert.AreEqual("foo", foo[0].GetName());

            var bar = repository.FindPackages("bar");

            Assert.AreEqual(2, bar.Length);
            Assert.AreEqual("bar", bar[0].GetName());
            Assert.AreEqual("2", bar[0].GetVersionPretty());
            Assert.AreEqual("bar", bar[1].GetName());
            Assert.AreEqual("3", bar[1].GetVersionPretty());
        }

        [TestMethod]
        public void TestAutomaticallyAddAliasedPackageButNotRemove()
        {
            var repository = new RepositoryArray();

            var package = Helper.MockPackage("foo", "1");
            var alias = Helper.MockPackageAlias(package, "2");

            repository.AddPackage(alias);

            Assert.AreEqual(2, repository.Count);

            Assert.IsTrue(repository.HasPackage(Helper.MockPackage("foo", "1")));
            Assert.IsTrue(repository.HasPackage(Helper.MockPackage("foo", "2")));

            repository.RemovePackage(alias);

            Assert.AreEqual(1, repository.Count);

            Assert.IsTrue(repository.HasPackage(Helper.MockPackage("foo", "1")));
            Assert.IsFalse(repository.HasPackage(Helper.MockPackage("foo", "2")));
        }

        [TestMethod]
        public void TestSearch()
        {
            var repository = new RepositoryArray();
            repository.AddPackage(Helper.MockPackage("foo", "1"));
            repository.AddPackage(Helper.MockPackage("bar", "2"));

            var searched = repository.Search("foo", SearchMode.Fulltext);

            Assert.AreEqual(1, searched.Length);
            Assert.AreEqual("foo", searched[0].GetName());
            Assert.AreEqual(null, searched[0].GetDescription());

            searched = repository.Search("bar");

            Assert.AreEqual(1, searched.Length);
            Assert.AreEqual("bar", searched[0].GetName());
            Assert.AreEqual(null, searched[0].GetDescription());

            searched = repository.Search("foobar");
            Assert.AreEqual(0, searched.Length);
        }

        [TestMethod]
        public void TestSearchWithPackageType()
        {
            var repository = new RepositoryArray();
            repository.AddPackage(Helper.MockPackage("foo", "1", type: PackageType.Library));
            repository.AddPackage(Helper.MockPackage("bar", "2", type: PackageType.Library));
            repository.AddPackage(Helper.MockPackage("foobar", "3", type: PackageType.Plugin));

            var searched = repository.Search("foo", SearchMode.Fulltext, PackageType.Library);

            Assert.AreEqual(1, searched.Length);
            Assert.AreEqual("foo", searched[0].GetName());
            Assert.AreEqual(null, searched[0].GetDescription());

            searched = repository.Search("bar", SearchMode.Fulltext, PackageType.Plugin);

            Assert.AreEqual(1, searched.Length);
            Assert.AreEqual("foobar", searched[0].GetName());
            Assert.AreEqual(null, searched[0].GetDescription());
        }
    }
}

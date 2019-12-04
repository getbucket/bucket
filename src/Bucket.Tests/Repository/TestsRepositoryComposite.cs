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
using System;

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryComposite
    {
        private RepositoryArray repositoryOne;
        private RepositoryArray repositoryTwo;
        private RepositoryComposite repositoryComposite;

        [TestInitialize]
        public void Initialize()
        {
            repositoryOne = new RepositoryArray();
            repositoryTwo = new RepositoryArray();
            repositoryComposite = new RepositoryComposite(repositoryOne, repositoryTwo);
        }

        [TestMethod]
        public void TestHasPackage()
        {
            repositoryOne.AddPackage(Helper.MockPackage("foo", "1.0"));
            repositoryTwo.AddPackage(Helper.MockPackage("bar", "1.0"));

            Assert.IsTrue(repositoryComposite.HasPackage(Helper.MockPackage("foo", "1.0")));
            Assert.IsTrue(repositoryComposite.HasPackage(Helper.MockPackage("bar", "1.0")));

            Assert.IsFalse(repositoryComposite.HasPackage(Helper.MockPackage("foo", "2.0")));
            Assert.IsFalse(repositoryComposite.HasPackage(Helper.MockPackage("bar", "2.0")));
        }

        [TestMethod]
        public void TestFindPackage()
        {
            repositoryOne.AddPackage(Helper.MockPackage("foo", "1.0"));
            repositoryTwo.AddPackage(Helper.MockPackage("bar", "1.0"));

            Assert.AreEqual("foo", repositoryComposite.FindPackage("foo", "1.0").GetName());
            Assert.AreEqual("1.0", repositoryComposite.FindPackage("foo", "1.0").GetVersionPretty());
            Assert.AreEqual("bar", repositoryComposite.FindPackage("bar", "1.0").GetName());
            Assert.AreEqual("1.0", repositoryComposite.FindPackage("bar", "1.0").GetVersionPretty());

            Assert.AreEqual(null, repositoryComposite.FindPackage("foo", "2.0"));
        }

        [TestMethod]
        public void TestFindPackages()
        {
            repositoryOne.AddPackage(Helper.MockPackage("foo", "1.0"));
            repositoryOne.AddPackage(Helper.MockPackage("foo", "2.0"));
            repositoryOne.AddPackage(Helper.MockPackage("baz", "1.0"));

            repositoryTwo.AddPackage(Helper.MockPackage("bar", "1.0"));
            repositoryTwo.AddPackage(Helper.MockPackage("bar", "2.0"));
            repositoryTwo.AddPackage(Helper.MockPackage("foo", "3.0"));

            var bazs = repositoryComposite.FindPackages("baz");
            Assert.AreEqual(1, bazs.Length);
            Assert.AreEqual("baz", bazs[0].GetName());

            var bars = repositoryComposite.FindPackages("bar");
            Assert.AreEqual(2, bars.Length);
            Assert.AreEqual("bar", bars[0].GetName());

            var foos = repositoryComposite.FindPackages("foo");
            Assert.AreEqual(3, foos.Length);
            Assert.AreEqual("foo", foos[0].GetName());
        }

        [TestMethod]
        public void TestGetPackages()
        {
            repositoryOne.AddPackage(Helper.MockPackage("foo", "1.0"));
            repositoryTwo.AddPackage(Helper.MockPackage("bar", "1.0"));

            var packages = repositoryComposite.GetPackages();

            Assert.AreEqual(2, packages.Length);
            Assert.AreEqual("foo", packages[0].GetName());
            Assert.AreEqual("1.0", packages[0].GetVersionPretty());
            Assert.AreEqual("bar", packages[1].GetName());
            Assert.AreEqual("1.0", packages[1].GetVersionPretty());
        }

        [TestMethod]
        public void TestAddRepository()
        {
            repositoryOne.AddPackage(Helper.MockPackage("foo", "1.0"));
            Assert.AreEqual(1, repositoryComposite.Count);

            var repositoryOther = new RepositoryArray();
            repositoryOther.AddPackage(Helper.MockPackage("bar", "1.0"));
            repositoryOther.AddPackage(Helper.MockPackage("bar", "2.0"));
            repositoryOther.AddPackage(Helper.MockPackage("bar", "3.0"));
            repositoryComposite.AddRepository(repositoryOther);
            Assert.AreEqual(4, repositoryComposite.Count);
        }

        [TestMethod]
        public void TestCount()
        {
            repositoryOne.AddPackage(Helper.MockPackage("foo", "1.0"));
            repositoryTwo.AddPackage(Helper.MockPackage("bar", "1.0"));

            Assert.AreEqual(2, repositoryComposite.Count);
        }

        [TestMethod]
        public void TestNoRepositoriesFindPackages()
        {
            CollectionAssert.AreEqual(Array.Empty<IPackage>(), repositoryComposite.FindPackages("foo"));
        }

        [TestMethod]
        public void TestNoRepositoriesSearch()
        {
            CollectionAssert.AreEqual(Array.Empty<SearchResult>(), repositoryComposite.Search("foo"));
        }

        [TestMethod]
        public void TestNoRepositoriesGetPackages()
        {
            CollectionAssert.AreEqual(Array.Empty<IPackage>(), repositoryComposite.GetPackages());
        }
    }
}

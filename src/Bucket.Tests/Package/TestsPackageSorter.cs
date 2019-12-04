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
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Bucket.Tests.Package
{
    [TestClass]
    public class TestsPackageSorter
    {
        private Mock<IPackage> bar;
        private Mock<IPackage> baz;
        private Mock<IPackage> foo;
        private Mock<IPackage> aux;
        private Mock<IPackage> auux;

        [TestInitialize]
        public void TestInit()
        {
            foo = new Mock<IPackage>();
            foo.Setup((o) => o.GetName()).Returns("foo");
            foo.Setup((o) => o.GetRequires()).Returns(new[]
            {
                new Link("foo", "bar", null),
            });
            foo.Setup((o) => o.GetRequiresDev()).Returns(new[]
            {
                new Link("foo", "baz", null),
            });

            aux = new Mock<IPackage>();
            aux.Setup((o) => o.GetName()).Returns("aux");
            aux.Setup((o) => o.GetRequires()).Returns(new[]
            {
                new Link("aux", "bar", null),
            });

            auux = new Mock<IPackage>();
            auux.Setup((o) => o.GetName()).Returns("auux");
            auux.Setup((o) => o.GetRequires()).Returns(new[]
            {
                new Link("auux", "foo", null),
                new Link("auux", "baz", null),
            });

            bar = new Mock<IPackage>();
            bar.Setup((o) => o.GetName()).Returns("bar");

            baz = new Mock<IPackage>();
            baz.Setup((o) => o.GetName()).Returns("baz");
            baz.Setup((o) => o.GetRequires()).Returns(new[]
            {
                new Link("baz", "bar", null),
            });
        }

        [TestMethod]
        public void TestSortPackages()
        {
            var packages = PackageSorter.SortPackages(new[] { aux.Object, foo.Object, bar.Object, baz.Object, auux.Object });

            CollectionAssert.AreEqual(
                new[]
                {
                    "bar", "baz", "foo", "aux", "auux",
                }, Arr.Map(packages, (package) => package.GetName()));

            packages = PackageSorter.SortPackages(new[] { auux.Object, bar.Object, foo.Object, baz.Object, aux.Object });

            CollectionAssert.AreEqual(
                new[]
                {
                    "bar", "baz", "foo", "auux", "aux",
                }, Arr.Map(packages, (package) => package.GetName()));
        }

        [TestMethod]
        public void TestSortPackagesAsc()
        {
            var packages = PackageSorter.SortPackages(new[] { aux.Object, foo.Object, bar.Object, baz.Object, auux.Object }, true);

            CollectionAssert.AreEqual(
                new[]
                {
                    "aux", "auux", "foo", "baz", "bar",
                }, Arr.Map(packages, (package) => package.GetName()));
        }

        [TestMethod]
        public void TestSortReverse()
        {
            var packages = PackageSorter.SortPackages(new[] { aux.Object, foo.Object, bar.Object, baz.Object, auux.Object });
            Array.Reverse(packages);

            CollectionAssert.AreEqual(
                new[]
                {
                    "auux", "aux", "foo", "baz", "bar",
                }, Arr.Map(packages, (package) => package.GetName()));
        }
    }
}

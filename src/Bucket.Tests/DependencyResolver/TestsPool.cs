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

using Bucket.DependencyResolver;
using Bucket.Exception;
using Bucket.Package;
using Bucket.Plugin;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Bucket.Tests.DependencyResolver
{
    [TestClass]
    public class TestsPool
    {
        [TestMethod]
        public void TestPool()
        {
            var pool = new Pool();
            var package = MockPackage("foo", "1.0.0");

            pool.AddRepository(MockRepository(package));

            CollectionAssert.AreEqual(new[] { package }, pool.WhatProvides("foo"));
            CollectionAssert.AreEqual(new[] { package }, pool.WhatProvides("foo"));
        }

        [TestMethod]
        public void TestPoolWithFilterRequires()
        {
            var pool = new Pool(filterRequires: new Dictionary<string, IConstraint>()
            {
                { "foo", new Constraint(">", "1.0.0") },
            });

            var package = MockPackage("foo", "2.0.0");
            var packageBeta = MockPackage("foo", "1.0.0-beta");
            var packageAlpha = MockPackage("foo", "1.0.0-alpha");

            var repository = MockRepository(
                package, packageBeta, packageAlpha);

            pool.AddRepository(repository);

            CollectionAssert.AreEqual(new[] { package }, pool.WhatProvides("foo"));
        }

        [TestMethod]
        public void TestPoolWithFilterRequiresMatchPlatformRepository()
        {
            var pool = new Pool(filterRequires: new Dictionary<string, IConstraint>()
            {
                { "foo", new Constraint(">", "1.0.0") },
                { PluginManager.PluginRequire, new Constraint(">", "0.0.0") },
            });

            var package = MockPackage("foo", "2.0.0");
            var packageLow = MockPackage("foo", "1.0.0");

            var repository = MockRepository(
               package, packageLow);

            pool.AddRepository(repository);
            pool.AddRepository(new RepositoryPlatform());

            CollectionAssert.AreEqual(new[] { package }, pool.WhatProvides("foo"));
            Assert.AreEqual(1, pool.WhatProvides(PluginManager.PluginRequire).Length);
        }

        [TestMethod]
        public void TestPoolWithStabilitiesAndFilterRequires()
        {
            var pool = new Pool(
                filterRequires: new Dictionary<string, IConstraint>()
            {
                { "foo", new Constraint(">", "1.0.0") },
            }, stabilityFlags: new Dictionary<string, Stabilities>()
            {
                { "foo", Stabilities.Beta },
            });

            var package = MockPackage("foo", "2.0.0");
            var packageBeta = MockPackage("foo", "1.0.0-beta");
            var packageAlpha = MockPackage("foo", "1.0.0-alpha");

            var repository = MockRepository(
                package, packageBeta, packageAlpha);

            pool.AddRepository(repository);

            CollectionAssert.AreEqual(new[] { package }, pool.WhatProvides("foo"));
        }

        [TestMethod]
        public void TestPoolIgnoresIrrelevantPackages()
        {
            var pool = new Pool(Stabilities.Stable, new Dictionary<string, Stabilities>()
            {
                { "bar", Stabilities.Beta },
            });

            var package = MockPackage("bar", "1.0.0");
            var packageBeta = MockPackage("bar", "1.0.0-beta");
            var packageAlpha = MockPackage("bar", "1.0.0-alpha");

            var packageOther = MockPackage("foo", "1");
            var packageOtherRC = MockPackage("foo", "1rc");

            var repository = MockRepository(
                package, packageBeta, packageAlpha, packageOther, packageOtherRC);

            pool.AddRepository(repository);
            CollectionAssert.AreEqual(new[] { package, packageBeta }, pool.WhatProvides("bar"));
            CollectionAssert.AreEqual(new[] { packageOther }, pool.WhatProvides("foo"));
        }

        [TestMethod]
        public void TestGetPriorityWhenRepositoryIsAdded()
        {
            var pool = new Pool();
            var repositoryFirst = MockRepository();
            var repositorySecond = MockRepository();

            pool.AddRepository(repositoryFirst);
            pool.AddRepository(repositorySecond);

            Assert.AreEqual(0, pool.GetPriority(repositoryFirst));
            Assert.AreEqual(-1, pool.GetPriority(repositorySecond));
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "Could not determine repository priority.")]
        public void TestGetNoAddedRepository()
        {
            var pool = new Pool();
            pool.GetPriority(MockRepository());
        }

        [TestMethod]
        public void TestWhatProvidesSamePackageForDifferentRepositories()
        {
            var pool = new Pool();

            var packageFirst = MockPackage("foo", "1.0.0");
            var packageSecond = MockPackage("foo", "1.0.0");
            var packageThird = MockPackage("foo", "2.0.0");

            var repositoryFirst = MockRepository(packageFirst);
            var repositorySecond = MockRepository(packageSecond, packageThird);

            pool.AddRepository(repositoryFirst);
            pool.AddRepository(repositorySecond);

            CollectionAssert.AreEqual(
                new[] { packageFirst, packageSecond, packageThird },
                pool.WhatProvides("foo"));
        }

        [TestMethod]
        public void TestWhatProvidesPackageWithConstraint()
        {
            var pool = new Pool();

            var packageFirst = MockPackage("foo", "1.0.0");
            var packageSecond = MockPackage("foo", "2.0.0");

            var repository = MockRepository(packageFirst, packageSecond);

            pool.AddRepository(repository);

            CollectionAssert.AreEqual(
                new[] { packageFirst, packageSecond },
                pool.WhatProvides("foo"));

            CollectionAssert.AreEqual(
                new[] { packageSecond },
                pool.WhatProvides("foo", new Constraint("==", "2.0.0")));

            CollectionAssert.AreEqual(
                new[] { packageSecond },
                pool.WhatProvides("foo", new Constraint(">", "1.0.0")));

            CollectionAssert.AreEqual(
               new[] { packageFirst },
               pool.WhatProvides("foo", new Constraint("<=", "1.0.0")));
        }

        [TestMethod]
        public void TestWhatProvidesWhenPackageCannotBeFound()
        {
            var pool = new Pool();

            CollectionAssert.AreEqual(
                Array.Empty<IPackage>(),
                pool.WhatProvides("foo"));
        }

        [TestMethod]
        public void TestWhatProvidesWithProvide()
        {
            var pool = new Pool();

            var packageLinkFirst = MockLink("bar", new Constraint("==", "2.0.0"));
            var packageLinkSecond = MockLink("bar", new Constraint("==", "3.0.0"));
            var packageLinkThird = MockLink("none", new Constraint("==", "3.0.0"));

            var packageFirst = MockPackage("foo", "1.0.0", new[] { packageLinkFirst });
            var packageSecond = MockPackage("baz", "1.0.0", new[] { packageLinkSecond, packageLinkThird });

            var repository = MockRepository(packageFirst, packageSecond);
            pool.AddRepository(repository);

            CollectionAssert.AreEqual(
                new[] { packageFirst, packageSecond },
                pool.WhatProvides("bar"));

            CollectionAssert.AreEqual(
                new[] { packageSecond },
                pool.WhatProvides("bar", new Constraint(">", "2.0.0")));
        }

        [TestMethod]
        public void TestWhatProvidesWithReplace()
        {
            var pool = new Pool();

            var packageLinkFirst = MockLink("bar", new Constraint("==", "2.0.0"));
            var packageLinkSecond = MockLink("bar", new Constraint("==", "3.0.0"));
            var packageLinkThird = MockLink("none", new Constraint("==", "3.0.0"));
            var packageLinkFourth = MockLink("bar", new Constraint("==", "4.0.0"));

            var packageFirst = MockPackage("foo", "1.0.0", null, new[] { packageLinkFirst });
            var packageSecond = MockPackage("baz", "1.0.0", null, new[] { packageLinkSecond, packageLinkThird, packageLinkFourth });

            var repository = MockRepository(packageFirst, packageSecond);
            pool.AddRepository(repository);

            CollectionAssert.AreEqual(
                new[] { packageFirst, packageSecond, packageSecond },
                pool.WhatProvides("bar"));

            CollectionAssert.AreEqual(
                new[] { packageSecond, packageSecond },
                pool.WhatProvides("bar", new Constraint(">", "2.0.0")));
        }

        [TestMethod]
        public void TestGetPackageById()
        {
            var pool = new Pool();

            var packageFirst = MockPackage("foo", "1.0.0");
            var packageSecond = MockPackage("bar", "1.0.0");

            var repository = MockRepository(packageFirst, packageSecond);

            pool.AddRepository(repository);

            Assert.AreSame(packageFirst, pool.GetPackageById(1));
            Assert.AreSame(packageSecond, pool.GetPackageById(2));
        }

        [TestMethod]
        public void TestGetPackageByLiteral()
        {
            var pool = new Pool();

            var packageFirst = MockPackage("foo", "1.0.0");
            var packageSecond = MockPackage("bar", "1.0.0");

            var repository = MockRepository(packageFirst, packageSecond);

            pool.AddRepository(repository);

            Assert.AreSame(packageFirst, pool.GetPackageByLiteral(-1));
            Assert.AreSame(packageSecond, pool.GetPackageByLiteral(-2));
        }

        [TestMethod]
        [DataRow(-1, "uninstall")]
        [DataRow(1, "keep")]
        [DataRow(2, "install")]
        [DataRow(-2, "don't install")]
        public void TestLiteralToPrettyString(int literal, string expected)
        {
            var pool = new Pool();

            var packageFirst = MockPackage("foo", "1.0.0");
            var packageSecond = MockPackage("bar", "1.0.0");

            // package             literal      packageId
            // packageFirst        -1           1
            // packageSecond       -2           2
            var repository = MockRepository(packageFirst, packageSecond);

            pool.AddRepository(repository);

            var actual = pool.LiteralToPrettyString(literal, new Dictionary<int, IPackage>()
            {
                { 1, packageFirst },
            });

            StringAssert.Contains(actual, expected);
        }

        [TestMethod]
        public void TestWhiteList()
        {
            var pool = new Pool();
            pool.SetWhiteList(new HashSet<int>()
            {
                1, 2,
            });

            var packageFirst = MockPackage("foo", "1.0.0");
            var packageSecond = MockPackage("bar", "1.0.0");
            var packageThird = MockPackage("bar", "1.0.0");

            var repository = MockRepository(packageFirst, packageSecond, packageThird);

            pool.AddRepository(repository);

            CollectionAssert.AreEqual(new[] { packageSecond }, pool.WhatProvides("bar"));
        }

        [TestMethod]
        public void TestWhiteListWithPackageAlias()
        {
            var pool = new Pool();
            pool.SetWhiteList(new HashSet<int>()
            {
                2,
            });
            var packageFirst = MockPackage("foo", "1.0.0.0");
            var aliasPackage = new PackageAlias(packageFirst, "1.2", "1.2.0.0");
            var repository = MockRepository(aliasPackage, packageFirst);
            pool.AddRepository(repository);

            var packages = pool.WhatProvides("foo");

            Assert.AreEqual(2, packages.Length);
            Assert.AreEqual("1.2.0.0", packages[0].GetVersionPretty());
            Assert.AreEqual("1.2", packages[0].GetVersion());
            Assert.AreSame(packageFirst, ((PackageAlias)packages[0]).GetAliasOf());
            Assert.AreEqual("1.0.0.0", packages[1].GetVersion());
        }

        private IRepository MockRepository(params IPackage[] packages)
        {
            return Helper.MockRepository(packages);
        }

        private IPackage MockPackage(string name, string version, Link[] provides = null, Link[] replaces = null)
        {
            return Helper.MockPackage(name, version, provides, replaces);
        }

        private Link MockLink(string target, IConstraint constraint = null)
        {
            return new Link(string.Empty, target, constraint);
        }
    }
}

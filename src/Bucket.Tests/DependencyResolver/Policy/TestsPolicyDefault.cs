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
using Bucket.DependencyResolver.Policy;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Bucket.Tests.DependencyResolver.Policy
{
    [TestClass]
    public class TestsPolicyDefault
    {
        private Pool pool;
        private IPolicy policy;
        private RepositoryArray repository;
        private RepositoryArray repositoryInstalled;

        [TestInitialize]
        public void Initialize()
        {
            pool = new Pool(Stabilities.Dev);
            policy = new PolicyDefault();
            repository = new RepositoryArray();
            repositoryInstalled = new RepositoryArray();
        }

        [TestMethod]
        public void TestSelectSingle()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            repository.AddPackage(packageFoo);
            pool.AddRepository(repository);

            var literals = new[] { packageFoo.Id };
            var expected = new[] { packageFoo.Id };

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectNewest()
        {
            var packageFooOldest = Helper.MockPackage("foo", "1.0");
            var packageFooNewest = Helper.MockPackage("foo", "2.0");

            repository.AddPackage(packageFooOldest);
            repository.AddPackage(packageFooNewest);
            pool.AddRepository(repository);

            var literals = new[] { packageFooOldest.Id, packageFooNewest.Id };
            var expected = new[] { packageFooNewest.Id };

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectNewestPicksLatest()
        {
            var packageFooOldest = Helper.MockPackage("foo", "1.0.0");
            var packageFooNewest = Helper.MockPackage("foo", "1.0.1-alpha");

            repository.AddPackage(packageFooOldest);
            repository.AddPackage(packageFooNewest);
            pool.AddRepository(repository);

            var literals = new[] { packageFooOldest.Id, packageFooNewest.Id };
            var expected = new[] { packageFooNewest.Id };

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectNewestPicksLatestStableWithPreferStable()
        {
            var packageFooStable = Helper.MockPackage("foo", "1.0.0");
            var packageFooAlpha = Helper.MockPackage("foo", "1.0.1-alpha");

            repository.AddPackage(packageFooStable);
            repository.AddPackage(packageFooAlpha);
            pool.AddRepository(repository);

            var literals = new[] { packageFooStable.Id, packageFooAlpha.Id };
            var expected = new[] { packageFooStable.Id };

            var policyPreferStable = new PolicyDefault(true);
            var actual = policyPreferStable.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectNewestWithDevPicksNonDev()
        {
            var packageFooDev = Helper.MockPackage("foo", "dev-foo");
            var packageFooNonDev = Helper.MockPackage("foo", "1.0.0");

            repository.AddPackage(packageFooDev);
            repository.AddPackage(packageFooNonDev);
            pool.AddRepository(repository);

            var literals = new[] { packageFooDev.Id, packageFooNonDev.Id };
            var expected = new[] { packageFooNonDev.Id };

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectNewestOverThanInstalled()
        {
            var packageFoo = Helper.MockPackage("foo", "2.0");
            var packageFooInstalled = Helper.MockPackage("foo", "1.0");

            repository.AddPackage(packageFoo);
            repositoryInstalled.AddPackage(packageFooInstalled);

            pool.AddRepository(repositoryInstalled);
            pool.AddRepository(repository);

            var literals = new[] { packageFoo.Id, packageFooInstalled.Id };
            var expected = new[] { packageFoo.Id };

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectFirstRepository()
        {
            var repositoryOther = new RepositoryArray();

            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageFooImportant = Helper.MockPackage("foo", "1.0");

            repository.AddPackage(packageFoo);
            repositoryOther.AddPackage(packageFooImportant);

            // The order of addition determines the priority.
            pool.AddRepository(repositoryInstalled);
            pool.AddRepository(repositoryOther);
            pool.AddRepository(repository);

            var literals = new[] { packageFoo.Id, packageFooImportant.Id };
            var expected = new[] { packageFooImportant.Id };

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestRepositoryOrderingAffectsPriority()
        {
            var repository1 = new RepositoryArray();
            var repository2 = new RepositoryArray();

            var package1 = Helper.MockPackage("foo", "1.0");
            var package2 = Helper.MockPackage("foo", "1.1");
            var package3 = Helper.MockPackage("foo", "1.1");
            var package4 = Helper.MockPackage("foo", "1.2");

            repository1.AddPackage(package1);
            repository1.AddPackage(package2);
            repository2.AddPackage(package3);
            repository2.AddPackage(package4);

            pool.AddRepository(repository1);
            pool.AddRepository(repository2);

            var literals = new[] { package1.Id, package2.Id, package3.Id, package4.Id };
            var expected = new[] { package2.Id };

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);

            pool = new Pool(Stabilities.Dev);
            pool.AddRepository(repository2);
            pool.AddRepository(repository1);

            expected = new[] { package4.Id };

            actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectRootPackageFirst()
        {
            var packageFooImportment = Helper.MockPackage("FOO", "dev-feature-a");
            var packageFooAliasImportment = Helper.MockPackageAlias(packageFooImportment, "2.1.x-dev");
            packageFooAliasImportment.SetRootPackageAlias();

            repository.AddPackage(packageFooImportment);
            repository.AddPackage(packageFooAliasImportment);

            var packageFoo2Importment = Helper.MockPackage("FOO", "dev-master");
            var packageFoo2AliasImportment = Helper.MockPackageAlias(packageFoo2Importment, "2.1.x-dev");

            repository.AddPackage(packageFoo2Importment);
            repository.AddPackage(packageFoo2AliasImportment);

            pool.AddRepository(repositoryInstalled);
            pool.AddRepository(repository);

            var packages = pool.WhatProvides("foo", new Constraint("=", $"2.1.{VersionParser.VersionMax}.{VersionParser.VersionMaster}"));
            var literals = new List<int>();
            Array.ForEach(packages, (package) => literals.Add(package.Id));

            var expected = new[] { packageFooAliasImportment.Id };
            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals.ToArray());
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectAllProviders()
        {
            var fooReplaces = new[]
            {
                new Link("foo", "BAZ", new Constraint("=", "1.0"), "provides"),
            };

            var barReplaces = new[]
            {
                new Link("bar", "BAZ", new Constraint("=", "1.0"), "provides"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", null, fooReplaces);
            var packageBar = Helper.MockPackage("bar", "2.0", null, barReplaces);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            pool.AddRepository(repository);

            var literals = new[] { packageFoo.Id, packageBar.Id };
            var expected = literals;

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [Description("There is no replacement provider will be ranked first in front.")]
        public void TestPreferNonReplacingFromSameRepository()
        {
            var barReplaces = new[]
            {
                new Link("bar", "foo", new Constraint("=", "1.0"), "provides"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageBar = Helper.MockPackage("bar", "2.0", null, barReplaces);

            repository.AddPackage(packageBar);
            repository.AddPackage(packageFoo);

            pool.AddRepository(repository);

            var literals = new[] { packageFoo.Id, packageBar.Id };
            var expected = literals;

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSelectLowest()
        {
            var lowestPolicy = new PolicyDefault(false, true);

            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageBar = Helper.MockPackage("foo", "2.0");

            repository.AddPackage(packageBar);
            repository.AddPackage(packageFoo);

            pool.AddRepository(repository);

            var literals = new[] { packageFoo.Id, packageBar.Id };
            var expected = new[] { packageFoo.Id };

            var actual = lowestPolicy.SelectPreferredPackages(pool, GetInstalledMap(), literals);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestPreferReplacingPackageFromSameVendor()
        {
            // test default order.
            var fooReplaces = new[]
            {
                new Link("@vendor-foo/replacer", "@vendor-foo/package", new Constraint("=", "1.0"), "replaces"),
            };

            var barReplaces = new[]
            {
                new Link("@vendor-bar/replacer", "@vendor-foo/package", new Constraint("=", "1.0"), "replaces"),
            };

            var packageFoo = Helper.MockPackage("@vendor-foo/replacer", "1.0", null, fooReplaces);
            var packageBar = Helper.MockPackage("@vendor-bar/replacer", "1.0", null, barReplaces);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            pool.AddRepository(repository);

            var literals = new[] { packageFoo.Id, packageBar.Id };
            var expected = literals;

            var actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals, "@vendor-foo/package");
            CollectionAssert.AreEqual(expected, actual);

            // test with reversed order in repository
            packageFoo = Helper.MockPackage("@vendor-foo/replacer", "1.0", null, fooReplaces);
            packageBar = Helper.MockPackage("@vendor-bar/replacer", "1.0", null, barReplaces);

            var reversedRepository = new RepositoryArray();
            reversedRepository.AddPackage(packageBar);
            reversedRepository.AddPackage(packageFoo);

            var devPool = new Pool(Stabilities.Dev);
            devPool.AddRepository(reversedRepository);

            actual = policy.SelectPreferredPackages(pool, GetInstalledMap(), literals, "@vendor-foo/package");
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestFindUpdatePackages()
        {
            var barReplaces = new[]
            {
                new Link("bar", "foo", new Constraint("=", "3.0"), "replaces"),
            };

            var packageFoo1 = Helper.MockPackage("foo", "1.0");
            var packageFoo2 = Helper.MockPackage("foo", "2.0");
            var packageBar = Helper.MockPackage("bar", "2.0", null, barReplaces);
            var packageBaz = Helper.MockPackage("baz", "2.0");

            repository.AddPackage(packageFoo1);
            repository.AddPackage(packageFoo2);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBaz);

            pool.AddRepository(repository);

            var expected = new[] { packageFoo2, packageBar };

            var actual = policy.FindUpdatePackages(pool, GetInstalledMap(), packageFoo1);
            CollectionAssert.AreEqual(expected, actual);
        }

        private IDictionary<int, IPackage> GetInstalledMap(IEnumerable<IPackage> repoInstalled = null)
        {
            var installed = new Dictionary<int, IPackage>();
            foreach (var package in repoInstalled ?? repositoryInstalled)
            {
                installed[package.Id] = package;
            }

            return installed;
        }
    }
}

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
using Bucket.Exception;
using Bucket.IO;
using Bucket.Package;
using Bucket.Package.Loader;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Tester;
using Bucket.Tests.Support;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace Bucket.Tests.Package.Loader
{
    [TestClass]
    public class TestsLoaderPackageRoot
    {
        private LoaderPackageRoot loader;
        private Mock<Config> config;
        private Mock<RepositoryManager> manager;
        private TesterIOConsole tester;
        private IIO io;

        [TestInitialize]
        public void Initialize()
        {
            tester = new TesterIOConsole();
            io = tester.Mock();
            config = new Mock<Config>(true, null);
            manager = new Mock<RepositoryManager>(IONull.That, config.Object, null, null);
            loader = new LoaderPackageRoot(manager.Object, config.Object, io);
        }

        [TestMethod]
        [DataFixture("package-root-stability-flags.json")]
        public void TestStabilityFlagsParsing(ConfigBucket bucket)
        {
            var package = loader.Load<IPackageRoot>(bucket);

            Assert.AreEqual(Stabilities.Alpha, package.GetMinimumStability());
            CollectionAssert.AreEqual(
                new SortedDictionary<string, Stabilities>
                {
                    { "bar/baz", Stabilities.Dev },
                    { "multi/complex", Stabilities.Dev },
                    { "multi/lowest-wins", Stabilities.Dev },
                    { "multi/or", Stabilities.Dev },
                    { "multi/without-flags1", Stabilities.Dev },
                    { "multi/without-flags2", Stabilities.Alpha },
                    { "qux/quux", Stabilities.RC },
                }, new SortedDictionary<string, Stabilities>(package.GetStabilityFlags()));
        }

        [TestMethod]
        [DataFixture("package-root-aliases.json")]
        public void TestAliasesParsing(ConfigBucket bucket)
        {
            var package = loader.Load<IPackageRoot>(bucket);

            var actual = Arr.Map(package.GetAliases(), (alias) =>
            {
                return $"{alias.Package}, {alias.Version}, {alias.Alias}, {alias.AliasNormalized}";
            });

            CollectionAssert.AreEqual(
                new[]
                {
                    "foo/bar, dev-bug-fixed, 1.0.x-dev, 1.0.9999999.9999999-dev",
                    "bar/baz, 1.0.9999999.9999999-dev, 1.2.0, 1.2.0.0",
                    "bar/boo, 1.0.5.0, 1.2.x-dev, 1.2.9999999.9999999-dev",
                }, actual);
        }

        [TestMethod]
        [DataFixture("package-root-reference.json")]
        public void TestReferenceParsing(ConfigBucket bucket)
        {
            var package = loader.Load<IPackageRoot>(bucket);

            CollectionAssert.AreEqual(
                new SortedDictionary<string, string>
                {
                    { "foo/aux", "d123456789012345678901234567890123456789" },
                    { "foo/bar", "a123456789012345678901234567890123456789" },
                    { "foo/baz", "b123456789012345678901234567890123456789" },
                    { "foo/boo", "c123456789012345678901234567890123456789" },
                }, new SortedDictionary<string, string>(package.GetReferences()));
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "The \"version\" property not allowed to be empty.")]
        public void TestNoVersionThrowException()
        {
            var bucket = new ConfigBucket
            {
                Version = string.Empty,
            };
            loader.Load<IPackageRoot>(bucket);
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "The \"name\" property not allowed to be empty.")]
        public void TestNoNameThrowException()
        {
            var bucket = new ConfigBucket
            {
                Name = string.Empty,
            };
            loader.Load<IPackageRoot>(bucket);
        }

        [TestMethod]
        [DataRow(typeof(IPackage))]
        [DataRow(typeof(IPackageComplete))]
        [ExpectedExceptionAndMessage(typeof(ArgumentException), "The type must implement \"IPackageRoot\".")]
        public void TestUnallowedPackageTypes(Type type)
        {
            var bucket = new ConfigBucket();
            loader.Load(bucket, type);
        }

        [TestMethod]
        [DataFixture("package-root-require-self.json")]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "Root package \"test\" cannot require itself in its bucket.json")]
        public void TestRequireSelfThrowException(ConfigBucket bucket)
        {
            loader.Load<IPackageRoot>(bucket);
        }
    }
}

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
using Bucket.Package;
using Bucket.Package.Version;
using Bucket.Semver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using BPackage = Bucket.Package.Package;
using BVersionParser = Bucket.Package.Version.VersionParser;

namespace Bucket.Tests.Package.Version
{
    [TestClass]
    public class TestsVersionSelector
    {
        private Mock<Pool> pool;
        private VersionSelector selector;

        [TestInitialize]
        public void Initialize()
        {
            pool = new Mock<Pool>(Stabilities.Stable, null, null);
            selector = new VersionSelector(pool.Object);
        }

        [TestMethod]
        public void TestLatestVersionIsReturned()
        {
            var packageName = "foobar";
            var package1 = CreatePackage("1.2.1");
            var package2 = CreatePackage("1.2.2");
            var package3 = CreatePackage("1.2.0");

            pool.Setup((o) => o.WhatProvides(packageName, null, true, false))
                .Returns(new[] { package1, package2, package3 });

            var best = selector.FindBestPackage(packageName);
            Assert.AreEqual(package2, best, "Latest version should be 1.2.2");
        }

        [TestMethod]
        public void TestMostStableVersionIsReturned()
        {
            var packageName = "foobar";
            var package1 = CreatePackage("1.0.0");
            var package2 = CreatePackage("1.1.0-beta");

            pool.Setup((o) => o.WhatProvides(packageName, null, true, false))
                .Returns(new[] { package1, package2 });

            var best = selector.FindBestPackage(packageName);
            Assert.AreEqual(package1, best, "Latest most stable version should be returned (1.0.0)");
        }

        [TestMethod]
        public void TestMostStableVersionIsReturnedRegardlessOfOrder()
        {
            var packageName = "foobar";
            var package1 = CreatePackage("2.x-dev");
            var package2 = CreatePackage("2.0.0-beta3");

            pool.SetupSequence((o) => o.WhatProvides(packageName, null, true, false))
                .Returns(new[] { package1, package2 })
                .Returns(new[] { package2, package1 });

            var best = selector.FindBestPackage(packageName);
            Assert.AreEqual(package2, best, "Expecting 2.0.0-beta3, cause beta is more stable than dev");

            best = selector.FindBestPackage(packageName);
            Assert.AreEqual(package2, best, "Expecting 2.0.0-beta3, cause beta is more stable than dev");
        }

        [TestMethod]
        public void TestHighestVersionIsReturned()
        {
            var packageName = "foobar";
            var package1 = CreatePackage("1.0.0");
            var package2 = CreatePackage("1.1.0-beta");

            pool.Setup((o) => o.WhatProvides(packageName, null, true, false))
                .Returns(new[] { package1, package2 });

            var best = selector.FindBestPackage(packageName, null, Stabilities.Dev);
            Assert.AreEqual(package2, best, "Latest version should be returned (1.1.0-beta)");
        }

        [TestMethod]
        public void TestHighestVersionMatchingStabilityIsReturned()
        {
            var packageName = "foobar";
            var package1 = CreatePackage("1.0.0");
            var package2 = CreatePackage("1.1.0-beta");
            var package3 = CreatePackage("1.2.0-alpha");

            pool.Setup((o) => o.WhatProvides(packageName, null, true, false))
                .Returns(new[] { package1, package2, package3 });

            var best = selector.FindBestPackage(packageName, null, Stabilities.Beta);
            Assert.AreEqual(package2, best, "Latest version should be returned (1.1.0-beta)");
        }

        [TestMethod]
        public void TestMostStableUnstableVersionIsReturned()
        {
            var packageName = "foobar";
            var package1 = CreatePackage("1.1.0-beta");
            var package2 = CreatePackage("1.2.0-alpha");

            pool.Setup((o) => o.WhatProvides(packageName, null, true, false))
               .Returns(new[] { package1, package2 });

            var best = selector.FindBestPackage(packageName, null, Stabilities.Stable);
            Assert.AreEqual(package1, best, "Latest version should be returned (1.1.0-beta)");
        }

        [TestMethod]
        public void TestNullReturnedOnNoPackages()
        {
            pool.Setup((o) => o.WhatProvides(It.IsAny<string>(), null, true, false))
               .Returns(Array.Empty<IPackage>());

            var best = selector.FindBestPackage("foobaz");
            Assert.AreEqual(null, best, "No versions are available returns null");
        }

        [TestMethod]
        [DataRow("1.2.1", false, Stabilities.Stable, "^1.2")]
        [DataRow("1.2", false, Stabilities.Stable, "^1.2")]
        [DataRow("v1.2.1", false, Stabilities.Stable, "^1.2")]
        [DataRow("3.1.2-pl2", false, Stabilities.Stable, "^3.1")]
        [DataRow("3.1.2-patch", false, Stabilities.Stable, "^3.1")]
        [DataRow("2.0-beta.1", false, Stabilities.Beta, "^2.0@beta")]
        [DataRow("3.1.2-alpha5", false, Stabilities.Alpha, "^3.1@alpha")]
        [DataRow("3.0-RC2", false, Stabilities.RC, "^3.0@RC")]
        [DataRow("0.1.0", false, Stabilities.Stable, "^0.1.0")]
        [DataRow("0.1.3", false, Stabilities.Stable, "^0.1.3")]
        [DataRow("0.0.3", false, Stabilities.Stable, "^0.0.3")]
        [DataRow("0.0.3-alpha", false, Stabilities.Alpha, "^0.0.3@alpha")]
        [DataRow("v20121020", false, Stabilities.Stable, "v20121020")]
        [DataRow("v20121020.2", false, Stabilities.Stable, "v20121020.2")]
        [DataRow("dev-master", true, Stabilities.Dev, "dev-master")]
        [DataRow("3.1.2-dev", true, Stabilities.Dev, "3.1.2-dev")]
        public void TestFindRecommendedRequireVersion(string versionPretty, bool isDev, Stabilities stability, string expectedVersion)
        {
            var versionParser = new BVersionParser();

            var package = new Mock<IPackage>();
            package.Setup((o) => o.GetVersionPretty()).Returns(versionPretty);
            package.Setup((o) => o.GetVersion()).Returns(versionParser.Normalize(versionPretty));
            package.Setup((o) => o.IsDev).Returns(isDev);
            package.Setup((o) => o.GetStability()).Returns(stability);

            var recommended = selector.FindRecommendedRequireVersion(package.Object);
            Assert.AreEqual(expectedVersion, recommended);
        }

        private IPackage CreatePackage(string version)
        {
            var parser = new BVersionParser();
            return new BPackage("foo", parser.Normalize(version), version);
        }
    }
}

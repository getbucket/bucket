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
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.Package
{
    [TestClass]
    public class TestsPackageAlias
    {
        [TestMethod]
        public void TestGetRequires()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");

            Assert.AreEqual(1, packageAlias.GetRequires().Length);
            Assert.AreEqual(
                "foobarbaz requires bar == 1.2",
                packageAlias.GetRequires()[0].GetPrettyString(mockPackage.Object));
        }

        [TestMethod]
        public void TestGetRequiresDev()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");

            Assert.AreEqual(1, packageAlias.GetRequiresDev().Length);
            Assert.AreEqual(
                "foobarbaz requiresDev bar == 1.2",
                packageAlias.GetRequiresDev()[0].GetPrettyString(mockPackage.Object));
        }

        [TestMethod]
        public void TestGetConflicts()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");

            Assert.AreEqual(2, packageAlias.GetConflicts().Length);
            Assert.AreEqual(
                "foobarbaz conflicts bar == 1.0",
                packageAlias.GetConflicts()[0].GetPrettyString(mockPackage.Object));
            Assert.AreEqual(
                "foobarbaz conflicts bar == 1.2",
                packageAlias.GetConflicts()[1].GetPrettyString(mockPackage.Object));
        }

        [TestMethod]
        public void TestGetProvides()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");

            Assert.AreEqual(2, packageAlias.GetProvides().Length);
            Assert.AreEqual(
                "foobarbaz provides bar == 1.0",
                packageAlias.GetProvides()[0].GetPrettyString(mockPackage.Object));
            Assert.AreEqual(
                "foobarbaz provides bar == 1.2",
                packageAlias.GetProvides()[1].GetPrettyString(mockPackage.Object));
        }

        [TestMethod]
        public void TestGetReplaces()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");

            Assert.AreEqual(2, packageAlias.GetProvides().Length);
            Assert.AreEqual(
                "foobarbaz replaces bar == 1.0",
                packageAlias.GetReplaces()[0].GetPrettyString(mockPackage.Object));
            Assert.AreEqual(
                "foobarbaz replaces bar == 1.2",
                packageAlias.GetReplaces()[1].GetPrettyString(mockPackage.Object));
        }

        [TestMethod]
        public void TestGetAliasOf()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");

            Assert.AreSame(mockPackage.Object, packageAlias.GetAliasOf());
        }

        [TestMethod]
        public void TestGetStability()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");
            Assert.AreEqual(Stabilities.Stable, packageAlias.GetStability());

            packageAlias = new PackageAlias(mockPackage.Object, "1.2-beta", "1.2.0.0-beta");
            Assert.AreEqual(Stabilities.Beta, packageAlias.GetStability());
        }

        [TestMethod]
        public void TestGetVersion()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");
            Assert.AreEqual("1.2", packageAlias.GetVersion());
        }

        [TestMethod]
        public void TestGetVersionPretty()
        {
            var mockPackage = MockGeneralPackage();
            var packageAlias = new PackageAlias(mockPackage.Object, "1.2", "1.2.0.0");
            Assert.AreEqual("1.2.0.0", packageAlias.GetVersionPretty());
        }

        private Mock<IPackage> MockGeneralPackage()
        {
            Link CreateLink(string description)
            {
                return new Link("foo", "bar", new Constraint("=", "1.0"), description, BasePackage.SelfVersion);
            }

            var mockPackage = new Mock<IPackage>();
            mockPackage.Setup((o) => o.GetName()).Returns(() => "foo");
            mockPackage.Setup((o) => o.GetPrettyString()).Returns(() => "foobarbaz");
            mockPackage.Setup((o) => o.GetRequires()).Returns(() => new[] { CreateLink("requires") });
            mockPackage.Setup((o) => o.GetRequiresDev()).Returns(() => new[] { CreateLink("requiresDev") });
            mockPackage.Setup((o) => o.GetConflicts()).Returns(() => new[] { CreateLink("conflicts") });
            mockPackage.Setup((o) => o.GetProvides()).Returns(() => new[] { CreateLink("provides") });
            mockPackage.Setup((o) => o.GetReplaces()).Returns(() => new[] { CreateLink("replaces") });

            return mockPackage;
        }
    }
}

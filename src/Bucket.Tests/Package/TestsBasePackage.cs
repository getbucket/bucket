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

using Bucket.Exception;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Semver.Constraint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.Package
{
    [TestClass]
    public class TestsBasePackage
    {
        [TestMethod]
        public void TestGetName()
        {
            var mockPackage = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            Assert.AreEqual("foobar", mockPackage.Object.GetName());
        }

        [TestMethod]
        public void TestGetNameUnique()
        {
            var mockPackage = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            mockPackage.Setup((o) => o.GetVersion()).Returns(() => "1.2");
            Assert.AreEqual("foobar-1.2", mockPackage.Object.GetNameUnique());
        }

        [TestMethod]
        public void TestGetPrettyString()
        {
            var mockPackage = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            mockPackage.Setup((o) => o.GetVersionPretty()).Returns(() => "1.2.0.0");
            Assert.AreEqual("fooBar 1.2.0.0", mockPackage.Object.GetPrettyString());
        }

        [TestMethod]
        [DataRow("1.2.0.0 f81da29", "git", "f81da29e8a279af2b9af80fa041b84497a26ddf8", true)]
        [DataRow("1.2.0.0 f81da29e8a279af2b9af80fa041b84497a26ddf8", "git", "f81da29e8a279af2b9af80fa041b84497a26ddf8", false)]
        public void TestGetVersionPrettyFull(string expected, string sourceType, string sourceRefernece, bool truncate)
        {
            var mockPackage = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            mockPackage.Setup((o) => o.IsDev).Returns(() => true);
            mockPackage.Setup((o) => o.GetSourceType()).Returns(() => sourceType);
            mockPackage.Setup((o) => o.GetSourceReference()).Returns(() => sourceRefernece);
            mockPackage.Setup((o) => o.GetVersionPretty()).Returns(() => "1.2.0.0");

            Assert.AreEqual(expected, mockPackage.Object.GetVersionPrettyFull(truncate));
        }

        [TestMethod]
        public void TestGetNamePretty()
        {
            var mockPackage = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            Assert.AreEqual("fooBar", mockPackage.Object.GetNamePretty());
        }

        [TestMethod]
        public void TestGetNames()
        {
            var mockPackage = new Mock<BasePackage>("foo")
            {
                CallBase = true,
            };
            mockPackage.Setup((o) => o.GetProvides()).Returns(() =>
            {
                return new[] { new Link("foo", "bar", new Constraint("=", "1.0")) };
            });

            mockPackage.Setup((o) => o.GetReplaces()).Returns(() =>
            {
                return new[]
                {
                    new Link("foo", "baz", new Constraint("=", "2.0")),
                    new Link("foo", "bar", new Constraint("=", "2.0")),
                };
            });

            CollectionAssert.AreEqual(
                new[]
            {
                "foo", "bar", "baz",
            }, mockPackage.Object.GetNames());
        }

        [TestMethod]
        public void TestGetSetRepository()
        {
            var mockPackage = new Mock<BasePackage>("foo")
            {
                CallBase = true,
            };

            var mockRepository = new Mock<IRepository>();
            mockPackage.Object.SetRepository(mockRepository.Object);

            Assert.AreSame(mockRepository.Object, mockPackage.Object.GetRepository());
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(UnexpectedException), "A package can only be added to one repository.")]
        public void TestSetTwoRepository()
        {
            var mockPackage = new Mock<BasePackage>("foo")
            {
                CallBase = true,
            };

            var mockRepository1 = new Mock<IRepository>();
            var mockRepository2 = new Mock<IRepository>();

            mockPackage.Object.SetRepository(mockRepository1.Object);
            mockPackage.Object.SetRepository(mockRepository2.Object);
        }

        [TestMethod]
        public void TestEqual()
        {
            var mockPackage1 = new Mock<BasePackage>("foo")
            {
                CallBase = true,
            };

            var mockPackage2 = new Mock<BasePackage>("bar")
            {
                CallBase = true,
            };

            Assert.IsFalse(mockPackage1.Object.Equals(mockPackage2.Object));
        }

        [TestMethod]
        public void TestEqualWithPackageAlias()
        {
            var mockPackage1 = new Mock<BasePackage>("foo")
            {
                CallBase = true,
            };
            mockPackage1.Setup((o) => o.GetVersion()).Returns("1.0");
            var aliasPackage = new PackageAlias(mockPackage1.Object, "2.0", "2.0.0.0");

            Assert.IsTrue(mockPackage1.Object.Equals(aliasPackage));
            Assert.IsTrue(aliasPackage.Equals(mockPackage1.Object));
        }

        [TestMethod]
        public void TestToString()
        {
            var mockPackage = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            mockPackage.Setup((o) => o.GetVersion()).Returns(() => "1.2");

            Assert.AreEqual("foobar-1.2", mockPackage.Object.ToString());
        }

        [TestMethod]
        public void TestGetHashing()
        {
            var mockPackage1 = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            mockPackage1.Setup((o) => o.GetVersion()).Returns(() => "1.2");

            var mockPackage2 = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            mockPackage2.Setup((o) => o.GetVersion()).Returns(() => "1.2");

            Assert.IsTrue(mockPackage1.Object.GetHashCode() == mockPackage2.Object.GetHashCode());
        }

        [TestMethod]
        public void TestGetHashingWithPackageAlias()
        {
            var mockPackage1 = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            mockPackage1.Setup((o) => o.GetVersion()).Returns(() => "1.2");

            var mockPackage2 = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            mockPackage2.Setup((o) => o.GetVersion()).Returns(() => "1.2");
            var packageAlias = new PackageAlias(mockPackage2.Object, "2.0", "2.0.0.0");

            Assert.IsTrue(mockPackage1.Object.GetHashCode() == packageAlias.GetHashCode());
        }

        [TestMethod]
        public void TestClone()
        {
            var mockPackage = new Mock<BasePackage>("fooBar")
            {
                CallBase = true,
            };
            var package = mockPackage.Object;
            package.Id = 100;
            var mockRepository = new Mock<IRepository>();
            package.SetRepository(mockRepository.Object);

            var clone = (BasePackage)package.Clone();

            Assert.AreEqual(100, package.Id);
            Assert.AreEqual(-1, clone.Id);
            Assert.AreSame(mockRepository.Object, package.GetRepository());
            Assert.AreEqual(null, clone.GetRepository());
            Assert.IsTrue(package.GetName() == clone.GetName());
            Assert.IsTrue(package.GetNamePretty() == clone.GetNamePretty());
        }
    }
}

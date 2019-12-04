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
using Bucket.Installer;
using Bucket.IO;
using Bucket.Json;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Tester;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Bucket.Tests.Package
{
    [TestClass]
    public class TestsLocker
    {
        private Mock<JsonFile> jsonFile;
        private Mock<InstallationManager> installationManager;
        private IIO io;
        private TesterIOConsole tester;

        [TestInitialize]
        public void Initialize()
        {
            jsonFile = new Mock<JsonFile>(string.Empty, null, null);
            installationManager = new Mock<InstallationManager>();
            tester = new TesterIOConsole();
            io = tester.Mock();
        }

        [TestMethod]
        public void TestIsLocked()
        {
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, GetJsonContent());
            jsonFile.Setup((o) => o.Exists()).Returns(true);
            jsonFile.Setup((o) => o.Read<ConfigLocker>()).Returns(new ConfigLocker
            {
                Packages = Array.Empty<ConfigLockerPackage>(),
            });

            Assert.IsTrue(locker.IsLocked());
        }

        [TestMethod]
        [ExpectedException(typeof(RuntimeException))]
        public void TestGetNotLockedPackages()
        {
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, GetJsonContent());
            jsonFile.Setup((o) => o.Exists()).Returns(false);

            locker.GetLockedRepository();
        }

        [TestMethod]
        public void TestGetLockedPackages()
        {
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, GetJsonContent());
            jsonFile.Setup((o) => o.Exists()).Returns(true);
            jsonFile.Setup((o) => o.Read<ConfigLocker>()).Returns(new ConfigLocker
            {
                Packages = new[]
                {
                    new ConfigLockerPackage
                    {
                        Name = "foo",
                        Version = "1.0.0-beta",
                    },
                    new ConfigLockerPackage
                    {
                        Name = "bar",
                        Version = "0.2.0",
                    },
                },
            });

            var repository = locker.GetLockedRepository();
            Assert.AreNotEqual(null, repository.FindPackage("foo", "1.0.0-beta"));
            Assert.AreNotEqual(null, repository.FindPackage("bar", "0.2.0"));
        }

        [TestMethod]
        public void TestGetLockedPackageIncludeDev()
        {
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, GetJsonContent());
            jsonFile.Setup((o) => o.Exists()).Returns(true);
            jsonFile.Setup((o) => o.Read<ConfigLocker>()).Returns(new ConfigLocker
            {
                Packages = new[]
                {
                    new ConfigLockerPackage
                    {
                        Name = "foo",
                        Version = "1.0.0-beta",
                    },
                },
                PackagesDev = new[]
                {
                    new ConfigLockerPackage
                    {
                        Name = "bar",
                        Version = "0.2.0",
                    },
                },
            });

            var repository = locker.GetLockedRepository(true);
            Assert.AreNotEqual(null, repository.FindPackage("foo", "1.0.0-beta"));
            Assert.AreNotEqual(null, repository.FindPackage("bar", "0.2.0"));
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(
            typeof(RuntimeException),
            "The lock file does not contain require-dev information, run install with the --no-dev option or run update to install those packages.")]
        public void TestGetLockedPackagesIncludeDevNoPackages()
        {
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, GetJsonContent());
            jsonFile.Setup((o) => o.Exists()).Returns(true);
            jsonFile.Setup((o) => o.Read<ConfigLocker>()).Returns(new ConfigLocker());
            locker.GetLockedRepository(true);
        }

        [TestMethod]
        public void TestBeginSetLockData()
        {
            var jsonContent = GetJsonContent() + "    ";
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, jsonContent);

            var packageFooMock = new Mock<IPackage>();
            var packageBarMock = new Mock<IPackage>();

            packageFooMock.Setup((o) => o.GetNamePretty()).Returns("foo");
            packageFooMock.Setup((o) => o.GetVersionPretty()).Returns("1.0.0-beta");
            packageFooMock.Setup((o) => o.GetVersion()).Returns("1.0.0.0-beta");

            packageBarMock.Setup((o) => o.GetNamePretty()).Returns("bar");
            packageBarMock.Setup((o) => o.GetVersionPretty()).Returns("0.2.0");
            packageBarMock.Setup((o) => o.GetVersion()).Returns("0.2.0.0");

            var contentHash = Security.Md5(jsonContent.Trim());
            jsonFile.Setup((o) => o.Write(It.IsAny<object>())).Callback<object>((data) =>
            {
                data = JObject.FromObject(data);
                StringAssert.Contains(data.ToString(), contentHash);
                var expected =
@"{
  '_readme': 'This file is generated automatically',
  'content-hash': '59eedf9c4842dd75d51e100343398439',
  'packages': [
    {
      'name': 'bar',
      'version': '0.2.0'
    },
    {
      'name': 'foo',
      'version': '1.0.0-beta'
    }
  ],
  'packages-dev': [],
  'aliases': [],
  'minimum-stability': 'dev',
  'stability-flags': {
    'bar': 'stable',
    'foo': 'stable'
  },
  'prefer-stable': false,
  'prefer-lowest': false,
  'platform': {}
}";
                Assert.AreEqual(expected.Replace("'", "\"", StringComparison.Ordinal), data.ToString());
            });

            var ret = locker.BeginSetLockData(new[] { packageFooMock.Object, packageBarMock.Object })
                .SetMinimumStability(Stabilities.Dev)
                .SetPreferStable(false)
                .SetPreferLowest(false)
                .SetStabilityFlags(new Dictionary<string, Stabilities>
                {
                    { "foo", Stabilities.Stable },
                    { "bar", Stabilities.Stable },
                })
                .Save();

            Assert.AreEqual(true, ret);
            jsonFile.Verify((o) => o.Write(It.IsAny<object>()), Times.Once);
        }

        [TestMethod]
        public void BeginSetLockDataWithMinimal()
        {
            var jsonContent = GetJsonContent();
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, jsonContent);

            var packageFooMock = new Mock<IPackage>();
            var packageBarMock = new Mock<IPackage>();

            packageFooMock.Setup((o) => o.GetNamePretty()).Returns("foo");
            packageFooMock.Setup((o) => o.GetVersionPretty()).Returns("1.0.0-beta");
            packageFooMock.Setup((o) => o.GetVersion()).Returns("1.0.0.0-beta");

            packageBarMock.Setup((o) => o.GetNamePretty()).Returns("bar");
            packageBarMock.Setup((o) => o.GetVersionPretty()).Returns("0.2.0");
            packageBarMock.Setup((o) => o.GetVersion()).Returns("0.2.0.0");

            var contentHash = Security.Md5(jsonContent.Trim());
            jsonFile.Setup((o) => o.Write(It.IsAny<object>())).Callback<object>((data) =>
            {
                data = JObject.FromObject(data);
                StringAssert.Contains(data.ToString(), contentHash);
                var expected =
@"{
  '_readme': 'This file is generated automatically',
  'content-hash': '59eedf9c4842dd75d51e100343398439',
  'packages': [
    {
      'name': 'bar',
      'version': '0.2.0'
    },
    {
      'name': 'foo',
      'version': '1.0.0-beta'
    }
  ],
  'packages-dev': [],
  'aliases': [],
  'minimum-stability': 'stable',
  'stability-flags': {},
  'prefer-stable': true,
  'prefer-lowest': false,
  'platform': {}
}";
                Assert.AreEqual(expected.Replace("'", "\"", StringComparison.Ordinal), data.ToString());
            });

            var ret = locker.BeginSetLockData(new[] { packageFooMock.Object, packageBarMock.Object })
                .Save();

            Assert.AreEqual(true, ret);
            jsonFile.Verify((o) => o.Write(It.IsAny<object>()), Times.Once);
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "has no version or name and can not be locked.")]
        public void TestLockBadPackages()
        {
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, GetJsonContent());
            var packageFooMock = new Mock<IPackage>();
            packageFooMock.Setup((o) => o.GetNamePretty()).Returns("foo");

            locker.BeginSetLockData(new[] { packageFooMock.Object }).Save();
        }

        [TestMethod]
        public void TestIsFresh()
        {
            var jsonContent = GetJsonContent();
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, jsonContent);

            jsonFile.Setup((o) => o.Read<ConfigLocker>()).Returns(new ConfigLocker()
            {
                ContentHash = Security.Md5(jsonContent),
            });

            Assert.IsTrue(locker.IsFresh());
        }

        [TestMethod]
        public void TestIsFreshFalse()
        {
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, GetJsonContent());

            jsonFile.Setup((o) => o.Read<ConfigLocker>()).Returns(new ConfigLocker()
            {
                ContentHash = Security.Md5(GetJsonContent(new { name = "foo" })),
            });

            Assert.IsFalse(locker.IsFresh());
        }

        [TestMethod]
        public void TestFullData()
        {
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, GetJsonContent());
            jsonFile.Setup((o) => o.Exists()).Returns(true);
            jsonFile.Setup((o) => o.Read<ConfigLocker>()).Returns(new ConfigLocker
            {
                MinimumStability = Stabilities.Beta,
                PreferStable = false,
                PreferLowest = true,
            });

            Assert.AreEqual(Stabilities.Beta, locker.GetMinimumStability());
            Assert.AreEqual(false, locker.GetPreferStable());
            Assert.AreEqual(true, locker.GetPreferLowest());
            Assert.AreNotEqual(null, locker.GetAliases());
        }

        [TestMethod]
        public void TestDeleteWhenNoPackages()
        {
            var jsonContent = GetJsonContent();
            var locker = new Locker(io, jsonFile.Object, installationManager.Object, jsonContent);
            Assert.IsFalse(locker.BeginSetLockData(Array.Empty<IPackage>()).Save());
            jsonFile.Verify((o) => o.Delete(), Times.Once);
        }

        private string GetJsonContent(dynamic customData = null)
        {
            var data = new JObject
            {
                ["minimum-stability"] = "beta",
                ["name"] = "test",
            };

            if (customData != null)
            {
                data.Merge(JObject.FromObject(customData));
            }

            return data.ToString();
        }
    }
}

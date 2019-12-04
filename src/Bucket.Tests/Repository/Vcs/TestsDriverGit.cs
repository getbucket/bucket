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
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Repository.Vcs;
using Bucket.Tests.Support;
using Bucket.Util;
using GameBox.Console.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Text;

namespace Bucket.Tests.Repository.Vcs
{
    [TestClass]
    public class TestsDriverGit
    {
        private static string root;
        private IProcessExecutor process;
        private BaseFileSystem fileSystem;
        private Config config;

        [TestInitialize]
        public void Initialize()
        {
            root = Helper.GetTestFolder<TestsDriverGit>();
            process = new BucketProcessExecutor() { Cwd = root };
            fileSystem = new FileSystemLocal(root);
            config = new Config();

            fileSystem.Delete();
            Directory.CreateDirectory(root);
        }

        [TestCleanup]
        public void Cleanup()
        {
            fileSystem.Delete();
        }

        [TestMethod]
        public void TestLocalGit()
        {
            InitializeGitRepository();
            var driver = CreateAndInitializeDriver();

            Assert.AreEqual("master", driver.GetRootIdentifier());

            CollectionAssert.AreEqual(
                new[] { "0.1.0", "0.6.0", "1.0.0" },
                driver.GetTags().Keys.ToArray());

            CollectionAssert.AreEqual(
                new[] { "1.0", "feature/a", "master", "oldbranch" },
                driver.GetBranches().Keys.ToArray());

            Assert.AreEqual("git", driver.GetSource(null).Type);
            Assert.AreEqual(fileSystem.Root, driver.GetSource(null).Uri);
        }

        [TestMethod]
        public void TestChangedRootIdentifier()
        {
            InitializeGitRepository();
            Assert.AreEqual(0, process.Execute("git checkout 1.0"));
            var driver = CreateAndInitializeDriver();

            Assert.AreEqual("1.0", driver.GetRootIdentifier());
        }

        [TestMethod]
        public void TestGetBucketInformation()
        {
            InitializeGitRepository();
            var driver = CreateAndInitializeDriver();

            var bucket = driver.GetBucketInformation(driver.GetRootIdentifier());

            Assert.AreEqual("a/b", bucket.Name);
            Assert.AreEqual("1.0.0", bucket.Version);
        }

        [TestMethodOnline]
        [DataRow("https://github.com/dotnet/core")]
        public void TestRemoteGit(string repository)
        {
            var driver = CreateAndInitializeDriver(repository);

            Assert.AreEqual("master", driver.GetRootIdentifier());

            CollectionAssert.Contains(driver.GetTags().Keys.ToArray(), "v2.2.5");
            CollectionAssert.Contains(driver.GetBranches().Keys.ToArray(), "master");
        }

        private static Stream ToStream(string content)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private void InitializeGitRepository()
        {
            Assert.AreEqual(0, process.Execute("git init"));
            Assert.AreEqual(0, process.Execute("git config user.email buckettest@example.org"));
            Assert.AreEqual(0, process.Execute("git config user.name BucketTest"));

            fileSystem.Write("foo", ToStream("foo"));

            Assert.AreEqual(0, process.Execute("git add foo"));
            Assert.AreEqual(0, process.Execute("git commit -m init"));

            Assert.AreEqual(0, process.Execute("git tag 0.1.0"));
            Assert.AreEqual(0, process.Execute("git branch oldbranch"));

            var jsonObject = new JObject() { { "name", "a/b" } };
            fileSystem.Write("bucket.json", ToStream(jsonObject.ToString()));
            Assert.AreEqual(0, process.Execute("git add bucket.json"));
            Assert.AreEqual(0, process.Execute("git commit -m addbucket"));
            Assert.AreEqual(0, process.Execute("git tag 0.6.0"));

            // add feature-a branch
            Assert.AreEqual(0, process.Execute("git checkout -b feature/a"));
            fileSystem.Write("foo", ToStream("bar feature"));
            Assert.AreEqual(0, process.Execute("git add foo"));
            Assert.AreEqual(0, process.Execute("git commit -m change-foo"));

            // add version to bucket.json
            Assert.AreEqual(0, process.Execute("git checkout master"));
            jsonObject["version"] = "1.0.0";
            fileSystem.Write("bucket.json", ToStream(jsonObject.ToString()));
            Assert.AreEqual(0, process.Execute("git add bucket.json"));
            Assert.AreEqual(0, process.Execute("git commit -m addversion"));
            Assert.AreEqual(0, process.Execute("git tag 1.0.0"));
            Assert.AreEqual(0, process.Execute("git branch 1.0"));
        }

        private DriverGit CreateAndInitializeDriver(string repository = null)
        {
            var configRepository = new ConfigRepositoryVcs
            {
                Type = "vcs",
                Uri = repository ?? fileSystem.Root,
            };

            var driver = new DriverGit(configRepository, IONull.That, config, process);

            driver.Initialize();

            return driver;
        }
    }
}

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
using Bucket.Repository;
using Bucket.Tester;
using Bucket.Util;
using GameBox.Console.Output;
using GameBox.Console.Process;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryVcs
    {
        private static string root;
        private IProcessExecutor process;
        private BaseFileSystem fileSystem;
        private Config config;

        [TestInitialize]
        public void Initialize()
        {
            root = Helper.GetTestFolder<TestsRepositoryVcs>();
            process = new BucketProcessExecutor() { Cwd = root };
            fileSystem = new FileSystemLocal(root);
            config = new Config();

            fileSystem.Delete();

            Directory.CreateDirectory(root);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                fileSystem.Delete();
            }
            catch (IOException)
            {
                // ignore.
            }
        }

        [TestMethod]
        public void TestLocalVcs()
        {
            InitializeGitRepository();

            var configRepository = new ConfigRepositoryVcs
            {
                Type = "vcs",
                Uri = fileSystem.Root,
            };

            var tester = new TesterIOConsole();
            var io = tester.Mock(AbstractTester.OptionVerbosity(OutputOptions.VerbosityVeryVerbose));
            var repository = new RepositoryVcs(configRepository, io, config);

            var packages = repository.GetPackages();

            CollectionAssert.AreEqual(
                new[]
                {
                    "a/b 1.0.0",
                    "a/b 1.0.x-dev",
                    "a/b dev-feature/a",
                    "a/b dev-master",
                }, Arr.Map(packages, (package) => $"{package.GetName()} {package.GetVersionPretty()}"));
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
    }
}

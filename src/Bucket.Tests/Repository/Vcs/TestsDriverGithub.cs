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
using Bucket.Downloader.Transport;
using Bucket.IO;
using Bucket.Repository.Vcs;
using Bucket.Tests.Support.MockExtension;
using Bucket.Util;
using GameBox.Console.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using static Moq.Mock;

namespace Bucket.Tests.Repository.Vcs
{
    [TestClass]
    public class TestsDriverGithub
    {
        private readonly string repoContent =
@"{
  ""id"": 100,
  ""name"": ""repository"",
  ""private"": false,
  ""owner"": { ""login"": ""owner"" },
  ""has_issues"": true,
  ""default_branch"": ""master""
}";

        private readonly string githubUri = "https://github.com/owner/repository";

        private Mock<Config> config;
        private Mock<IIO> io;
        private Mock<ITransport> transport;
        private Mock<IProcessExecutor> process;
        private string root;
        private DriverGithub driver;

        [TestInitialize]
        public void Initialize()
        {
            config = new Mock<Config>(true, null);
            io = new Mock<IIO>();
            transport = new Mock<ITransport>();
            process = new Mock<IProcessExecutor>();

            root = Helper.GetTestFolder<TestsDriverGithub>();
            config.Setup((o) => o.Get(It.IsIn(Settings.CacheRepoDir), ConfigOptions.None))
                .Returns(root);

            transport.SetupGetString("https://api.github.com/repos/owner/repository", null, () => repoContent)
                .Verifiable();

            driver = new DriverGithub(
                new ConfigRepositoryGithub { Uri = githubUri },
                io.Object, config.Object, transport.Object, process.Object);

            driver.Initialize();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root);
            }
        }

        [TestMethod]
        [DataRow(true, "http://example.com/owner/repository.git")]
        [DataRow(true, "https://example.com/owner/repository")]
        [DataRow(true, "https://example.com/owner/repository.git")]
        [DataRow(true, "git@example.com:/owner/repository.git")]
        [DataRow(true, "git@example.com:owner/repository")]
        [DataRow(false, "git@foo.com:owner/repository")]
        [DataRow(false, "https://foo.com/owner/repository.git")]
        [DataRow(false, "failed uri")]

        public void TestIsSupport(bool expected, string uri)
        {
            config.Setup((o) => o.Get(It.IsIn(Settings.GithubDomains), ConfigOptions.None))
                .Returns(new[] { "example.com" });

            Assert.AreEqual(expected, DriverGithub.IsSupport(io.Object, config.Object, uri));
        }

        [TestMethod]
        public void TestGetSource()
        {
            var resource = driver.GetSource("master");
            Assert.AreEqual("https://github.com/owner/repository.git", resource.Uri);
            Assert.AreEqual("master", resource.Reference);
        }

        [TestMethod]
        public void TestGetDist()
        {
            var resource = driver.GetDist("master");
            Assert.AreEqual("https://api.github.com/repos/owner/repository/zipball/master", resource.Uri);
            Assert.AreEqual("master", resource.Reference);
        }

        [TestMethod]
        public void TestGetRootIdentifier()
        {
            Assert.AreEqual("master", driver.GetRootIdentifier());
        }

        [TestMethod]
        public void TestGetTags()
        {
            var headers = new HttpTestHeaders
            {
                { "Link", @"<https://api.github.com/repositories/100/tags?per_page=100&page=2>; rel=""next"", <https://api.github.com/repositories/100/tags?per_page=100&page=100>; rel=""last""" },
            };

            var tagsContent =
@"[{
    ""name"": ""v1.2.0"",
    ""commit"": { ""sha"": ""abc123"" }
  },
  {
    ""name"": ""v1.0.0"",
    ""commit"": { ""sha"": ""abc456"" }
  }]";
            transport.SetupGetString("https://api.github.com/repos/owner/repository/tags?per_page=100", headers, () => tagsContent);
            transport.SetupGetString("https://api.github.com/repositories/100/tags?per_page=100&page=2", null, () => "[]")
                .Verifiable();

            var tags = driver.GetTags();

            CollectionAssert.AreEqual(
                new[]
                {
                    "v1.2.0:abc123",
                    "v1.0.0:abc456",
                }, Arr.Map(tags, (item) => $"{item.Key}:{item.Value}"));

            Verify(transport);
        }

        [TestMethod]
        public void TestGetBranch()
        {
            var branchesContent =
@"[
  {
    ""ref"": ""refs/heads/feature/foo"",
    ""object"": { ""sha"": ""abc123"" }
  },
  {
    ""ref"": ""refs/heads/master"",
    ""object"": { ""sha"": ""abc456"" }
  },
  {
    ""ref"": ""refs/heads/gh-pages"",
    ""object"": { ""sha"": ""abcefg"" }
  }
]";
            transport.SetupGetString("https://api.github.com/repos/owner/repository/git/refs/heads?per_page=100", null, () => branchesContent);

            var branches = driver.GetBranches();

            CollectionAssert.AreEqual(
                new[]
                {
                    "feature/foo:abc123",
                    "master:abc456",
                }, Arr.Map(branches, (item) => $"{item.Key}:{item.Value}"));

            Verify(transport);
        }

        [TestMethod]
        public void TestGetBucketInformation()
        {
            var bucketContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(@"{ ""name"": ""foo"", ""version"": ""1.0.0"" }"));
            var resourceContent =
@"{
  ""content"": """ + bucketContent + @"\n"",
  ""encoding"": ""base64"",
}";
            transport.SetupGetString("https://api.github.com/repos/owner/repository/contents/bucket.json?ref=master", null, () => resourceContent)
                .Verifiable();

            var commitContent =
@"{
  ""commit"": {
    ""committer"": { ""date"": ""2019-11-26T04:08:30Z"" }
  }
}";
            var branchesContent =
@"[
  {
    ""ref"": ""refs/heads/foo"",
    ""object"": { ""sha"": ""abc456"" }
  },
  {
    ""ref"": ""refs/heads/gh-pages"",
    ""object"": { ""sha"": ""abcefg"" }
  }
]";
            transport.SetupGetString("https://api.github.com/repos/owner/repository/git/refs/heads?per_page=100", null, () => branchesContent)
                .Verifiable();
            transport.SetupGetString("https://api.github.com/repos/owner/repository/commits/master", null, () => commitContent)
                .Verifiable();

            var bucket = driver.GetBucketInformation("master");

            Assert.AreEqual("foo", bucket.Name);
            Assert.AreEqual("1.0.0", bucket.Version);

            var expectedReleaseDate = DateTime.Parse("2019-11-26T04:08:30Z").ToString("yyyy-MM-dd HH:mm:ss");
            Assert.AreEqual(expectedReleaseDate, bucket.ReleaseDate?.ToString("yyyy-MM-dd HH:mm:ss"));
            Assert.AreEqual("https://github.com/owner/repository/issues", bucket.Support["issues"]);
            Assert.AreEqual("https://github.com/owner/repository/tree/master", bucket.Support["source"]);

            Verify(transport);
        }

        private class HttpTestHeaders : HttpHeaders
        {
        }
    }
}

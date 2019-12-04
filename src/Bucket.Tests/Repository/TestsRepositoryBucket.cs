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
using Bucket.Package;
using Bucket.Repository;
using Bucket.Tests.Support.MockExtension;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryBucket
    {
        public static IEnumerable<object[]> ProvideFindPackage()
        {
            return new[]
            {
                new object[]
                {
                    "foo/bar",
                    "^1.0",
                    new List<(string, string)>()
                    {
                        { ("foo/bar", "1.0.0") },
                        { ("foo/bar", "1.2.0") },
                    },
                    new List<(string, string)>()
                    {
                        { ("foo/bar", "1.0.0") },
                        { ("foo/bar", "1.2.0") },
                        { ("foo/baz", "1.6.0") },
                    },
                },
                new object[]
                {
                    "foo/baz",
                    "^1.5",
                    new List<(string, string)>()
                    {
                        { ("foo/baz", "1.6.0") },
                        { ("foo/baz", "1.8.0") },
                    },
                    new List<(string, string)>()
                    {
                        { ("foo/bar", "1.0.0") },
                        { ("foo/baz", "1.4.0") },
                        { ("foo/baz", "1.6.0") },
                        { ("foo/baz", "1.8.0") },
                        { ("bar/baz", "1.2.0") },
                    },
                },
            };
        }

        [TestMethod]
        [DynamicData("ProvideFindPackage", DynamicDataSourceType.Method)]
        public void TestFindPackage(
            string name,
            string version,
            IList<(string packageName, string packageVersion)> expected,
            IList<(string packageName, string packageVersion)> repoPackages)
        {
            var repoConfig = new ConfigRepositoryBucket()
            {
                Uri = "http://example.org",
            };

            var factory = new Mock.MockFactory();
            var transport = new Mock<ITransport>();

            var uid = 1;
            var providers = new Dictionary<string, ConfigMetadata>();
            var packages = new Dictionary<string, ConfigVersions>();
            foreach (var (packageName, packageVersion) in repoPackages)
            {
                if (!packages.TryGetValue(packageName, out ConfigVersions versions))
                {
                    packages[packageName] = versions = new ConfigVersions();
                }

                versions.Add(packageVersion, new ConfigPackageBucket()
                {
                    Uid = uid++,
                    Name = packageName,
                    Version = packageVersion,
                });
            }

            foreach (var item in packages)
            {
                var packageName = item.Key;
                var content = $"{{\"packages\": {{ \"{packageName}\":{JsonConvert.SerializeObject(item.Value)}}}}}";
                var hash = Security.Sha256(content);
                transport.SetupGetString($"http://example.org/provider-packages/{packageName}%24{hash}.json")
                .Returns(content);

                providers.Add(packageName, new ConfigMetadata()
                {
                    { "sha256", Security.Sha256(content) },
                });
            }

            {
                var content = $"{{\"providers\":{JsonConvert.SerializeObject(providers)}}}";
                var hash = Security.Sha256(content);
                transport.SetupGetString($"http://example.org/provider-latest%24{hash}.json")
                    .Returns(content);

                transport.SetupGetString("http://example.org/packages.json")
                    .Returns(
    @"{ 
      ""providers-url"":""/provider-packages/%package%$%hash%.json"",
      ""provider-includes"":{
          ""/provider-latest$%hash%.json"": { ""sha256"": """ + hash + @"""  }
      }
}");
            }

            var repository = new Mock<RepositoryBucket>(
                repoConfig,
                IONull.That,
                factory.CreateConfig(),
                transport.Object,
                null,
                null)
            { CallBase = true };

            var expectedPackages = new List<IPackage>();
            foreach (var (packageName, packageVersion) in expected)
            {
                expectedPackages.Add(Helper.GetPackage<IPackage>(packageName, packageVersion));
            }

            Assert.AreEqual(
                expectedPackages[0].ToString(),
                repository.Object.FindPackage(name, version).ToString());

            CollectionAssert.AreEqual(
                Arr.Map(expectedPackages.ToArray(), (o) => o.ToString()),
                Arr.Map(repository.Object.FindPackages(name, version), (o) => o.ToString()));
        }

        [TestMethod]
        public void TestSearchWithType()
        {
            var repoConfig = new ConfigRepositoryBucket()
            {
                Uri = "http://example.org",
            };

            var transport = new Mock<ITransport>();
            transport.SetupGetString("http://example.org/packages.json")
                   .Returns("{\"search\":\"/search.json?q=%query%&type=%type%\"}");

            var ret = new ConfigSearchResults
            {
                Results = new List<ConfigSearchResult>
                {
                    new ConfigSearchResult { Name = "foo" },
                },
            };

            transport.SetupGetString("http://example.org/search.json?q=foo&type=library")
                   .Returns(JsonConvert.SerializeObject(ret));

            transport.SetupGetString("http://example.org/search.json?q=foo&type=invalid")
                   .Returns("{}");

            var factory = new Mock.MockFactory();
            var repository = new RepositoryBucket(
                repoConfig,
                IONull.That,
                factory.CreateConfig(),
                transport.Object);

            var searched = repository.Search("foo", SearchMode.Fulltext, "library");
            CollectionAssert.AreEqual(new[] { "foo" }, Arr.Map(searched, (o) => o.ToString()));

            searched = repository.Search("foo", SearchMode.Fulltext, "invalid");
            CollectionAssert.AreEqual(Array.Empty<SearchResult>(), searched);
        }

        [TestMethod]
        [DataRow("https://example.org/path/to/file", "https://example.org", "/path/to/file")]
        [DataRow("https://example.org/normalize_uri", "https://should-not-see-me.test", "https://example.org/normalize_uri")]
        [DataRow("file:///path/to/repository/file", "file:///path/to/repository", "/path/to/repository/file")]
        [DataRow("https://example.org/path/to/unusual_${0}_filename", "https://example.org", "/path/to/unusual_${0}_filename")]
        public void TestNormalizeUri(string expected, string baseUri, string uri)
        {
            var repoConfig = new ConfigRepositoryBucket()
            {
                Uri = baseUri,
            };

            var factory = new Mock.MockFactory();
            var repository = new RepositoryBucket(
                repoConfig,
                IONull.That,
                factory.CreateConfig());

            Assert.AreEqual(expected, repository.NormalizeUri(uri));
        }

        private sealed class ConfigMetadata : Dictionary<string, string>
        {
        }

        private sealed class ConfigVersions : Dictionary<string, ConfigPackageBucket>
        {
        }

        private sealed class ConfigPackageBucket : ConfigBucketBase
        {
            [JsonProperty("uid")]
            public int Uid { get; set; }

            /// <inheritdoc />
            public override bool ShouldDeserializeSource()
            {
                return true;
            }

            /// <inheritdoc />
            public override bool ShouldDeserializeDist()
            {
                return true;
            }

            /// <inheritdoc />
            public override bool ShouldSerializeSource()
            {
                return true;
            }

            /// <inheritdoc />
            public override bool ShouldSerializeDist()
            {
                return true;
            }
        }

        private sealed class ConfigSearchResult : ConfigBucketBase
        {
            [JsonProperty("virtual")]
            public bool Virtual { get; set; }
        }

        private sealed class ConfigSearchResults
        {
            [JsonProperty("results")]
            public IList<ConfigSearchResult> Results { get; set; }

            [JsonProperty("total")]
            public int Total { get; set; }

            [JsonProperty("next")]
            public string Next { get; set; }
        }
    }
}

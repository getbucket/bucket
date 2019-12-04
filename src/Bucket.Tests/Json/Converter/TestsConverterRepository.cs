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

#pragma warning disable CA1034
#pragma warning disable CA1819

using Bucket.Configuration;
using Bucket.Json.Converter;
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Bucket.Tests.Json.Converter
{
    [TestClass]
    public class TestsConverterRepository
    {
        [TestMethod]
        [DataFixture("repository-vcs-1.json")]
        public void TestRepositoryVcs(Foo foo)
        {
            Assert.AreEqual(1, foo.Repositories.Length);
            Assert.AreEqual("foo", foo.Repositories[0].Name);
            Assert.AreEqual("git", foo.Repositories[0].Type);

            var repository = (ConfigRepositoryVcs)foo.Repositories[0];
            Assert.AreEqual("https://example.com/", repository.Uri);
            Assert.AreEqual(false, repository.SecureHttp);
        }

        [TestMethod]
        [DataFixture("repository-bucket-1.json")]
        public void TestRepositoryBucket(Foo foo)
        {
            Assert.AreEqual(1, foo.Repositories.Length);
            Assert.AreEqual("foo", foo.Repositories[0].Name);
            Assert.AreEqual("bucket", foo.Repositories[0].Type);

            var repository = (ConfigRepositoryBucket)foo.Repositories[0];
            Assert.AreEqual("https://example.com/", repository.Uri);
        }

        [TestMethod]
        [DataFixture("repository-mixture-1.json")]
        public void TestRepositoryMultMixture(Foo foo)
        {
            Assert.AreEqual(2, foo.Repositories.Length);
            Assert.AreEqual("foo", foo.Repositories[0].Name);
            Assert.AreEqual("git", foo.Repositories[0].Type);

            var repositoryVcs = (ConfigRepositoryVcs)foo.Repositories[0];
            Assert.AreEqual("https://example.com/", repositoryVcs.Uri);
            Assert.AreEqual(false, repositoryVcs.SecureHttp);

            Assert.AreEqual("foo", foo.Repositories[1].Name);
            Assert.AreEqual("bucket", foo.Repositories[1].Type);

            var repositoryBucket = (ConfigRepositoryBucket)foo.Repositories[1];
            Assert.AreEqual("https://example.com/", repositoryBucket.Uri);
        }

        [TestMethod]
        [DataFixture("repository-vcs-1.json")]
        public void TestRepositoryVcsWrite(string expected)
        {
            var foo = new Foo
            {
                Repositories = new[]
                {
                    new ConfigRepositoryVcs
                    {
                        Name = "foo",
                        Type = "git",
                        Uri = "https://example.com/",
                        SecureHttp = false,
                    },
                },
            };

            Assert.AreEqual(expected, JsonConvert.SerializeObject(foo, Formatting.Indented));
        }

        [JsonObject]
        public sealed class Foo
        {
            [JsonProperty("repositories")]
            [JsonConverter(typeof(ConverterRepository))]
            public ConfigRepository[] Repositories { get; set; }
        }
    }
}

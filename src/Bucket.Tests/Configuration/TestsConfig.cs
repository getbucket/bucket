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
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Bucket.Tests.Configuration
{
    [TestClass]
    public class TestsConfig
    {
        [TestMethod]
        public void TestDefaultConfig()
        {
            var config = new Config();
            var home = Helper.GetHome();

            Assert.AreEqual($"{home}/cache", (string)config.Get(Settings.CacheDir));
            Assert.AreEqual($"{home}/cache/repo", (string)config.Get(Settings.CacheRepoDir));
            Assert.AreEqual($"{home}/cache/vcs", (string)config.Get(Settings.CacheVcsDir));
            Assert.IsTrue(config.Get(Settings.SecureHttp));
            CollectionAssert.AreEqual(new[] { "https", "ssh" }, (string[])config.Get(Settings.GithubProtocols));
            CollectionAssert.AreEqual(new[] { "github.com" }, (string[])config.Get(Settings.GithubDomains));
            CollectionAssert.AreEqual(new[] { "gitlab.com" }, (string[])config.Get(Settings.GitlabDomains));
            Assert.AreEqual("prompt", (string)config.Get(Settings.StoreAuth));
            Assert.AreEqual("false", (string)config.Get(Settings.DiscardChanges));
            Assert.AreEqual("auto", (string)config.Get(Settings.PreferredInstall));
            Assert.AreEqual($"vendor", (string)config.Get(Settings.VendorDir));
            Assert.AreEqual($"vendor/bin", (string)config.Get(Settings.BinDir));
            Assert.AreEqual($"auto", (string)config.Get(Settings.BinCompat));
            Assert.AreEqual(15552000, (int)config.Get(Settings.CacheTTL));
            Assert.AreEqual(15552000, (int)config.Get(Settings.CacheFilesTTL));
            Assert.AreEqual(300 * 1024 * 1024, (int)config.Get(Settings.CacheFilesMaxSize));
        }

        [TestMethod]
        [DataFixture("config-get-merge.json")]
        public void TestGetWithMerge(JObject json)
        {
            var config = new Config();
            config.Merge(json);

            StringAssert.Contains(config.Get(Settings.Home), "foo");
            Assert.AreEqual("cache/dir", (string)config.Get(Settings.CacheDir));
            Assert.AreEqual("cache/dir/repo", (string)config.Get(Settings.CacheRepoDir));
            Assert.AreEqual("cache/dir/vcs", (string)config.Get(Settings.CacheVcsDir));
            Assert.IsFalse(config.Get(Settings.SecureHttp));
            CollectionAssert.AreEqual(new[] { "git", "ssh" }, (string[])config.Get(Settings.GithubProtocols));
            CollectionAssert.AreEqual(new[] { "github.com" }, (string[])config.Get(Settings.GithubDomains));
            CollectionAssert.AreEqual(new[] { "gitlab.com" }, (string[])config.Get(Settings.GitlabDomains));
            Assert.AreEqual("prompt", (string)config.Get(Settings.StoreAuth));
            Assert.AreEqual("true", (string)config.Get(Settings.DiscardChanges));
            Assert.AreEqual("source", (string)config.Get(Settings.PreferredInstall));
            Assert.AreEqual($"foo-vendor", (string)config.Get(Settings.VendorDir));
            Assert.AreEqual($"foo-bin", (string)config.Get(Settings.BinDir));
            Assert.AreEqual($"full", (string)config.Get(Settings.BinCompat));
            Assert.AreEqual(15552000, (int)config.Get(Settings.CacheTTL));
            Assert.AreEqual(100, (int)config.Get(Settings.CacheFilesTTL));
            Assert.AreEqual(200 * 1024, (int)config.Get(Settings.CacheFilesMaxSize));
        }

        [TestMethod]
        [DataFixture("config-get-variant.json")]
        public void TestTypeVariant(JObject json)
        {
            var config = new Config();
            config.Merge(json);

            Assert.AreEqual(false, (bool)config.Get(Settings.SecureHttp));
            CollectionAssert.AreEqual(new[] { "git" }, (string[])config.Get(Settings.GithubProtocols));
            CollectionAssert.AreEqual(new[] { "github.com" }, (string[])config.Get(Settings.GithubDomains));
            CollectionAssert.AreEqual(new[] { "gitlab.com" }, (string[])config.Get(Settings.GitlabDomains));
            Assert.AreEqual("true", (string)config.Get(Settings.StoreAuth));
            Assert.AreEqual("stash", (string)config.Get(Settings.DiscardChanges));

            var preferred = config.Get<ConfigPreferred>(Settings.PreferredInstall);
            Assert.IsTrue(preferred.ContainsKey("foo/bar"));
            Assert.IsTrue(preferred.ContainsKey("foo/*"));
        }

        [TestMethod]
        [DataRow(Settings.SecureHttp, null)]
        [DataRow(Settings.GithubProtocols, null)]
        [DataRow(Settings.GithubDomains, null)]
        [DataRow(Settings.GitlabDomains, null)]
        [DataRow(Settings.StoreAuth, null)]
        [DataRow(Settings.BinCompat, null)]
        [DataRow(Settings.CacheFilesMaxSize, null)]
        public void TestIllegalValue(string field, Type expectedException)
        {
            expectedException = expectedException ?? typeof(ConfigException);
            var config = new Config();
            config.Merge(LoadJson("config-get-illegal.json"));

            try
            {
                config.Get(field);
                Assert.Fail($"Illegal field \"{field}\" need throw exception: {expectedException}");
            }
            catch (System.Exception ex) when (ex.GetType() == expectedException)
            {
                // ignore.
            }
        }

        [TestMethod]
        public void TestConfigNotFound()
        {
            var config = new Config();
            Assert.AreEqual(null, config.Get("foo"));
            Assert.AreEqual(null, config.Get<ConfigAuth>("foo"));
        }

        [TestMethod]
        [DataFixture("config-get-automatic.json")]
        public void TestAutomaticConfig(JObject json)
        {
            var config = new Config();
            config.Merge(json);

            Assert.AreEqual(100, (int)config.Get("int-value"));
            Assert.AreEqual("foo", (string)config.Get("string-value"));
            Assert.AreEqual("foo/bar", (string)config.Get("string-value-variable"));
            Assert.AreEqual(true, (bool)config.Get("bool-value"));
            Assert.AreEqual(0.123456f, (float)config.Get("float-value"));
            CollectionAssert.AreEqual(new[] { "foo", "bar" }, (string[])config.Get("array-string-value"));
        }

        [TestMethod]
        [ExpectedException(typeof(ConfigException))]
        [DataRow(new[] { "git" })]
        [DataRow(new[] { "git", "http" })]
        public void TestGetGithubProtocolsWithSecureHttp(string[] protocols)
        {
            var config = new Config();
            var merged = new JObject() { { "config", new JObject() } };
            merged["config"][Settings.SecureHttp] = true;
            merged["config"][Settings.GithubProtocols] = new JArray(protocols);
            config.Merge(merged);

            config.Get(Settings.GithubProtocols);
        }

        [TestMethod]
        public void TestVariableReplacement()
        {
            var config = new Config(false);
            var merged = new JObject() { { "config", new JObject() } };
            merged["config"][Settings.CacheDir] = "~/foo/";
            merged["config"][Settings.CacheRepoDir] = "$HOME";
            merged["config"]["foo"] = "foo";
            merged["config"]["bar"] = "{$foo}";
            config.Merge(merged);

            var home = Helper.GetHome();
            Assert.AreEqual($"{home}", (string)config.Get(Settings.CacheRepoDir));
            Assert.AreEqual($"{home}/foo", (string)config.Get(Settings.CacheDir));
        }

        [TestMethod]
        public void TestRealpathReplacement()
        {
            var config = new Config(false, "/foo/bar");
            var merged = new JObject() { { "config", new JObject() } };
            merged["config"][Settings.CacheDir] = "/baz/";
            merged["config"][Settings.CacheRepoDir] = "$HOME/foo";
            merged["config"][Settings.CacheVcsDir] = "vcs";
            config.Merge(merged);

            var home = Helper.GetHome();

            Assert.AreEqual($"{home}/foo", (string)config.Get(Settings.CacheRepoDir));
            Assert.AreEqual($"/baz", (string)config.Get(Settings.CacheDir));
            Assert.AreEqual("/foo/bar/vcs", (string)config.Get(Settings.CacheVcsDir));
            Assert.AreEqual($"vcs", (string)config.Get(Settings.CacheVcsDir, ConfigOptions.RelativePath));
        }

        [TestMethod]
        public void TestDefaultRepository()
        {
            var config = new Config();

            Assert.AreEqual(1, config.GetRepositories().Length);

            var repository = (ConfigRepositoryBucket)config.GetRepositories()[0];
            Assert.AreEqual("bucket", repository.Type);
            Assert.AreEqual(Config.DefaultRepositoryUri, repository.Uri);
            Assert.AreEqual(true, repository.AllowSSLDowngrade);
        }

        [TestMethod]
        public void TestAddRepository()
        {
            var config = new Config();
            var repositories = new JArray(
                new JObject()
                {
                    { "type", "vcs" },
                    { "url", "git://github.com/foo/bar" },
                },
                new JObject()
                {
                    { "type", "vcs" },
                    { "url", "git://github.com/baz/boo" },
                    { "secure-http", false },
                });

            config.Merge(new JObject() { { "repositories", repositories } });

            Assert.AreEqual(3, config.GetRepositories().Length);

            var repository = (ConfigRepositoryVcs)config.GetRepositories()[0];
            Assert.AreEqual("vcs", repository.Type);
            Assert.AreEqual("git://github.com/foo/bar", repository.Uri);
            Assert.AreEqual(true, repository.SecureHttp);

            repository = (ConfigRepositoryVcs)config.GetRepositories()[1];
            Assert.AreEqual("vcs", repository.Type);
            Assert.AreEqual("git://github.com/baz/boo", repository.Uri);
            Assert.AreEqual(false, repository.SecureHttp);

            var baseRepository = config.GetRepositories()[2];
            Assert.AreEqual("bucket", baseRepository.Type);
        }

        [TestMethod]
        [DataRow(true, Settings.CacheDir)]
        [DataRow(false, "setting-not-found")]
        public void TestHas(bool expected, string field)
        {
            var config = new Config();
            Assert.AreEqual(expected, config.Has(field));
        }

        [TestMethod]
        public void TestNotChangeOriginalObject()
        {
            var config = new Config();
            JObject cloned = config;
            cloned[Settings.CacheDir] = "foo";

            StringAssert.Contains(config.Get(Settings.CacheDir), "/cache");
        }

        [TestMethod]
        [DataFixture("config-get-complex.json")]
        public void TestGetComplex(JObject json)
        {
            var config = new Config();
            config.Merge(json);

            var auth = config.Get<ConfigAuth>(Settings.GithubOAuth);
            Assert.AreEqual("foo", auth["github.com"]);
            Assert.AreEqual("bar", auth["private.github.com"]);

            auth = config.Get<ConfigAuth>(Settings.GitlabOAuth);
            Assert.AreEqual("foo", auth["gitlab.com"]);
            Assert.AreEqual("bar", auth["private.gitlab.com"]);

            auth = config.Get<ConfigAuth>(Settings.GitlabToken);
            Assert.AreEqual("foo", auth["gitlab.com"]);
            Assert.IsFalse(auth.ContainsKey("private.gitlab.com"));

            var basicHttp = config.Get<ConfigAuth<HttpBasic>>(Settings.HttpBasic);
            Assert.IsTrue(basicHttp.ContainsKey("example.org"));
            Assert.AreEqual("foo", basicHttp["example.org"].Username);
            Assert.AreEqual("bar", basicHttp["example.org"].Password);
        }

        private JObject LoadJson(string filename)
        {
            var filePath = Helper.Fixtrue($"Configuration/{filename}");
            return JObject.Parse(File.ReadAllText(filePath));
        }
    }
}

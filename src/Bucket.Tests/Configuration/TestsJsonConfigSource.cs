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
using Bucket.Json;
using Bucket.Package;
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;

namespace Bucket.Tests.Configuration
{
    [TestClass]
    public class TestsJsonConfigSource
    {
        private IConfigSource source;
        private TesterJsonFile jsonFile;

        [TestInitialize]
        public void Init()
        {
            jsonFile = new TesterJsonFile();
            source = new JsonConfigSource(jsonFile);
        }

        [TestMethod]
        [DataFixture("json-source-add-property-1.json")]
        public void TestAddProperty(string expected)
        {
            source.AddProperty("name", "foo");
            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        [TestMethod]
        [DataFixture("json-source-add-property-2.json")]
        public void TestAddPropertyReplacement(string expected)
        {
            source.AddProperty("name", "foo");
            source.AddProperty("name", "bar");
            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        [TestMethod]
        [DataFixture("json-source-remove-property-1.json")]
        public void TestRemoveProperty(string expected)
        {
            source.AddProperty("name", "foo");
            source.AddProperty("version", "1.0.0");
            source.RemoveProperty("version");

            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        [TestMethod]
        [DataFixture("json-source-add-repository-1.json")]
        public void TestAddRepository(string expected)
        {
            var repository = new ConfigRepositoryVcs()
            {
                Name = "foo",
                Type = "vcs",
                Uri = "http://foo.com",
                SecureHttp = false,
            };

            source.AddRepository(repository);
            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        [TestMethod]
        [DataFixture("json-source-add-repository-2.json")]
        public void TestAddRepositoryWithExists(string expected)
        {
            var foo = new ConfigRepositoryVcs()
            {
                Name = "foo",
                Type = "vcs",
                Uri = "http://foo.com",
                SecureHttp = false,
            };
            source.AddRepository(foo);

            var bar = new ConfigRepositoryBucket()
            {
                Type = "bucket",
                Uri = "http://foo.com",
                AllowSSLDowngrade = false,
            };
            source.AddRepository(bar);

            foo.Uri = "http://bar.com";
            source.AddRepository(foo);

            Assert.AreEqual(expected, jsonFile.GetWriteContents(), "Cannot modify the order of the repository.");
        }

        [TestMethod]
        [DataFixture("json-source-remove-repository-1.json")]
        public void TestRemoveRepository(string expected)
        {
            var foo = new ConfigRepositoryVcs()
            {
                Name = "foo",
                Type = "vcs",
                Uri = "http://foo.com",
                SecureHttp = false,
            };
            source.AddRepository(foo);

            var bar = new ConfigRepositoryBucket()
            {
                Type = "bucket",
                Uri = "http://foo.com",
                AllowSSLDowngrade = false,
            };
            source.AddRepository(bar);

            source.RemoveRepository("foo");

            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        [TestMethod]
        [DataFixture("json-source-add-link-1.json")]
        public void TestAddLink(string expected)
        {
            source.AddLink(LinkType.Require, "foo", "1.0.0");
            source.AddLink(LinkType.RequireDev, "foo-dev", "1.0.0");
            source.AddLink(LinkType.Replace, "foo-replace", "1.0.0");
            source.AddLink(LinkType.Conflict, "foo-conflict", "1.0.0");
            source.AddLink(LinkType.Provide, "foo-provide", "1.0.0");

            source.AddLink(LinkType.Require, "bar", "1.0.0");
            source.AddLink(LinkType.Require, "bar", "2.0.0");

            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        [TestMethod]
        [DataFixture("json-source-remove-link-1.json")]
        public void TestRemoveLink(string expected)
        {
            source.AddLink(LinkType.Require, "foo", "1.0.0");
            source.AddLink(LinkType.Require, "bar", "2.0.0");
            source.AddLink(LinkType.Replace, "foo-replace", "1.0.0");
            source.AddLink(LinkType.Conflict, "foo-conflict", "1.0.0");
            source.AddLink(LinkType.Provide, "foo-provide", "1.0.0");

            source.RemoveLink(LinkType.Replace, "foo-replace");
            source.RemoveLink(LinkType.Require, "bar");

            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        [TestMethod]
        [DataFixture("json-source-add-config-setting-1.json")]
        public void TestAddConfigSetting(string expected)
        {
            source.AddConfigSetting(Settings.CacheDir, "foo");
            source.AddConfigSetting(Settings.VendorDir, "bar");

            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        [TestMethod]
        [DataFixture("json-source-remove-config-setting-1.json")]
        public void TestRemoveConfigSetting(string expected)
        {
            source.AddConfigSetting(Settings.CacheDir, "foo");
            source.AddConfigSetting(Settings.VendorDir, "bar");

            source.RemoveConfigSetting(Settings.VendorDir);
            Assert.AreEqual(expected, jsonFile.GetWriteContents());
        }

        private sealed class TesterJsonFile : JsonFile
        {
            private string contents;
            private string json;

            public TesterJsonFile()
                : base(string.Empty)
            {
            }

            public override JObject Read()
            {
                return JObject.Parse(json);
            }

            public override bool Exists()
            {
                return !string.IsNullOrEmpty(json);
            }

            public override void Write(object content)
            {
                if (!(content is JObject data))
                {
                    data = JObject.FromObject(content);
                }

                using (StringWriter streamWriter = new StringWriter(CultureInfo.InvariantCulture))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(streamWriter)
                    {
                        Formatting = Formatting.Indented,
                        Indentation = 4,
                    })
                    {
                        data.WriteTo(writer);
                        json = this.contents = streamWriter.ToString();
                    }
                }
            }

            public string GetWriteContents()
            {
                return contents;
            }

            public void SetJson(string json)
            {
                this.json = json;
            }
        }
    }
}

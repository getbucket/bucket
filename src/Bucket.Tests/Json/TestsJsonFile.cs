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

#pragma warning disable CA1812

using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Bucket.Tests.Json
{
    [TestClass]
    public class TestsJsonFile
    {
        [TestMethod]
        public void TestRead()
        {
            var mockIO = new Mock<IIO>();
            var filePath = Helper.Fixtrue("Json/read-1.json");
            var jsonFile = new JsonFile(filePath, null, mockIO.Object);
            var jObject = jsonFile.Read();

            Assert.AreEqual("menghanyu", jObject["name"]);
            Assert.AreEqual("miaomiao", jObject["aliases"][0]);
            Assert.AreEqual("candycat", jObject["aliases"][1]);

            mockIO.Verify((o) => o.WriteError($"JsonFile reading: {filePath}", true, Verbosities.Debug));
        }

        [TestMethod]
        public void TestWrite()
        {
            var mockIO = new Mock<IIO>();
            var mockFileSystem = new Mock<IFileSystem>();
            var jsonFile = new JsonFile(string.Empty, mockFileSystem.Object, mockIO.Object);

            string actual = null;
            mockFileSystem.Setup((o) => o.Write(It.IsAny<string>(), It.IsAny<Stream>(), false))
                .Callback((string path, Stream stream, bool append) =>
            {
                actual = stream.ToText();
            });

            var data = new JObject()
            {
                { "foo", "bar" },
            };

            jsonFile.Write(data);

            Assert.AreEqual(
@"{
    ""foo"": ""bar""
}", actual);
        }

        [TestMethod]
        public void TestDelete()
        {
            var mockFileSystem = new Mock<IFileSystem>();
            var jsonFile = new JsonFile("foo/bar", mockFileSystem.Object, null);
            jsonFile.Delete();
            mockFileSystem.Verify((o) => o.Delete("foo/bar"));
        }

        [TestMethod]
        public void TestReadFromRemoteFileSystem()
        {
            var mockIO = new Mock<IIO>();
            var mockFileSystem = new Mock<IFileSystem>();
            var filePath = Helper.Fixtrue("Json/read-2.json");
            mockFileSystem.Setup((o) => o.Read(filePath))
                .Returns(() => File.OpenRead(filePath));

            var jsonFile = new JsonFile(filePath, mockFileSystem.Object, mockIO.Object);
            var jObject = jsonFile.Read();

            Assert.AreEqual("foo", jObject["name"]);
            Assert.AreEqual("bar", jObject["aliases"][0]);
            Assert.AreEqual("baz", jObject["aliases"][1]);
        }

        [TestMethod]
        public void TestExists()
        {
            var filePath = Helper.Fixtrue("Json/read-1.json");
            var jsonFile = new JsonFile(filePath);

            Assert.AreEqual(true, jsonFile.Exists());
        }

        [TestMethod]
        public void TestGetPath()
        {
            var filePath = Helper.Fixtrue("Json/read-1.json");
            var jsonFile = new JsonFile(filePath);

            StringAssert.Contains(jsonFile.GetPath(), "Json/read-1.json");
        }

        [TestMethod]
        public void TestParseObject()
        {
            var filePath = Helper.Fixtrue("Json/read-1.json");
            var content = File.ReadAllText(filePath);

            var foo = JsonFile.Parse<Read1>(content);

            Assert.AreEqual("menghanyu", foo.Name);
            Assert.AreEqual("miaomiao", foo.Aliases[0]);
            Assert.AreEqual("candycat", foo.Aliases[1]);
        }

        private sealed class Read1
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("aliases")]
            public string[] Aliases { get; set; }
        }
    }
}

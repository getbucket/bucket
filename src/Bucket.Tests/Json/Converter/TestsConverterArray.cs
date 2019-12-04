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

using Bucket.Json.Converter;
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

namespace Bucket.Tests.Json.Converter
{
    [TestClass]
    public class TestsConverterArray
    {
        [TestMethod]
        [DataFixture("read-1.json")]
        public void TestRead(Foo foo)
        {
            Assert.AreEqual(1, foo.Bar.Length);
            Assert.AreEqual("bar", foo.Bar[0]);
        }

        [TestMethod]
        [DataFixture("read-2.json")]
        public void TestReadArray(Foo foo)
        {
            Assert.AreEqual(2, foo.Bar.Length);
            Assert.AreEqual("bar", foo.Bar[0]);
            Assert.AreEqual("foo", foo.Bar[1]);
        }

        [TestMethod]
        [DataFixture("read-3.json")]
        public void TestReadNull(Foo foo)
        {
            Assert.AreEqual(null, foo.Bar);
        }

        [TestMethod]
        [DataFixture("read-1.json")]
        public void TestWrite(string expected)
        {
            var foo = new Foo
            {
                Bar = new[] { "bar" },
            };

            Assert.AreEqual(expected, JsonConvert.SerializeObject(foo));
        }

        [TestMethod]
        [DataFixture("read-2.json")]
        public void TestWriteArray(string expected)
        {
            var foo = new Foo
            {
                Bar = new[] { "bar", "foo" },
            };

            Assert.AreEqual(expected, JsonConvert.SerializeObject(foo));
        }

        [TestMethod]
        [DataFixture("read-3.json")]
        public void TestWriteNull(string expected)
        {
            var foo = new Foo
            {
                Bar = null,
            };

            Assert.AreEqual(expected, JsonConvert.SerializeObject(foo));
        }

        [TestMethod]
        public void TestWriteEmpty()
        {
            var foo = new Foo
            {
                Bar = Array.Empty<string>(),
            };

            Assert.AreEqual("{\"bar\":[]}", JsonConvert.SerializeObject(foo));
        }

        [JsonObject]
        public sealed class Foo
        {
            [JsonProperty("bar")]
            [JsonConverter(typeof(ConverterArray))]
            public string[] Bar { get; set; }
        }
    }
}

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

using Bucket.Json.Converter;
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Bucket.Tests.Json.Converter
{
    [TestClass]
    public class TestsConverterDeprecated
    {
        [TestMethod]
        [DataFixture("deprecated-true.json", true)]
        [DataFixture("deprecated-true-string.json", "true")]
        [DataFixture("deprecated-string.json", "vendor/foo")]
        public void TestValueDeserialization(Foo foo, object expected)
        {
            Assert.AreEqual(expected, foo.Deprecated);
        }

        [TestMethod]
        [DataFixture("deprecated-false.json")]
        public void TestBoolFalseDeserialization(Foo foo)
        {
            Assert.AreEqual(false, foo.Deprecated);
        }

        [TestMethod]
        [DataFixture("deprecated-true-string.json")]
        public void TestBoolTrueSerialization(string expected)
        {
            var foo = new Foo() { Deprecated = "true" };
            Assert.AreEqual(expected, JsonConvert.SerializeObject(foo));
        }

        [TestMethod]
        [DataFixture("deprecated-string.json")]
        public void TestReplacementSerialization(string expected)
        {
            var foo = new Foo() { Deprecated = "vendor/foo" };
            Assert.AreEqual(expected, JsonConvert.SerializeObject(foo));
        }

        [TestMethod]
        public void TestSerialization()
        {
            var foo = new Foo() { Deprecated = "false" };
            Assert.AreEqual("{\"deprecated\":\"false\"}", JsonConvert.SerializeObject(foo));

            foo = new Foo() { Deprecated = null };
            Assert.AreEqual("{}", JsonConvert.SerializeObject(foo));
        }

        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        public sealed class Foo
        {
            [JsonProperty("deprecated")]
            [JsonConverter(typeof(ConverterDeprecated))]
            public dynamic Deprecated { get; set; }
        }
    }
}

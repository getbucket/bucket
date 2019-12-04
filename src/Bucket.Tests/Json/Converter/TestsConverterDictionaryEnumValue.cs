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

#pragma warning disable CA2227
#pragma warning disable CA1034
#pragma warning disable SA1602

using Bucket.Json.Converter;
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Bucket.Tests.Json.Converter
{
    [TestClass]
    public class TestsConverterDictionaryEnumValue
    {
        [TestMethod]
        [DataFixture("dictionary-enum-1.json")]
        public void TestRead(Foo foo)
        {
            Assert.IsTrue(foo.Map.ContainsKey("foo"));
            Assert.IsTrue(foo.Map.ContainsKey("bar"));
            Assert.AreEqual(Auux.Foo, foo.Map["foo"]);
            Assert.AreEqual(Auux.Bar, foo.Map["bar"]);
        }

        [TestMethod]
        public void TestEmptyContent()
        {
            var foo = JsonConvert.DeserializeObject<Foo>("{}");
            Assert.AreEqual(null, foo.Map);
        }

        [TestMethod]
        public void TestWrite()
        {
            var foo = new Foo
            {
                Map = new Dictionary<string, Auux>
                {
                    { "foo", Auux.Foo },
                    { "bar", Auux.Bar },
                },
            };

            Assert.AreEqual(@"{""map"":{""foo"":""faz"",""bar"":""baz""}}", JsonConvert.SerializeObject(foo));
        }

#pragma warning disable SA1201
        public enum Auux
#pragma warning restore SA1201
        {
            [EnumMember(Value = "faz")]
            Foo,
            [EnumMember(Value = "baz")]
            Bar,
        }

        [JsonObject]
        public sealed class Foo
        {
            [JsonProperty("map")]
            [JsonConverter(typeof(ConverterDictionaryEnumValue<Auux>))]
            public IDictionary<string, Auux> Map { get; set; } = null;
        }
    }
}

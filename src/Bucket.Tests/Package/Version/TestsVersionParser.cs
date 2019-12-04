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

using Bucket.Package.Version;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Package.Version
{
    [TestClass]
    public class TestsVersionParser
    {
        [TestMethod]
        [DataRow(true, "0.1.0.0", "0.2.0.0")]
        [DataRow(true, "1.2.0.0", "2.2.0.0")]
        [DataRow(false, "1.2.0.0", "0.2.0.0")]
        public void TestIsUpgrade(bool expected, string from, string to)
        {
            Assert.AreEqual(expected, VersionParser.IsUpgrade(from, to));
        }

        [TestMethod]
        [DataRow("foo", "^7.0", new[] { "foo@^7.0" })]
        [DataRow("foo", "7.0", new[] { "foo:7.0" })]
        [DataRow("foo", "^7.0", new[] { "foo", "^7.0" })]
        [DataRow("foo", "version", new[] { "foo", "version" })]
        public void TestParseNameVersionPairs(string expectedName, string expectedVersion, string[] input)
        {
            var versionParser = new VersionParser();
            var result = versionParser.ParseNameVersionPairs(input);

            Assert.AreEqual(1, result.Length);

            var (actualName, actualVersion) = result[0];
            Assert.AreEqual(expectedName, actualName);
            Assert.AreEqual(expectedVersion, actualVersion);
        }

        [TestMethod]
        public void TestParseNameVersionMultPackages()
        {
            var versionParser = new VersionParser();
            var result = versionParser.ParseNameVersionPairs(new[] { "foo", "vendor/bar" });

            Assert.AreEqual(2, result.Length);

            var (actualName, actualVersion) = result[0];
            Assert.AreEqual("foo", actualName);
            Assert.AreEqual(null, actualVersion);

            (actualName, actualVersion) = result[1];
            Assert.AreEqual("vendor/bar", actualName);
            Assert.AreEqual(null, actualVersion);
        }
    }
}

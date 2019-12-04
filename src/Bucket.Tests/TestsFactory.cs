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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace Bucket.Tests
{
    [TestClass]
    public class TestsFactory
    {
        [TestMethod]
        [DataRow(null, null, "0foo")]
        [DataRow(null, "foobar", "foobar")]
        [DataRow(null, "FooBar", "FooBar")]
        [DataRow(null, "foo.bar", "foo.bar")]
        [DataRow(null, "foo-bar", "foo-bar")]
        [DataRow(null, "foo_bar", "foo_bar")]
        [DataRow(null, null, "0foo/bar")]
        [DataRow(null, null, "foo/0bar")]
        [DataRow(null, null, "foo/bar/baz")]
        [DataRow(null, null, "/baz")]
        [DataRow("foo", "bar", "foo/bar")]
        [DataRow("foo-0", "bar", "foo-0/bar")]
        [DataRow("foo_0_1", "bar", "foo_0_1/bar")]
        [DataRow("foo_0_1", "bar.Baz", "foo_0_1/bar.Baz")]
        [DataRow("a", "b", "a/b")]
        [DataRow("ab", "c", "ab/c")]
        [DataRow("ab", "cd", "ab/cd")]
        [DataRow("a", "bc", "a/bc")]
        public void TestRegexPackageName(string expectedProvide, string expectedPackageName, string packageName)
        {
            var mathed = Regex.Match(packageName, Factory.RegexPackageName, RegexOptions.IgnoreCase);
            var expected = !(string.IsNullOrEmpty(expectedProvide) && string.IsNullOrEmpty(expectedPackageName));

            if (!expected)
            {
                return;
            }

            Assert.AreEqual(expectedProvide ?? string.Empty, mathed.Groups["provide"].Value);
            Assert.AreEqual(expectedPackageName, mathed.Groups["package"].Value);
        }
    }
}

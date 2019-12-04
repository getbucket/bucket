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

using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsGlob
    {
        [TestMethod]
        [DataRow(@"\.[^/]*", ".*")]
        [DataRow(@"(?=[^\.])foo/", "foo/")]
        [DataRow(@"(?=[^\.])foo/(?=[^\.])[^/]*\.txt", "foo/*.txt")]
        [DataRow(@"\.dot/(?=[^\.])[^/]*\.txt", ".dot/*.txt")]
        [DataRow(@"\.dot/(?:(?:(?=[^\.])[^/]+)+/)*(?=[^\.])[^/]*\.txt", ".dot/**/*.txt")]
        public void TestGlobParse(string expected, string input)
        {
            Assert.AreEqual(expected, Glob.Parse(input));
        }

        [TestMethod]
        public void TestGlobParseStrictDots()
        {
            Assert.AreEqual(@"(?=[^\.])/(?:(?:(?=[^\.])[^/]+)+/)*(?=[^\.])[^/]*\.txt", Glob.Parse("/**/*.txt"));
        }

        [TestMethod]
        public void TestGlobParseNonStrictDots()
        {
            Assert.AreEqual(@"/(?:(?:[^/]+)+/)*[^/]*\.txt", Glob.Parse("/**/*.txt", false));
        }

        [TestMethod]
        public void TestGlobParseWithoutLeadingSlash()
        {
            Assert.AreEqual(@"(?=[^\.])/(?=[^\.])Fixtures/(?=[^\.])foo/(?:(?:(?=[^\.])[^/]+)+/?)*", Glob.Parse("/Fixtures/foo/**"));
        }

        [TestMethod]
        public void TestGlobParseWithoutLeadingSlashNotStrictLeadingDot()
        {
            Assert.AreEqual(@"/Fixtures/foo/(?:(?:[^/]+)+/?)*", Glob.Parse("/Fixtures/foo/**", false));
        }
    }
}

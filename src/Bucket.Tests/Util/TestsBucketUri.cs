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
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsBucketUri
    {
        [TestMethod]
        [DataRow("https://api.github.com/repos/foo/bar/zipball/abc123", "https://github.com/foo/bar/zipball/reference", "abc123")]
        [DataRow("https://api.github.com/repos/foo/bar/tarball/abc123", "http://github.com/foo/bar/tarball/reference", "abc123")]
        [DataRow("https://api.github.com/repos/foo/bar/zipball/abc123", "https://www.github.com/foo/bar/zipball/reference", "abc123")]
        [DataRow("https://api.github.com/repos/foo/bar/zipball/abc123", "https://www.github.com/foo/bar/archive/bundle.zip", "abc123")]
        [DataRow("https://api.github.com/repos/foo/bar/zipball/abc123", "http://github.com/foo/bar/archive/bundle.zip", "abc123")]
        [DataRow("https://api.github.com/repos/foo/bar/tarball/abc123", "http://github.com/foo/bar/archive/bundle.tar.gz", "abc123")]
        [DataRow("https://api.github.com/repos/foo/bar/zipball/abc123", "https://api.github.com/repos/foo/bar/zipball/reference", "abc123")]
        [DataRow("https://api.github.com/repos/foo/bar/tarball/abc123", "http://api.github.com/repos/foo/bar/tarball/reference", "abc123")]
        [DataRow("https://gitlab.com/api/v4/projects/1/repository/archive.zip?sha=abc123", "https://gitlab.com/api/v4/projects/1/repository/archive.zip?sha=reference", "abc123")]
        [DataRow("https://gitlab.com/api/v4/projects/1/repository/archive.zip?sha=abc123", "http://www.gitlab.com/api/v4/projects/1/repository/archive.zip?sha=reference", "abc123")]
        [DataRow("https://gitlab.com/api/v4/projects/1/repository/archive.zip?sha=abc123", "https://www.gitlab.com/api/v3/projects/1/repository/archive.zip?sha=reference", "abc123")]
        public void TestUpdateDistReference(string expected, string uri, string reference)
        {
            var config = new Mock<Config>(true, null);
            var actual = BucketUri.UpdateDistReference(config.Object, uri, reference);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow("https://github.example.com/repos/foo/bar/zipball/abc123", "https://github.example.com/repos/foo/bar/zipball/reference", "abc123")]
        [DataRow("http://github.example.com/repos/foo/bar/zipball/abc123", "http://github.example.com/repos/foo/bar/zipball/reference", "abc123")]
        [DataRow("https://gitlab.example.com/api/v4/projects/1/repository/archive.zip?sha=abc123", "https://gitlab.example.com/api/v4/projects/1/repository/archive.zip?sha=reference", "abc123")]
        [DataRow("https://gitlab.example.com/api/v3/projects/1/repository/archive.zip?sha=abc123", "https://gitlab.example.com/api/v3/projects/1/repository/archive.zip?sha=reference", "abc123")]
        [DataRow("http://gitlab.example.com/api/v3/projects/1/repository/archive.tar.gz?sha=abc123", "http://gitlab.example.com/api/v3/projects/1/repository/archive.tar.gz?sha=reference", "abc123")]
        [DataRow("https://undefined.example.com", "https://undefined.example.com", "abc123")]
        public void TestUpdateDistReferenceWithConfigDomains(string expected, string uri, string reference)
        {
            var config = new Mock<Config>(true, null);
            config.Setup((o) => o.Get(It.IsIn(Settings.GithubDomains), ConfigOptions.None))
                .Returns(new[] { "github.example.com" });

            config.Setup((o) => o.Get(It.IsIn(Settings.GitlabDomains), ConfigOptions.None))
                .Returns(new[] { "gitlab.example.com" });

            var actual = BucketUri.UpdateDistReference(config.Object, uri, reference);
            Assert.AreEqual(expected, actual);
        }
    }
}

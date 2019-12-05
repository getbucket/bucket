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
using Bucket.Downloader;
using Bucket.Downloader.Transport;
using Bucket.IO;
using Bucket.Tests.Support.MockExtension;
using Bucket.Util.SCM;
using GameBox.Console.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Net.Http.Headers;
using static Moq.Mock;

namespace Bucket.Tests.Util.SCM
{
    [TestClass]
    public class TestsGithub
    {
        private Mock<IIO> io;
        private Mock<ITransport> transport;
        private Mock<Config> config;

        [TestInitialize]
        public void Initialize()
        {
            io = new Mock<IIO>();
            transport = new Mock<ITransport>();
            config = new Mock<Config>(true, null);
        }

        [TestMethod]
        public void TestUsernamePasswordAuthentication()
        {
            io.Setup((o) => o.AskPassword("Token (hidden): ", false))
                .Returns("token").Verifiable();

            io.Setup((o) => o.SetAuthentication("github.com", "token", "x-oauth-basic"))
                .Verifiable();

            transport.SetupGetString("https://api.github.com")
                .Returns("{}").Verifiable();

            var sourceAuth = new Mock<IConfigSource>();
            sourceAuth.Setup((o) => o.AddConfigSetting("github-oauth.github.com", "token"))
                .Verifiable();

            config.Setup((o) => o.GetSourceAuth())
                .Returns(sourceAuth.Object);

            var github = new Github(io.Object, config.Object, null, transport.Object);
            Assert.IsTrue(github.AuthorizeOAuthInteractively("github.com", "reason"));

            Verify(io, transport, sourceAuth);
        }

        [TestMethod]
        public void TestUsernamePasswordFailure()
        {
            io.Setup((o) => o.AskPassword("Token (hidden): ", false))
               .Returns("token").Verifiable();

            transport.SetupGetString("https://api.github.com")
               .Throws(new TransportException("failed", HttpStatusCode.Unauthorized, null));

            var github = new Github(io.Object, config.Object, null, transport.Object);
            Assert.IsFalse(github.AuthorizeOAuthInteractively("github.com"));
        }

        [TestMethod]
        [DataRow("https://api.github.com", "github.com")]
        [DataRow("https://example.com/api/v3", "example.com")]
        public void TestGetApi(string expected, string host)
        {
            Assert.AreEqual(expected, Github.GetApiUri(host));
        }

        [TestMethod]
        public void TestAuthorizeOAuth()
        {
            config.Setup((o) => o.Get(It.IsIn(Settings.GithubDomains), ConfigOptions.None))
                .Returns(new[] { "example.com" });

            io.Setup((o) => o.SetAuthentication("example.com", "token", "x-oauth-basic"))
                .Verifiable();

            var process = new Mock<IProcessExecutor>();
            process.Setup("git config github.accesstoken", "token");

            var github = new Github(io.Object, config.Object, process.Object, transport.Object);
            Assert.IsTrue(github.AuthorizeOAuth("example.com"));

            Verify(io);
        }

        [TestMethod]
        public void TestGetRateLimit()
        {
            var headers = new HttpTestHeaders
            {
                { "X-RateLimit-Limit", "5000" },
                { "X-RateLimit-Reset", "1574843420" },
            };

            var github = new Github(io.Object, config.Object, null, transport.Object);
            var (limit, reset) = github.GetRateLimit(headers);

            var expectedReset = DateTimeOffset.FromUnixTimeSeconds(1574843420)
                .ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            Assert.AreEqual("5000", limit);
            Assert.AreEqual(expectedReset, reset);
        }

        [TestMethod]
        [DataRow(true, "0")]
        [DataRow(false, "1")]
        [DataRow(false, "10")]
        public void TestIsRateLimit(bool expected, string remainin)
        {
            var headers = new HttpTestHeaders
            {
                { "X-RateLimit-Remainin", remainin },
            };

            var github = new Github(io.Object, config.Object, null, transport.Object);
            Assert.AreEqual(expected, github.IsRateLimit(headers));
        }

        private class HttpTestHeaders : HttpHeaders
        {
        }
    }
}

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
using Bucket.Downloader.Transport;
using Bucket.IO;
using Bucket.Tester;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Moq.Mock;

namespace Bucket.Tests.Downloader.Transport
{
    [TestClass]
    public class TestsTransportHttp
    {
        private Mock<HttpMessageHandler> handler;
        private Mock<Config> config;
        private TesterIOConsole tester;
        private TransportHttp transport;
        private IIO io;

        [TestInitialize]
        public void Initialize()
        {
            handler = new Mock<HttpMessageHandler>();
            var httpClient = new Mock<HttpClient>(handler.Object);
            config = new Mock<Config>(true, null) { CallBase = true };
            tester = new TesterIOConsole();
            io = tester.Mock();
            transport = new TransportHttp(io, config.Object, httpClient.Object);
        }

        [TestMethod]
        public void TestGetStringWithGithubAuthorization()
        {
            io.SetAuthentication("github.com", "token", "x-oauth-basic");
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cacnelToken) =>
                {
                    Assert.AreEqual("https://www.github.com/owner/repository", request.RequestUri.ToString());
                    Assert.AreEqual(HttpMethod.Get, request.Method);
                    Assert.IsTrue(request.Headers.TryGetValue("Authorization", out string auth));
                    Assert.AreEqual("Bearer token", auth);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("success"),
                    });
                })
                .Verifiable();

            var actual = transport.GetString("https://www.github.com/owner/repository");
            Assert.AreEqual("success", actual);

            Verify(handler);
        }

        [TestMethod]
        public void TestGetStringWithGitlabOAuth2()
        {
            io.SetAuthentication("gitlab.com", "token", "oauth2");
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cacnelToken) =>
                {
                    Assert.AreEqual("https://gitlab.com/owner/repository", request.RequestUri.ToString());
                    Assert.AreEqual(HttpMethod.Get, request.Method);
                    Assert.AreEqual("Bearer token", request.Headers.Authorization.ToString());

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("success"),
                    });
                })
                .Verifiable();

            var actual = transport.GetString("https://gitlab.com/owner/repository");
            Assert.AreEqual("success", actual);

            Verify(handler);
        }

        [TestMethod]
        public void TestGetStringWithGitlabPrivateToken()
        {
            io.SetAuthentication("gitlab.com", "token", "private-token");
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cacnelToken) =>
                {
                    Assert.AreEqual("https://gitlab.com/owner/repository", request.RequestUri.ToString());
                    Assert.AreEqual(HttpMethod.Get, request.Method);
                    Assert.IsTrue(request.Headers.TryGetValue("PRIVATE-TOKEN", out string value));
                    Assert.AreEqual("token", value);

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("success"),
                    });
                })
                .Verifiable();

            var actual = transport.GetString("https://gitlab.com/owner/repository");
            Assert.AreEqual("success", actual);

            Verify(handler);
        }

        [TestMethod]
        public void TestGetStringBasicAuth()
        {
            io.SetAuthentication("example.com", "username", "password");
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cacnelToken) =>
                {
                    var authStr = Convert.ToBase64String(Encoding.UTF8.GetBytes($"username:password"));
                    Assert.AreEqual("https://example.com/owner/repository", request.RequestUri.ToString());
                    Assert.AreEqual(HttpMethod.Get, request.Method);
                    Assert.AreEqual($"Basic {authStr}", request.Headers.Authorization.ToString());

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("success"),
                    });
                })
                .Verifiable();

            var actual = transport.GetString("https://example.com/owner/repository");
            Assert.AreEqual("success", actual);

            Verify(handler);
        }

        [TestMethod]
        public void TestCaptureAuthentication()
        {
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cacnelToken) =>
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("success"),
                    });
                })
                .Verifiable();

            var actual = transport.GetString("https://username:password@example.com/owner/repository");
            Assert.AreEqual("success", actual);

            Assert.IsTrue(io.HasAuthentication("example.com"));
            var (username, password) = io.GetAuthentication("example.com");

            Assert.AreEqual("username", username);
            Assert.AreEqual("password", password);
        }

        [TestMethod]
        public void TestUriParamsInputPriority()
        {
            io.SetAuthentication("github.com", "token", "x-oauth-basic");
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cacnelToken) =>
                {
                    Assert.AreEqual("https://www.github.com/owner/repository?access_token=abc123", request.RequestUri.ToString());
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("success"),
                    });
                })
                .Verifiable();

            var actual = transport.GetString("https://www.github.com/owner/repository?access_token=abc123");
            Assert.AreEqual("success", actual);

            Verify(handler);
        }
    }
}

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

#pragma warning disable S1450

using Bucket.Configuration;
using Bucket.Util;
using GameBox.Console.Util;
using System.Net.Http;

namespace Bucket.Downloader.Transport
{
    /// <summary>
    /// Allows the creation of a basic context supporting http client.
    /// </summary>
    public class HttpClientFactory
    {
        private readonly Config config;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientFactory"/> class.
        /// </summary>
        public HttpClientFactory(Config config)
        {
            this.config = config;
        }

        /// <summary>
        /// Create a new <see cref="HttpClient"/> instance.
        /// </summary>
        public static HttpClient CreateHttpClient(Config config)
        {
            var factory = new HttpClientFactory(config);
            return factory.CreateHttpClient();
        }

        /// <summary>
        /// Create a new <see cref="HttpClient"/> instance.
        /// </summary>
        public HttpClient CreateHttpClient()
        {
            var client = new HttpClient(CreateHttpClientHandler());
            return InitializeHttpClient(client);
        }

        /// <summary>
        /// Create the http client handler.
        /// </summary>
        protected virtual HttpClientHandler CreateHttpClientHandler()
        {
            // todo: implement proxy.
            // todo: implement tls/ssl.
            return new HttpClientHandler();
        }

        /// <summary>
        /// Initialize the http client.
        /// </summary>
        protected virtual HttpClient InitializeHttpClient(HttpClient client)
        {
            var headers = client.DefaultRequestHeaders;
            if (!headers.Contains("user-agent"))
            {
                var ci = string.Empty;
                if (!string.IsNullOrEmpty(Terminal.GetEnvironmentVariable("CI")))
                {
                    ci = "; CI";
                }

                var userAgent = "Bucket/{0} ({1}; {2}{3})";
                headers.Add("user-agent", string.Format(
                                                userAgent,
                                                Bucket.GetVersion(),
                                                Platform.GetOSInfo(),
                                                Platform.GetRuntimeInfo(),
                                                ci));
            }

            return client;
        }
    }
}

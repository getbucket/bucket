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
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using SException = System.Exception;

namespace Bucket.Downloader.Transport
{
    /// <summary>
    /// <seealso cref="TransportHttp"/> can download file from http.
    /// </summary>
    public class TransportHttp : ITransport
    {
        private readonly HttpClient httpClient;
        private readonly byte[] buffer;
        private readonly Config config;
        private readonly IIO io;
        private readonly ISet<string> displayedHostAuthentications;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportHttp"/> class.
        /// </summary>
        public TransportHttp()
            : this(IONull.That, new Config())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportHttp"/> class.
        /// </summary>
        public TransportHttp(IIO io, Config config, HttpClient httpClient = null)
        {
            this.io = io;
            this.config = config;
            this.httpClient = httpClient ?? HttpClientFactory.CreateHttpClient(config);
            buffer = new byte[8192];
            displayedHostAuthentications = new HashSet<string>();
        }

        /// <inheritdoc />
        /// <exception cref="TransportException">When the transport has an error.</exception>
        public virtual string GetString(string uri, out HttpHeaders httpResponseHeaders, IReadOnlyDictionary<string, object> additionalOptions = null)
        {
            using (var responseContent = new MemoryStream())
            {
                var (responseHeaders, contentHeaders) = GetRequest(uri, responseContent, null, additionalOptions);
                httpResponseHeaders = responseHeaders;
                return HttpContentConvert.ReadStreamAsString(responseContent, contentHeaders);
            }
        }

        /// <inheritdoc />
        public virtual void Copy(string uri, string target, IProgress<ProgressChanged> progress = null, IReadOnlyDictionary<string, object> additionalOptions = null)
        {
            FileSystemLocal.EnsureDirectory(Directory.GetParent(target).FullName);
            using (var saved = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length))
            {
                GetRequest(uri, saved, progress, additionalOptions);
            }
        }

        /// <summary>
        /// Throws an exception if the IsSuccessStatusCode property for the HTTP response is false.
        /// </summary>
        protected static HttpResponseMessage EnsureSuccessStatusCode(string uri, HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new TransportException(
                    $"Data transport failed ({uri}), status code: {response.StatusCode}", response.StatusCode, response.Headers, null);
            }

            return response;
        }

        /// <summary>
        /// Get the content from uri.
        /// </summary>
        /// <param name="uri">The remote file uri.</param>
        /// <param name="responseContent">The response content.</param>
        /// <param name="progress">Report access progress.</param>
        /// <param name="additionalOptions">The options for request.</param>
        protected virtual (HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders) GetRequest(
            string uri, Stream responseContent, IProgress<ProgressChanged> progress = null, IReadOnlyDictionary<string, object> additionalOptions = null)
        {
            var host = GetUriHost(uri);
            CaptureAuthentication(uri);

            var options = GetOptionsForUri(host, additionalOptions);
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            PrepareRequestMessage(request, options);

            HttpStatusCode httpStatusCode = 0;
            HttpResponseHeaders responseHeaders = null;
            try
            {
                using (var response = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result)
                {
                    EnsureSuccessStatusCode(uri, response);

                    httpStatusCode = response.StatusCode;
                    responseHeaders = response.Headers;

                    using (var content = response.Content)
                    {
                        ProgressDownload(response.Content, responseContent, progress).Wait();
                        return (response.Headers, response.Content.Headers);
                    }
                }
            }
#pragma warning disable CA1031
            catch (SException ex) when (!(ex is TransportException))
#pragma warning restore CA1031
            {
                throw new TransportException(ex.Message, httpStatusCode, responseHeaders, ex);
            }
        }

        /// <summary>
        /// Download as a progress callback.
        /// </summary>
        /// <param name="content">The http content instance.</param>
        /// <param name="saved">The stream will writed.</param>
        /// <param name="progress">The progress callback.</param>
        protected async Task ProgressDownload(HttpContent content, Stream saved, IProgress<ProgressChanged> progress = null)
        {
            using (var contentStream = await content.ReadAsStreamAsync())
            {
                var totalSize = content.Headers.ContentLength == null ?
                    -1 : content.Headers.ContentLength.Value;
                var totalRead = 0L;

                do
                {
                    var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0)
                    {
                        break;
                    }

                    await saved.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    progress?.Report(new ProgressChanged(totalSize, totalRead));
                }
                while (true);
            }
        }

        protected virtual string GetUriHost(string uri)
        {
            var host = new Uri(uri).Host;
            if (host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase))
            {
                host = "github.com";
            }

            return host;
        }

        protected virtual void PrepareRequestMessage(HttpRequestMessage request, Options options)
        {
            var uriBuilder = new UriBuilder(request.RequestUri.AbsoluteUri);
            var uriParams = HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (var param in options.RequestParams)
            {
                if (Array.Exists(uriParams.AllKeys, (key) => key == param.Key))
                {
                    continue;
                }

                uriParams.Add(param.Key, param.Value);
            }

            foreach (var header in options.RequestHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            uriBuilder.Query = uriParams.ToString();
            request.RequestUri = uriBuilder.Uri;
        }

        protected virtual Options GetOptionsForUri(string host, IReadOnlyDictionary<string, object> additionalOptions)
        {
            var options = new Options();

            if (io.HasAuthentication(host))
            {
                var authenticationDisplayMessage = string.Empty;
                var (username, password) = io.GetAuthentication(host);
                if (host == "github.com" && password == "x-oauth-basic")
                {
                    options.RequestHeaders.Add("Authorization", $"Bearer {username}");
                    authenticationDisplayMessage = "Using GitHub token authentication";
                }
                else if (config != null && Array.Exists((string[])config.Get(Settings.GitlabDomains), (domain) => domain == host))
                {
                    if (password == "oauth2")
                    {
                        options.RequestHeaders.Add("Authorization", $"Bearer {username}");
                        authenticationDisplayMessage = "Using Gitlab OAuth token authentication";
                    }
                    else if (password == "private-token" || password == "gitlab-ci-token")
                    {
                        options.RequestHeaders.Add("PRIVATE-TOKEN", username);
                        authenticationDisplayMessage = "Using GitLab private token authentication";
                    }
                }
                else
                {
                    var authStr = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                    options.RequestHeaders.Add("Authorization", $"Basic {authStr}");
                    authenticationDisplayMessage = $"Using HTTP basic authentication with username \"{username}\"";
                }

                if (!string.IsNullOrEmpty(authenticationDisplayMessage) && displayedHostAuthentications.Add(host))
                {
                    io.WriteError(authenticationDisplayMessage, verbosity: Verbosities.Debug);
                }
            }

            return options;
        }

        private void CaptureAuthentication(string uri)
        {
            var match = Regex.Match(uri, @"^https?://(?<user>[^:/]+):(?<pass>[^@/]+)@([^/]+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return;
            }

            var origin = BucketUri.GetOrigin(config, uri);
            var username = match.Groups["user"].Value;
            var password = match.Groups["pass"].Value;
            io.SetAuthentication(origin, Uri.UnescapeDataString(username), Uri.UnescapeDataString(password));
        }

        protected class Options
        {
            public IDictionary<string, string> RequestParams { get; } = new Dictionary<string, string>();

            public IDictionary<string, string> RequestHeaders { get; } = new Dictionary<string, string>();
        }
    }
}

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
using GameBox.Console.Process;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Bucket.Util.SCM
{
    /// <summary>
    /// Represents a GitHub helper function.
    /// </summary>
    public class Github
    {
        private readonly IIO io;
        private readonly Config config;
        private readonly IProcessExecutor process;
        private readonly ITransport transport;

        /// <summary>
        /// Initializes a new instance of the <see cref="Github"/> class.
        /// </summary>
        public Github(IIO io, Config config, IProcessExecutor process = null, ITransport transport = null)
        {
            this.io = io;
            this.config = config;
            this.process = process ?? new BucketProcessExecutor(io);
            this.transport = transport ?? new Factory().CreateTransport(io, config);
        }

        /// <summary>
        /// Get the github api uri.
        /// </summary>
        public static string GetApiUri(string host)
        {
            string apiUri;
            if (host == "github.com")
            {
                apiUri = "api.github.com";
            }
            else
            {
                apiUri = $"{host}/api/v3";
            }

            return $"https://{apiUri}";
        }

        /// <summary>
        /// Attempts to authorize a GitHub domain via OAuth.
        /// </summary>
        /// <param name="host">Auth host.</param>
        /// <returns>True on success.</returns>
        public virtual bool AuthorizeOAuth(string host)
        {
            string[] domains = config.Get(Settings.GithubDomains);
            if (!Array.Exists(domains, (domain) => domain == host))
            {
                return false;
            }

            // if available use token from git config.
            if (process.Execute("git config github.accesstoken", out string stdout) != 0)
            {
                return false;
            }

            io.SetAuthentication(host, (stdout ?? string.Empty).Trim(), "x-oauth-basic");
            return true;
        }

        /// <summary>
        /// Authorizes a GitHub domain interactively via OAuth.
        /// </summary>
        /// <param name="host">The host this GitHub instance is located at.</param>
        /// <param name="reason">The reason this authorization is required.</param>
        /// <returns>True on success.</returns>
        public virtual bool AuthorizeOAuthInteractively(string host, string reason = null)
        {
            if (!string.IsNullOrEmpty(reason))
            {
                io.WriteError(reason);
            }

            var note = "Bucket";

            // todo: Switch to control whether hostname is displayed.
            try
            {
                if (process.Execute("hostname", out string stdout) == 0)
                {
                    note = $"{note} on {stdout.Trim()}";
                }
            }
            catch (TimeoutException)
            {
                // noop.
            }

            note = $"{note} {DateTime.Now.ToString("yyyy-MM-dd HHmm")}";
            note = Uri.EscapeDataString(note).Replace("%20", "+");
            var uri = $"https://{host}/settings/tokens/new?scopes=repo&description={note}";
            io.WriteError($"Head to {uri}");
            io.WriteError($"to retrieve a token. It will be stored in \"{config.GetSourceAuth()?.GetPrettyName()}\" for future use by Bucket.");

            var token = (io.AskPassword("Token (hidden): ") ?? string.Empty).ToString().Trim();
            if (string.IsNullOrEmpty(token))
            {
                io.WriteError("<warning>No token given, aborting.</warning>");
                return false;
            }

            io.SetAuthentication(host, token, "x-oauth-basic");

            try
            {
                // test auth.
                transport.GetString(GetApiUri(host));
            }
            catch (TransportException ex)
                when (ex.HttpStatusCode == HttpStatusCode.Forbidden ||
                      ex.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                io.WriteError("<error>Invalid token provided.</error>");
                return false;
            }

            config.GetSourceBucket()?.RemoveConfigSetting($"{Settings.GithubOAuth}.{host}");
            var status = config.GetSourceAuth()?.AddConfigSetting($"{Settings.GithubOAuth}.{host}", token);
            if (status.HasValue && status.Value)
            {
                io.WriteError("<info>Token stored successfully.</info>");
            }
            else
            {
                io.WriteError("<info>Token stored failed.</info>");
            }

            return true;
        }

        /// <summary>
        /// Extract ratelimit from response.
        /// </summary>
        /// <param name="headers">The http response headers.</param>
        public virtual (string limit, string reset) GetRateLimit(HttpHeaders headers)
        {
            headers.TryGetValue("X-RateLimit-Limit", out string limit);

            string reset = null;
            if (headers.TryGetValue("X-RateLimit-Reset", out string headerReset))
            {
                reset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(headerReset)).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }

            return (limit ?? string.Empty, reset ?? string.Empty);
        }

        /// <summary>
        /// Whether a request failed due to rate limiting.
        /// </summary>
        public virtual bool IsRateLimit(HttpHeaders headers)
        {
            if (headers == null || !headers.TryGetValue("X-RateLimit-Remainin", out string header))
            {
                return false;
            }

            return header.Trim() == "0";
        }
    }
}

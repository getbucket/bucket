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
using System;
using System.Text.RegularExpressions;

namespace Bucket.Util
{
    /// <summary>
    /// Some helper functions for uri operations.
    /// </summary>
    public static class BucketUri
    {
        private static readonly string[] Github = new[] { "api.github.com", "github.com", "www.github.com" };
        private static readonly string[] Gitlab = new[] { "gitlab.com", "www.gitlab.com" };

        /// <summary>
        /// Upgrade the uri to match the corresponding code hosting platform.
        /// </summary>
        public static string UpdateDistReference(Config config, string uri, string reference)
        {
            var host = new Uri(uri).Host.ToLower();

            if (Array.Exists(Github, (v) => v == host))
            {
                var upgradeUris = new[]
                {
                    @"^https?://(?:www\.)?github\.com/(?<user>[^/]+)/(?<repo>[^/]+)/(?<type>zip|tar)ball/(.+)$",
                    @"^https?://(?:www\.)?github\.com/(?<user>[^/]+)/(?<repo>[^/]+)/archive/.+\.(?<type>zip|tar)(?:\.gz)?$",
                    @"^https?://api\.github\.com/repos/(?<user>[^/]+)/(?<repo>[^/]+)/(?<type>zip|tar)ball(?:/.+)?$",
                };

                foreach (var regex in upgradeUris)
                {
                    var match = Regex.Match(uri, regex, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return $"https://api.github.com/repos/{match.Groups["user"].Value}/{match.Groups["repo"].Value}/{match.Groups["type"].Value}ball/{reference}";
                    }
                }

                return uri;
            }

            if (Array.Exists(Gitlab, (v) => v == host))
            {
                var match = Regex.Match(uri, @"^https?://(?:www\.)?gitlab\.com/api/v[34]/projects/(?<id>[^/]+)/repository/archive\.(?<type>zip|tar\.gz|tar\.bz2|tar)\?sha=.+$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return $"https://gitlab.com/api/v4/projects/{match.Groups["id"].Value}/repository/archive.{match.Groups["type"].Value}?sha={reference}";
                }

                return uri;
            }

            string[] domains = config.Get(Settings.GithubDomains) ?? Array.Empty<string>();
            if (Array.Exists(domains, (v) => v == host))
            {
                return Regex.Replace(uri, @"(/repos/[^/]+/[^/]+/(zip|tar)ball)(?:/.+)?$", $"${{1}}/{reference}", RegexOptions.IgnoreCase);
            }

            domains = config.Get(Settings.GitlabDomains) ?? Array.Empty<string>();
            if (Array.Exists(domains, (v) => v == host))
            {
                return Regex.Replace(uri, @"(/api/v[34]/projects/[^/]+/repository/archive\.(?:zip|tar\.gz|tar\.bz2|tar)\?sha=).+$", $"${{1}}{reference}", RegexOptions.IgnoreCase);
            }

            return uri;
        }

        /// <summary>
        /// Get the origin uri from uri.
        /// </summary>
        /// <remarks>E.g: repo.gxpack.org original uri is gxpack.org.</remarks>
        public static string GetOrigin(Config config, string uri)
        {
            if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return uri;
            }

            var uriInstance = new Uri(uri);
            var origin = uriInstance.Host;
            if (!uriInstance.IsDefaultPort)
            {
                origin += $"{origin}:{uriInstance.Port}";
            }

            if (origin == $"repo.{Config.DefaultRepositoryDomain}")
            {
                return Config.DefaultRepositoryDomain;
            }

            if (origin.IndexOf(".github.com", StringComparison.OrdinalIgnoreCase)
                == (origin.Length - 11))
            {
                return "github.com";
            }

            if (string.IsNullOrEmpty(origin))
            {
                origin = uri;
            }

            // Gitlab can be installed in a non-root context (i.e. gitlab.com/foo).
            // When downloading archives the originUri is the host without the path,
            // so we look for the registered gitlab-domains matching the host here.
            string[] domains = config.Get(Settings.GitlabDomains);
            if (!origin.Contains("/")
                && !Array.Exists(domains, (domain) => domain == origin)
                && Arr.Test(domains, origin.StartsWith, out string match))
            {
                return match;
            }

            return origin;
        }

        /// <summary>
        /// Determine if a Url is absolute or relative.
        /// </summary>
        public static bool IsAbsoluteUri(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException($"URL {uri} was in an invalid format", nameof(uri));
            }

            return Uri.IsWellFormedUriString(uri, UriKind.Absolute);
        }
    }
}

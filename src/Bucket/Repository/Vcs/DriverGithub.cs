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

using Bucket.Cache;
using Bucket.Configuration;
using Bucket.Downloader;
using Bucket.Downloader.Transport;
using Bucket.Exception;
using Bucket.IO;
using Bucket.Json;
using Bucket.Util;
using Bucket.Util.SCM;
using GameBox.Console.Process;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using static System.Uri;
using static Bucket.Util.SCM.Github;
using SException = System.Exception;

namespace Bucket.Repository.Vcs
{
    /// <summary>
    /// Represents the driver for github.
    /// </summary>
    public class DriverGithub : DriverVcs
    {
        /// <summary>
        /// The name of the driver.
        /// </summary>
        public const string DriverName = "github";

        private const string RegexGithub = @"^((?:https?|git)://(?<host>[^/]+)/|git@(?<host>[^:]+):/?)(?<owner>[^/]+)/(?<repository>.+?)(?:\.git|/)?$";
        private readonly ConfigRepositoryGithub configRepository;
        private readonly IDictionary<string, ConfigBucket> cacheInfomartion;
        private GithubRepoData repoData;
        private string host;
        private string owner;
        private string repository;
        private ICache cache;
        private DriverGit driverGit;
        private bool isPrivate;
        private bool hasIssues;
        private string rootIdentifier;
        private Dictionary<string, string> branches;
        private Dictionary<string, string> tags;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriverGithub"/> class.
        /// </summary>
        /// <param name="configRepository">The repository configuration.</param>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The config instance.</param>
        /// <param name="transport">The remote transport instance.</param>
        /// <param name="process">The process instance.</param>
        public DriverGithub(ConfigRepositoryGithub configRepository, IIO io, Config config, ITransport transport = null, IProcessExecutor process = null)
            : base(configRepository, io, config, transport, process)
        {
            this.configRepository = configRepository;
            cacheInfomartion = new Dictionary<string, ConfigBucket>();
        }

        /// <summary>
        /// Whether the driver support the specified uri.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The config instance.</param>
        /// <param name="uri">Thr specified uri.</param>
        /// <param name="deep">Whether is deep checker.</param>
        /// <returns>True if the driver is supported.</returns>
        public static bool IsSupport(IIO io, Config config, string uri, bool deep = false)
        {
            var match = Regex.Match(uri, RegexGithub);
            if (!match.Success)
            {
                return false;
            }

            var uriHost = match.Groups["host"].Value;
            uriHost = Regex.Replace(uriHost, @"^www\.", string.Empty, RegexOptions.IgnoreCase);

            string[] domains = config.Get(Settings.GithubDomains);
            if (!Array.Exists(domains, (domain) => domain == uriHost))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            var match = Regex.Match(Uri, RegexGithub);
            if (!match.Success)
            {
                throw new RuntimeException($"Repository uri is not a valid github address: {Uri}");
            }

            host = match.Groups["host"].Value;
            owner = match.Groups["owner"].Value;
            repository = match.Groups["repository"].Value;

            if (host == "www.github.com")
            {
                host = "github.com";
            }

            var cacheDirectory = Path.Combine(Config.Get(Settings.CacheRepoDir), host, owner, repository);
            cache = new CacheFileSystem(cacheDirectory, IO);

            if (configRepository.NoApi)
            {
                SetupDriverGit(configRepository.Uri);
            }

            FetchRootIdentifier();
        }

        /// <inheritdoc />
        public override ConfigBucket GetBucketInformation(string identifier)
        {
            if (driverGit != null)
            {
                return driverGit.GetBucketInformation(identifier);
            }

            if (cacheInfomartion.TryGetValue(identifier, out ConfigBucket configBucket))
            {
                return configBucket;
            }

            var shouldCached = ShouldCache(identifier);
            if (shouldCached && GetCache().TryRead(identifier, out Stream stream))
            {
                return cacheInfomartion[identifier] = ParseBucketInformation(stream.ToText());
            }

            configBucket = GetBaseBucketInformation(identifier);

            if (configBucket != null)
            {
                configBucket.Support = configBucket.Support ?? new Dictionary<string, string>();

                // specials for github.
                if (!configBucket.Support.ContainsKey("source"))
                {
                    var label = GetTags().FirstOrDefault(item => item.Value == identifier).Key;
                    if (string.IsNullOrEmpty(label))
                    {
                        label = GetBranches().FirstOrDefault(item => item.Value == identifier).Key;
                        if (string.IsNullOrEmpty(label))
                        {
                            label = identifier;
                        }
                    }

                    configBucket.Support["source"] = $"https://{host}/{owner}/{repository}/tree/{label}";
                }

                if (hasIssues && !configBucket.Support.ContainsKey("issues"))
                {
                    configBucket.Support["issues"] = $"https://{host}/{owner}/{repository}/issues";
                }
            }

            if (shouldCached)
            {
                GetCache().Write(identifier, configBucket ?? string.Empty);
            }

            return configBucket;
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, string> GetBranches()
        {
            if (driverGit != null)
            {
                return driverGit.GetBranches();
            }

            if (branches != null)
            {
                return branches;
            }

            var resourceUri = $"{GetApiUri(host)}/repos/{owner}/{repository}/git/refs/heads?per_page=100";
            var branchBanlist = new[] { "gh-pages" };
            branches = new Dictionary<string, string>();
            do
            {
                var (content, headers) = GetRemoteContent(resourceUri);

                if (string.IsNullOrEmpty(content))
                {
                    break;
                }

                var branchData = JsonFile.Parse<GithubBranches>(content);
                foreach (var branch in branchData)
                {
                    var name = branch.Ref.Length > 11 ? branch.Ref.Substring(11) : branch.Ref;
                    if (Array.Exists(branchBanlist, (v) => v == name))
                    {
                        continue;
                    }

                    branches[name] = branch.Object.Sha;
                }

                resourceUri = GetNextPage(headers);
            }
            while (!string.IsNullOrEmpty(resourceUri));

            return branches;
        }

        /// <inheritdoc />
        public override ConfigResource GetDist(string identifier)
        {
            var uri = $"{GetApiUri(host)}/repos/{owner}/{repository}/zipball/{identifier}";
            return new ConfigResource { Type = "zip", Uri = uri, Reference = identifier };
        }

        /// <inheritdoc />
        public override string GetRootIdentifier()
        {
            if (driverGit != null)
            {
                return driverGit.GetRootIdentifier();
            }

            return rootIdentifier;
        }

        /// <inheritdoc />
        public override ConfigResource GetSource(string identifier)
        {
            if (driverGit != null)
            {
                return driverGit.GetSource(identifier);
            }

            string uri;
            if (isPrivate)
            {
                // Private GitHub repositories should be accessed using the
                // SSH version of the URL.
                uri = GenerateSSHUri();
            }
            else
            {
                uri = GetUri();
            }

            return new ConfigResource { Type = "git", Uri = uri, Reference = identifier };
        }

        /// <summary>
        /// Get the uri.
        /// </summary>
#pragma warning disable CA1721
        public string GetUri()
#pragma warning restore CA1721
        {
            if (driverGit != null)
            {
                return driverGit.GetUri();
            }

            return $"https://{host}/{owner}/{repository}.git";
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, string> GetTags()
        {
            if (driverGit != null)
            {
                return driverGit.GetTags();
            }

            if (tags != null)
            {
                return tags;
            }

            var resourceUri = $"{GetApiUri(host)}/repos/{owner}/{repository}/tags?per_page=100";
            tags = new Dictionary<string, string>();
            do
            {
                var (content, headers) = GetRemoteContent(resourceUri);

                if (string.IsNullOrEmpty(content))
                {
                    break;
                }

                var tagsData = JsonFile.Parse<GithubTags>(content);
                foreach (var tag in tagsData)
                {
                    tags[tag.Name] = tag.Commit.Sha;
                }

                resourceUri = GetNextPage(headers);
            }
            while (!string.IsNullOrEmpty(resourceUri));

            return tags;
        }

        /// <inheritdoc />
        protected internal override string GetFileContent(string file, string identifier)
        {
            if (driverGit != null)
            {
                return driverGit.GetFileContent(file, identifier);
            }

            var resourceUri = $"{GetApiUri(host)}/repos/{owner}/{repository}/contents/{file}?ref={EscapeDataString(identifier)}";
            var (resourceContent, _) = GetRemoteContent(resourceUri);

            RuntimeException CreateNotRetrieveException(string message)
            {
                message = message ?? string.Empty;
                return new RuntimeException($"Could not retrieve {file} for {identifier}: {message}");
            }

            if (string.IsNullOrEmpty(resourceContent))
            {
                throw CreateNotRetrieveException("remote content is empty");
            }

            try
            {
                var resource = JsonFile.Parse<GithubResource>(resourceContent);
                if (resource.Encoding != "base64")
                {
                    throw CreateNotRetrieveException($"remote content not base64 format: {resource.Encoding}");
                }

                return Encoding.UTF8.GetString(Convert.FromBase64String(resource.Content));
            }
            catch (SException ex)
            {
                throw CreateNotRetrieveException(ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        protected internal override DateTime? GetChangeDate(string identifier)
        {
            if (driverGit != null)
            {
                return driverGit.GetChangeDate(identifier);
            }

            var resourceUri = $"{GetApiUri(host)}/repos/{owner}/{repository}/commits/{EscapeDataString(identifier)}";
            var (resourceContent, _) = GetRemoteContent(resourceUri);
            var resource = JsonFile.Parse<GithubCommits>(resourceContent);

            return DateTime.Parse(resource.Commit.Commiter.Date).ToLocalTime();
        }

        /// <summary>
        /// Get the github next page.
        /// </summary>
        protected static string GetNextPage(HttpHeaders headers)
        {
            if (headers == null || !headers.TryGetValue("link", out string header))
            {
                return null;
            }

            var links = header.Split(',');
            foreach (var link in links)
            {
                var match = Regex.Match(link, @"<(?<next>.+?)>; *rel=""next""");
                if (match.Success)
                {
                    return match.Groups["next"].Value;
                }
            }

            return null;
        }

        /// <inheritdoc />
        protected override ICache GetCache() => cache;

        /// <summary>
        /// Fetch root identifier from GitHub.
        /// </summary>
        protected void FetchRootIdentifier()
        {
            if (repoData != null)
            {
                return;
            }

            var repoDataUri = $"{GetApiUri(host)}/repos/{owner}/{repository}";
            var (repoContent, _) = GetRemoteContent(repoDataUri, true);

            // We can use alternatives.
            if (string.IsNullOrEmpty(repoContent) && driverGit != null)
            {
                return;
            }

            repoData = JsonFile.Parse<GithubRepoData>(repoContent);

            owner = repoData.Owner.Login;
            repository = repoData.Name;

            if (!string.IsNullOrEmpty(repoData.DefaultBranch))
            {
                rootIdentifier = repoData.DefaultBranch;
            }
            else if (!string.IsNullOrEmpty(repoData.MasterBranch))
            {
                rootIdentifier = repoData.MasterBranch;
            }
            else
            {
                rootIdentifier = "master";
            }

            hasIssues = repoData.HasIssues;
        }

        /// <inheritdoc cref="GetRemoteContent(string, bool)"/>
        /// <param name="fetchingRepoData">Whether is fetching the main repository data.</param>
        protected (string Content, HttpHeaders Headers) GetRemoteContent(string uri, bool fetchingRepoData = false)
        {
            try
            {
                return base.GetRemoteContent(uri);
            }
            catch (TransportException ex)
            {
                var github = new Github(IO, Config, Process, GetTransport());
                switch (ex.HttpStatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.NotFound:
                        if (!fetchingRepoData)
                        {
                            throw;
                        }

                        if (github.AuthorizeOAuth(host))
                        {
                            return base.GetRemoteContent(uri);
                        }

                        if (!IO.IsInteractive && AttemptCloneFallback())
                        {
                            return (null, null);
                        }

                        var scopesIssued = Array.Empty<string>();
                        var scopesNeeded = Array.Empty<string>();
                        var headers = ex.GetHeaders();
                        if (headers != null)
                        {
                            if (headers.TryGetValues("X-OAuth-Scopes", out IEnumerable<string> values))
                            {
                                var scopes = string.Join(string.Empty, values);
                                scopesIssued = scopes.Split(' ');
                            }

                            if (headers.TryGetValues("X-Accepted-OAuth-Scopes", out values))
                            {
                                var scopes = string.Join(string.Empty, values);
                                scopesNeeded = scopes.Split(' ');
                            }
                        }

                        var scopesFailed = Arr.Difference(scopesIssued, scopesNeeded);

                        // non-authenticated requests get no scopesNeeded, so ask
                        // for credentials authenticated requests which failed some
                        // scopes should ask for new credentials too
                        if (headers == null || scopesNeeded.Length == 0 || scopesFailed.Length > 0)
                        {
                            github.AuthorizeOAuthInteractively(host, $"Your GitHub credentials are required to fetch private repository metadata (<info>{uri}</info>)");
                        }

                        return base.GetRemoteContent(uri);
                    case HttpStatusCode.Forbidden:
                        if (!IO.HasAuthentication(host) && github.AuthorizeOAuth(host))
                        {
                            return base.GetRemoteContent(uri);
                        }

                        if (!IO.IsInteractive && fetchingRepoData && AttemptCloneFallback())
                        {
                            return (null, null);
                        }

                        var isRateLimited = github.IsRateLimit(ex.GetHeaders());
                        if (!IO.HasAuthentication(host))
                        {
                            if (!IO.IsInteractive)
                            {
                                IO.WriteError($"<error>GitHub API limit exhausted. Failed to get metadata for the {uri} repository, try running in interactive mode so that you can enter your GitHub credentials to increase the API limit</error>");
                                throw;
                            }

                            github.AuthorizeOAuthInteractively(host, $"API limit exhausted. Enter your GitHub credentials to get a larger API limit (<info>{uri}</info>)");
                            return base.GetRemoteContent(uri);
                        }

                        if (isRateLimited)
                        {
                            var (limit, reset) = github.GetRateLimit(ex.GetHeaders());
                            IO.WriteError($"<error>GitHub API limit ({limit} calls/hr) is exhausted. You are already authorized so you have to wait until {reset} before doing more requests</error>");
                        }

                        throw;
                    default:
                        throw;
                }
            }
        }

        /// <summary>
        /// Set the driver git to replace github api driver.
        /// </summary>
        protected void SetupDriverGit(string uri)
        {
            driverGit = new DriverGit(new ConfigRepositoryVcs { Uri = uri }, IO, Config, Process);
            driverGit.Initialize();
        }

        /// <summary>
        /// Generate an SSH URI.
        /// </summary>
        protected string GenerateSSHUri()
        {
            if (host.Contains(":"))
            {
                return $"ssh://git@{host}/{owner}/{repository}.git";
            }

            return $"git@{host}:{owner}/{repository}.git";
        }

        protected virtual bool AttemptCloneFallback()
        {
            isPrivate = true;

            try
            {
                // If this repository may be private (hard to say for sure,
                // GitHub returns 404 for private repositories) and we
                // cannot ask for authentication credentials (because we
                // are not interactive) then we fallback to GitDriver.
                SetupDriverGit(GenerateSSHUri());
            }
            catch (RuntimeException ex)
            {
                driverGit = null;
                IO.WriteError($"<error>Failed to clone the {GenerateSSHUri()} repository, try running in interactive mode so that you can enter your GitHub credentials. Exception: {ex.Message}</error>");
                throw;
            }

            return true;
        }

        private sealed class GithubRepoData
        {
            [JsonProperty("owner")]
            public GithubOwner Owner { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("private")]
            public bool IsPrivate { get; set; }

            [JsonProperty("has_issues")]
            public bool HasIssues { get; set; }

            [JsonProperty("default_branch")]
            public string DefaultBranch { get; set; }

            [JsonProperty("master_branch")]
            public string MasterBranch { get; set; }
        }

        private sealed class GithubOwner
        {
            [JsonProperty("login")]
            public string Login { get; set; }
        }

        private sealed class GithubResource
        {
            [JsonProperty("encoding")]
            public string Encoding { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }

        private sealed class GithubCommits
        {
            [JsonProperty("commit")]
            public GithubCommit Commit { get; set; }
        }

        private sealed class GithubCommit
        {
            [JsonProperty("committer")]
            public GithubCommiter Commiter { get; set; }
        }

        private sealed class GithubCommiter
        {
            [JsonProperty("date")]
            public string Date { get; set; }
        }

        private sealed class GithubBranches : List<GithubBranch>
        {
        }

        private sealed class GithubBranch
        {
            [JsonProperty("ref")]
            public string Ref { get; set; }

            [JsonProperty("object")]
            public GithubBranchObject Object { get; set; }
        }

        private sealed class GithubBranchObject
        {
            [JsonProperty("sha")]
            public string Sha { get; set; }
        }

        private sealed class GithubTags : List<GithubTag>
        {
        }

        private sealed class GithubTag
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("commit")]
            public GithubTagCommit Commit { get; set; }
        }

        private sealed class GithubTagCommit
        {
            [JsonProperty("sha")]
            public string Sha { get; set; }
        }
    }
}

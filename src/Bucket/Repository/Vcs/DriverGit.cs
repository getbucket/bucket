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
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Util;
using Bucket.Util.SCM;
using GameBox.Console.Exception;
using GameBox.Console.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Bucket.Repository.Vcs
{
    /// <summary>
    /// Represents a Git driver.
    /// </summary>
    public class DriverGit : DriverVcs
    {
        /// <summary>
        /// The name of the driver.
        /// </summary>
        public const string DriverName = "git";

        private string repositoryDirectory;
        private string rootIdentifier;
        private Dictionary<string, string> tags;
        private Dictionary<string, string> branches;
        private ICache cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriverGit"/> class.
        /// </summary>
        /// <param name="configRepository">The repository configuration.</param>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The bucket config.</param>
        /// <param name="process">The process instance.</param>
        public DriverGit(ConfigRepositoryVcs configRepository, IIO io, Config config, IProcessExecutor process = null)
            : base(configRepository, io, config, null, process)
        {
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
            if (Regex.IsMatch(uri, @"(^git://|\.git/?$|git(?:olite)?@|//git\.|//github\.com/|//gitlib\.com/)", RegexOptions.IgnoreCase))
            {
                return true;
            }

            bool ProcessCheck(string command, string cwd)
            {
                var process = new BucketProcessExecutor(io);
                return process.Execute(command, cwd) == 0;
            }

            if (FileSystemLocal.IsLocalPath(uri))
            {
                uri = FileSystemLocal.GetPlatformPath(uri);
                if (!Directory.Exists(uri))
                {
                    return false;
                }

                return ProcessCheck("git tag", uri);
            }

            if (!deep)
            {
                return false;
            }

            return ProcessCheck($"git ls-remote --heads {ProcessExecutor.Escape(uri)}", null);
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            string cacheUri;
            if (FileSystemLocal.IsLocalPath(Uri))
            {
                Uri = Regex.Replace(Uri, @"[\\/]\.git/?$", string.Empty);
                repositoryDirectory = Uri;
                cacheUri = Path.GetFullPath(Uri);
            }
            else
            {
                var cacheVcsDir = Config.Get(Settings.CacheVcsDir);
                if (!CacheFileSystem.IsUsable(cacheVcsDir))
                {
                    throw new RuntimeException("DriverGit requires a usable cache directory, and it looks like you set it to be disabled.");
                }

                if (Regex.IsMatch("^ssh://[^@]+@[^:]+:[^0-9]+", Uri))
                {
                    throw new InvalidArgumentException($"The source URL {Uri} is invalid, ssh URLs should have a port number after \":\".{Environment.NewLine}Use ssh://git@example.com:22/path or just git@example.com:path if you do not want to provide a password or custom port.");
                }

                Git.CleanEnvironment();

                var fileSystem = new FileSystemLocal($"{cacheVcsDir}/{CacheFileSystem.FormatCacheFolder(Uri)}/");
                var git = new Git(IO, Config, Process, fileSystem);

                repositoryDirectory = fileSystem.Root;

                if (!git.SyncMirror(Uri, repositoryDirectory))
                {
                    IO.WriteError($"<error>Failed to update {Uri}, package information from this repository may be outdated</error>");
                }

                cacheUri = Uri;
            }

            GetTags();
            GetBranches();

            cache = new CacheFileSystem($"{Config.Get(Settings.CacheRepoDir)}/{CacheFileSystem.FormatCacheFolder(cacheUri)}", IO);
        }

        /// <inheritdoc />
        public override string GetRootIdentifier()
        {
            if (!string.IsNullOrEmpty(rootIdentifier))
            {
                return rootIdentifier;
            }

            rootIdentifier = "master";

            // select currently checked out branch if master is not available.
            Process.Execute("git branch --no-color", out string[] branches, repositoryDirectory);

            if (Array.Exists(branches, (branch) => branch == "* master"))
            {
                return rootIdentifier;
            }

            foreach (var branch in branches)
            {
                if (string.IsNullOrEmpty(branch))
                {
                    continue;
                }

                var matched = Regex.Match(branch, @"^\* +(?<branch>\S+)");
                if (!matched.Success)
                {
                    continue;
                }

                rootIdentifier = matched.Groups["branch"].Value;
                break;
            }

            return rootIdentifier;
        }

        /// <inheritdoc />
        public override ConfigResource GetSource(string identifier)
        {
            return new ConfigResource()
            {
                Type = "git",
                Uri = Uri,
                Reference = identifier,
            };
        }

        /// <inheritdoc />
        public override ConfigResource GetDist(string identifier)
        {
            // Git repository can't get dist, so return null.
            return null;
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, string> GetTags()
        {
            if (tags != null)
            {
                return tags;
            }

            Process.Execute("git show-ref --tags --dereference", out string[] stdout, repositoryDirectory);

            tags = new Dictionary<string, string>();
            foreach (var tag in stdout)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                var matched = Regex.Match(tag, @"^(?<identifier>[a-f0-9]{40}) refs/tags/(?<tag>\S+?)(\^\{\})?$");

                if (!matched.Success)
                {
                    continue;
                }

                tags[matched.Groups["tag"].Value] = matched.Groups["identifier"].Value;
            }

            return tags;
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, string> GetBranches()
        {
            if (branches != null)
            {
                return branches;
            }

            Process.Execute("git branch --no-color --no-abbrev -v", out string[] stdout, repositoryDirectory);

            branches = new Dictionary<string, string>();
            foreach (var branch in stdout)
            {
                if (string.IsNullOrEmpty(branch) || Regex.IsMatch(branch, "^ *[^/]+/HEAD "))
                {
                    continue;
                }

                var matched = Regex.Match(branch, @"^(?:\* )? *(?<branch>\S+) *(?<identifier>[a-f0-9]+)(?: .*)?$");

                if (!matched.Success)
                {
                    continue;
                }

                branches[matched.Groups["branch"].Value] = matched.Groups["identifier"].Value;
            }

            return branches;
        }

        /// <summary>
        /// Get the git uri.
        /// </summary>
#pragma warning disable CA1721
        public string GetUri()
#pragma warning restore CA1721
        {
            return Uri;
        }

        /// <inheritdoc />
        protected internal override string GetFileContent(string file, string identifier)
        {
            var resources = $"{identifier}:{file}";
            Process.Execute($"git show {resources}", out string stdout, repositoryDirectory);

            if (string.IsNullOrEmpty(stdout.Trim()))
            {
                return null;
            }

            return stdout;
        }

        /// <inheritdoc />
        protected internal override DateTime? GetChangeDate(string identifier)
        {
            Process.Execute($"git log -1 --format=%aD {identifier}", out string stdout, repositoryDirectory);
            return DateTime.Parse(stdout);
        }

        /// <inheritdoc />
        protected override ICache GetCache()
        {
            return cache;
        }
    }
}

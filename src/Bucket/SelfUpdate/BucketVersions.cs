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
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Json;
using Bucket.Semver;
using Bucket.Util;
using GameBox.Console.Process;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Bucket.SelfUpdate
{
    /// <summary>
    /// Indicates the version information of the bucket.
    /// </summary>
    public sealed class BucketVersions
    {
        internal const string Home = "getbucket.gxpack.org";
        private readonly Config config;
        private readonly ITransport transport;
        private readonly IFileSystem fileSystem;
        private readonly IProcessExecutor process;
        private readonly IIO io;
        private Channel? channel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketVersions"/> class.
        /// </summary>
        public BucketVersions(IIO io, Config config, ITransport transport, IFileSystem fileSystem = null, IProcessExecutor process = null)
        {
            this.io = io;
            this.config = config;
            this.transport = transport;
            this.fileSystem = fileSystem ?? new FileSystemLocal();
            this.process = process ?? new BucketProcessExecutor(io);
        }

        /// <summary>
        /// Get the update channel.
        /// </summary>
        public Channel GetChannel()
        {
            if (channel != null)
            {
                return channel.Value;
            }

            var channelFile = Path.Combine(config.Get("home"), "update-channel");
            if (fileSystem.Exists(channelFile, FileSystemOptions.File)
                && Enum.TryParse(fileSystem.ReadString(channelFile).Trim(), out Channel channelInFile))
            {
                channel = channelInFile;
                return channel.Value;
            }

            channel = Channel.Stable;
            return channel.Value;
        }

        /// <summary>
        /// Set the update channel.
        /// </summary>
        public void SetChannel(Channel channel)
        {
            this.channel = channel;
            var channelFile = Path.Combine(config.Get("home"), "update-channel");
            fileSystem.Write(channelFile, channel.ToString());
            io.WriteError($"Write the channel file: {channelFile}", true, Verbosities.Debug);
        }

        /// <summary>
        /// Get the latest version. if the <see cref="Channel.Dev"/> channel, it will be a commit sha.
        /// </summary>
        public BucketVersion GetLatest()
        {
            var body = transport.GetString($"https://{Home}/versions");
            if (string.IsNullOrEmpty(body))
            {
                body = "{}";
            }

            var frameworkVersion = GetHighestFrameworkVersion();
            var channelMapping = JsonFile.Parse<ConfigVersions>(body);
            if (channelMapping.TryGetValue(GetChannel(), out ConfigVersion[] versions))
            {
                foreach (var version in versions)
                {
                    if (Comparator.LessThanOrEqual(version.MinDotnetCore, frameworkVersion))
                    {
                        return version;
                    }
                }
            }

            throw new RuntimeException($"There is no version of Bucket available for your framework version ({Platform.GetRuntimeInfo()})");
        }

        private string GetHighestFrameworkVersion()
        {
            var frameworkMapping = new Dictionary<string, string>()
            {
                { ".NETCoreApp", @"\.NETCore\.App" },
            };

            var runtimeFrameworkInfo = Platform.GetRuntimeInfo();
            var matchRuntime = Regex.Match(runtimeFrameworkInfo, @"^(?<framework>.*),Version=v?(?<version>(.?\d*)+)$", RegexOptions.IgnoreCase);

            if (!matchRuntime.Success)
            {
                throw new UnexpectedException($"Failed to parse frame version information: {runtimeFrameworkInfo}");
            }

            if (process.Execute("dotnet --list-runtimes", out string stdout) != 0
                || !frameworkMapping.TryGetValue(matchRuntime.Groups["framework"].Value, out string framework))
            {
                // if unable to get all version information through the dotnet command,
                // then downgrade, get the framework version information of the runtime.
                return matchRuntime.Groups["version"].Value;
            }

            var matches = Regex.Matches(
                                stdout,
                                $"^\\s*Microsoft{framework}\\s(?<version>(.?\\d+)+?)\\s.*$",
                                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var candidate = new List<string>();
            foreach (Match match in matches)
            {
                candidate.Add(match.Groups["version"].Value);
            }

            if (candidate.Count <= 0)
            {
                return matchRuntime.Groups["version"].Value;
            }

            return Semver.Semver.Sort(candidate, true)[0];
        }

        private class ConfigVersions : Dictionary<Channel, ConfigVersion[]>
        {
        }

        private class ConfigVersion
        {
            [JsonProperty("version", Order = 0)]
            public string Version { get; set; }

            [JsonProperty("version-pretty", Order = 1)]
            public string VersionPretty { get; set; }

            [JsonProperty("min-dotnet-core", Order = 5)]
            public string MinDotnetCore { get; set; }

            [JsonProperty("commit-sha", Order = 10)]
            public string Sha { get; set; }

            [JsonProperty("path", Order = 10)]
            public string Path { get; set; }

            public static implicit operator BucketVersion(ConfigVersion config)
            {
                return new BucketVersion()
                {
                    Version = config.Version.Trim(),
                    VersionPretty = config.VersionPretty?.Trim(),
                    Path = config.Path?.Trim(),
                    Sha = config.Sha?.Trim(),
                };
            }
        }
    }
}

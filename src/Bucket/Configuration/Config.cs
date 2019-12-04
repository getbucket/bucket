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

#pragma warning disable CA1716

using Bucket.Exception;
using Bucket.Util;
using GameBox.Console.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a configuration file.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Indicates the default repository domain.
        /// </summary>
        public const string DefaultRepositoryDomain = "gxpack.org";

        /// <summary>
        /// Indicates the default repository uri.
        /// </summary>
        public const string DefaultRepositoryUri = "https?://repo." + DefaultRepositoryDomain;

        private static readonly IDictionary<string, Type> RepositoryFactory = new Dictionary<string, Type>
        {
            { "bucket", typeof(ConfigRepositoryBucket) },
            { "package", typeof(ConfigRepositoryPackage) },
            { "vcs", typeof(ConfigRepositoryVcs) },
            { "git", typeof(ConfigRepositoryVcs) },
            { "github", typeof(ConfigRepositoryGithub) },
            { "gitlab", typeof(ConfigRepositoryVcs) },
        };

        private readonly JObject settings = new JObject()
        {
            { Settings.CacheDir, "{$home}/cache" },
            { Settings.CacheRepoDir, "{$cache-dir}/repo" },
            { Settings.CacheVcsDir, "{$cache-dir}/vcs" },
            { Settings.CacheFilesDir, "{$cache-dir}/files" },
            { Settings.VendorDir, "vendor" },
            { Settings.BinDir, "{$vendor-dir}/bin" },
            { Settings.BackupDir, "{$home}/backup" },
            { Settings.BinCompat, "auto" },
            { Settings.SecureHttp, true },
            { Settings.GithubProtocols, new JArray("https", "ssh", "git") },
            { Settings.GithubDomains, new JArray("github.com") },
            { Settings.GitlabDomains, new JArray("gitlab.com") },
            { Settings.StoreAuth, "prompt" },
            { Settings.DiscardChanges, false },
            { Settings.PreferredInstall, "auto" },
            { Settings.SortPackages, false },
            { Settings.CacheTTL, 15552000 }, // 6 month
            { Settings.CacheFilesTTL, null },
            { Settings.CacheFilesMaxSize, "300MiB" },

            // Unsetting:
            // - home
            // - github-oauth
            // - gitlab-oauth
            // - gitlab-token
            // - http-basic
        };

        private readonly JArray repositories = new JArray(
                new JObject()
                {
                    { "name", DefaultRepositoryDomain },
                    { "type", "bucket" },
                    { "url", DefaultRepositoryUri },
                    { "allow-ssl-downgrade", true },
                });

        private readonly ISet<string> disabledRepositories;
        private readonly bool useEnvironment;
        private readonly string root;
        private IConfigSource configSourceBucket;
        private IConfigSource configSourceAuth;
        private ConfigRepository[] repositoriesCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        /// <param name="useEnvironment">Whether is use environment to replace config setting.</param>
        /// <param name="root">Optional root directory of the config.</param>
        public Config(bool useEnvironment = true, string root = null)
        {
            this.useEnvironment = useEnvironment;
            this.root = root;
            disabledRepositories = new HashSet<string>();
            repositoriesCache = null;
        }

#pragma warning disable CA2225
        public static implicit operator bool(Config config)
#pragma warning restore CA2225
        {
            return config != null;
        }

#pragma warning disable CA2225
        public static implicit operator JObject(Config config)
#pragma warning restore CA2225
        {
            var clonedSettings = (JObject)config.settings.DeepClone();
            var clonedRepositories = (JArray)config.repositories.DeepClone();
            clonedSettings["repositories"] = clonedRepositories;
            return clonedSettings;
        }

        /// <summary>
        /// Merges new config values with the existing ones (overriding).
        /// </summary>
        /// <param name="config">The config object.</param>
        public void Merge(JObject config)
        {
            if (config.ContainsKey("config") && config["config"].Type == JTokenType.Object)
            {
                foreach (var item in (JObject)config["config"])
                {
                    settings[item.Key] = item.Value;
                }
            }

            if (config.ContainsKey("repositories") && config["repositories"].Type == JTokenType.Array)
            {
                // Push in and out to ensure consistent order.
                var stack = new Stack<JObject>();
                foreach (var item in (JArray)config["repositories"])
                {
                    stack.Push((JObject)item);
                }

                while (stack.Count > 0)
                {
                    var repository = stack.Pop();

                    if (repository.ContainsKey("type"))
                    {
                        repositories.AddFirst(repository);
                        continue;
                    }

                    foreach (var disabledItem in repository)
                    {
                        if (disabledItem.Value.Type == JTokenType.Boolean
                            && !disabledItem.Value.Value<bool>())
                        {
                            disabledRepositories.Add(disabledItem.Key);
                        }
                    }
                }

                repositoriesCache = null;
            }
        }

        /// <summary>
        /// Gets a setting.
        /// Must be able to get a value or throw an exception.
        /// </summary>
        /// <param name="key">The setting name. detail see:<see cref="Settings"/>.</param>
        /// <param name="options">Get the options to use when setting.</param>
        /// <returns>Returns a setting value. null if the value not found.</returns>
        public virtual Mixture Get(string key, ConfigOptions options = ConfigOptions.None)
        {
            switch (key)
            {
                case Settings.CacheDir:
                case Settings.CacheRepoDir:
                case Settings.CacheVcsDir:
                case Settings.CacheFilesDir:
                case Settings.VendorDir:
                case Settings.BinDir:
                case Settings.BackupDir:
                    var value = GetWithEnvironmentPathOverride(key, options);
                    return Is(options, ConfigOptions.RelativePath) ? value : AbsolutePath(value);
                case Settings.BinCompat:
                    return GetBinCompat(key);
                case Settings.Home:
                    return GetHomeDirectory(key, options);
                case Settings.SecureHttp:
                    return GetBooleanValue(key);
                case Settings.GithubProtocols:
                    return GetGithubProtocols(key, options);
                case Settings.DiscardChanges:
                    return GetDiscardChanges(key);
                case Settings.GithubDomains:
                case Settings.GitlabDomains:
                    return GetArrayStringValue(key);
                case Settings.StoreAuth:
                    return GetStringValue(key)?.ToLower();
                case Settings.PreferredInstall:
                    return GetStringValue(key)?.ToLower();
                case Settings.CacheTTL:
                    return GetIntValue(key);
                case Settings.CacheFilesTTL:
                    return GetIntValue(key) ?? GetIntValue(Settings.CacheTTL);
                case Settings.CacheFilesMaxSize:
                    return ParseSizeUnit(key);
                default:
                    return GetAutoTypeValue(key, options);
            }
        }

        /// <summary>
        /// Gets a complex configuration.
        /// </summary>
        /// <typeparam name="T">The type of the complex configuration type.</typeparam>
        /// <param name="key">The setting name. detail see:<see cref="Settings"/>.</param>
        /// <param name="options">Get the options to use when setting.</param>
        /// <returns>Returns a struct of the configuration. null if the value not found.</returns>
        public virtual T Get<T>(string key, ConfigOptions options = ConfigOptions.None)
        {
            if (!settings.TryGetValue(key, out JToken token))
            {
                return default;
            }

            return token.ToObject<T>();
        }

        /// <summary>
        /// Gets an array of repository config.
        /// </summary>
        /// <returns>Returns an array of repository config.</returns>
        public virtual ConfigRepository[] GetRepositories()
        {
            if (repositoriesCache != null)
            {
                return repositoriesCache;
            }

            var result = new ConfigRepository[repositories.Count];
            var index = 0;
            foreach (var item in repositories)
            {
                var repository = (JObject)item;
                if (repository.ContainsKey("name")
                    && disabledRepositories.Contains(repository["name"].Value<string>()))
                {
                    continue;
                }

                var repositoryType = repository["type"].Value<string>();
                result[index++] = (ConfigRepository)repository.ToObject(GetRepositoryConfiguration(repositoryType.ToLower()));
            }

            Array.Resize(ref result, index);
            return repositoriesCache = result;
        }

        /// <summary>
        /// Whether has specified setting.
        /// </summary>
        /// <param name="key">The setting name.</param>
        /// <returns>True if the setting exists.</returns>
        public bool Has(string key)
        {
            if (settings.ContainsKey(key))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the <see cref="IConfigSource"/> source object.
        /// </summary>
        /// <remarks>The impact on this object will be written to the file.</remarks>
        public virtual void SetSourceBucket(IConfigSource source)
        {
            configSourceBucket = source;
        }

        /// <summary>
        /// Gets the <see cref="IConfigSource"/> source object.
        /// </summary>
        public virtual IConfigSource GetSourceBucket()
        {
            return configSourceBucket;
        }

        /// <summary>
        /// Sets the <see cref="IConfigSource"/> source object with auth file.
        /// </summary>
        /// <remarks>The impact on this object will be written to the file.</remarks>
        public virtual void SetSourceAuth(IConfigSource source)
        {
            configSourceAuth = source;
        }

        /// <summary>
        /// Gets the <see cref="IConfigSource"/> source object.
        /// </summary>
        public virtual IConfigSource GetSourceAuth()
        {
            return configSourceAuth;
        }

        /// <summary>
        /// Gets the repository configuration type.
        /// </summary>
        /// <param name="repositoryType">A string representation type of repository.</param>
        internal static Type GetRepositoryConfiguration(string repositoryType)
        {
            if (!RepositoryFactory.TryGetValue(repositoryType.ToLower(), out Type ret))
            {
                throw new ConfigException(
                    $"Unable to build a repository with the specified configuration, type \"{repositoryType}\" is unrecognizable.");
            }

            return ret;
        }

        private static bool Is(ConfigOptions options, ConfigOptions expected)
        {
            return (options & expected) == expected;
        }

        private string GetStringValue(string key)
        {
            if (!settings.TryGetValue(key, out JToken token) || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (!new[] { JTokenType.String, JTokenType.Integer, JTokenType.Boolean, JTokenType.Float }
                    .Contains(token.Type))
            {
                throw new ConfigException($"Setting \"{key}\" must be a string value.");
            }

            return token.Value<string>();
        }

        private bool? GetBooleanValue(string key)
        {
            if (!settings.TryGetValue(key, out JToken token) || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token.Type == JTokenType.Integer)
            {
                return token.Value<int>() != 0;
            }

            if (!new[] { JTokenType.Boolean }
                    .Contains(token.Type))
            {
                throw new ConfigException($"Setting \"{key}\" must be a boolean value.");
            }

            return token.Value<bool>();
        }

        private int? GetIntValue(string key)
        {
            if (!settings.TryGetValue(key, out JToken token) || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (!new[] { JTokenType.Integer }
                    .Contains(token.Type))
            {
                throw new ConfigException($"Setting \"{key}\" must be a integer value.");
            }

            return token.Value<int>();
        }

        private float? GetFloatValue(string key)
        {
            if (!settings.TryGetValue(key, out JToken token) || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (!new[] { JTokenType.Float }
                    .Contains(token.Type))
            {
                throw new ConfigException($"Setting \"{key}\" must be a float value.");
            }

            return token.Value<float>();
        }

        private string[] GetArrayStringValue(string key)
        {
            if (!settings.TryGetValue(key, out JToken token) || token.Type == JTokenType.Null)
            {
#pragma warning disable S1168
                return null;
#pragma warning restore S1168
            }

            if (token.Type == JTokenType.String)
            {
                return new[] { token.Value<string>() };
            }
            else if (token.Type != JTokenType.Array)
            {
                throw new ConfigException($"Setting \"{key}\" must be a array value.");
            }

            return Arr.Map(token, (subToken) => subToken.Value<string>());
        }

        private Mixture GetAutoTypeValue(string key, ConfigOptions options)
        {
            if (!settings.TryGetValue(key, out JToken token) || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token.Type == JTokenType.String)
            {
                return Process(GetStringValue(key), options);
            }

            if (token.Type == JTokenType.Array)
            {
                return GetArrayStringValue(key);
            }

            if (token.Type == JTokenType.Boolean)
            {
                return GetBooleanValue(key);
            }

            if (token.Type == JTokenType.Integer)
            {
                return GetIntValue(key);
            }

            if (token.Type == JTokenType.Float)
            {
                return GetFloatValue(key);
            }

            throw new ConfigException($"Setting \"{key}\" type must be string, int, bool, float, string[] value.");
        }

        private string GetWithEnvironmentPathOverride(string key, ConfigOptions options)
        {
            var setting = GetStringValue(key);

            var environment = GetBucketEnvironment(key.ToUpper().Replace("-", "_"));

            setting = Process(environment ?? setting, options);
            setting = Platform.ExpandPath(setting);

            return setting.TrimEnd('/', '\\');
        }

        private string GetHomeDirectory(string key, ConfigOptions options)
        {
            var candidate = Terminal.GetEnvironmentVariable("HOME") ?? Terminal.GetEnvironmentVariable("USERPROFILE");
            if (candidate is null)
            {
                throw new ConfigException($"Setting {key} unable to get from environment variable: HOME or USERPROFILE.");
            }

            var home = ((string)candidate).Trim();
            var setting = string.Empty;
            if (settings.TryGetValue(key, out JToken token))
            {
                setting = token.Value<string>();
            }

            // Maybe contain variables: $HOME$, $HOME/, ~/, ~$
            // We will regard it as the root directory.
            home = Path.Combine(Regex.Replace(home, @"^(\$HOME|~)(/|$)", "/\\"), setting);
            home = Process(home, options);

            if (home.StartsWith("/\\", StringComparison.Ordinal))
            {
                return home.Substring(2);
            }

            return home;
        }

        private string[] GetGithubProtocols(string key, ConfigOptions options)
        {
            var protocols = GetArrayStringValue(key);

            if (Get(Settings.SecureHttp, options))
            {
                var index = Array.FindIndex(protocols, (protocol) => protocol == "git");
                if (index != -1)
                {
                    Arr.RemoveAt(ref protocols, index);
                }
            }

            if (protocols.Length <= 0)
            {
                throw new ConfigException("No protocol for github is not available anymore.");
            }
            else if (protocols[0] == "http")
            {
                throw new ConfigException("The http protocol for github is not available anymore, update your config's github-protocols to use \"https\", \"git\" or \"ssh\"");
            }

            return protocols;
        }

        private string GetDiscardChanges(string key)
        {
            var needles = new Dictionary<string, string>()
            {
                { "true", "true" },
                { "false", "false" },
                { "1", "true" },
                { "0", "false" },
                { "stash", "stash" },
            };

            string AssertNeedles(string value)
            {
                value = value.Trim().ToLower();
                if (!needles.TryGetValue(value, out string ret))
                {
                    throw new ConfigException($"Invalid value for {EnvironmentVariables.BucketDiscardChanges}: {value}. Expected 1, 0, true, false or stash.");
                }

                return ret;
            }

            var env = GetBucketEnvironment(EnvironmentVariables.BucketDiscardChanges);
            if (!(env is null))
            {
                return AssertNeedles(env);
            }

            return AssertNeedles(GetStringValue(key));
        }

        private string GetBinCompat(string key)
        {
            var value = GetBucketEnvironment(EnvironmentVariables.BucketDiscardChanges) ?? GetStringValue(key);
            if (value is null ||
                !Array.Exists(new[] { "auto", "full" }, (expected) => expected == value.ToString().ToLower()))
            {
                throw new ConfigException($"Invalid value for \"{Settings.BinCompat}\": {value}. Expected auto, full.");
            }

            return value.ToString().ToLower();
        }

        private string AbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(root) || Path.IsPathRooted(path))
            {
                return path;
            }

            // Windows supports either the forward slash or the backslash
            // Unix-based systems support only the forward slash.
            return root + Path.AltDirectorySeparatorChar + path;
        }

        private Mixture Process(Mixture value, ConfigOptions options)
        {
            if (value is null || !value.IsString)
            {
                return value;
            }

            return Regex.Replace(value, @"{\$(?<key>.+)}", (match) =>
            {
                return Get(match.Groups["key"].Value, options);
            });
        }

        private Mixture GetBucketEnvironment(string env)
        {
            if (!useEnvironment)
            {
                return null;
            }

            if (!env.StartsWith("BUCKET_", StringComparison.Ordinal))
            {
                env = $"BUCKET_{env}";
            }

            return Terminal.GetEnvironmentVariable(env);
        }

        private int ParseSizeUnit(string key)
        {
            var value = GetStringValue(key);
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var match = Regex.Match(value, @"^\s*(?<size>[0-9.]+)\s*(?:(?<unit>[kmg])(?:i?b)?)?\s*$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new ConfigException($"Could not parse the value of '{key}': {value}");
            }

            var size = int.Parse(match.Groups["size"].Value);
            var unit = match.Groups["unit"].Value;

            if (string.IsNullOrEmpty(unit))
            {
                return size;
            }

            switch (unit.ToLower())
            {
                case "g":
                    return size * 1024 * 1024 * 1024;
                case "m":
                    return size * 1024 * 1024;
                case "k":
                    return size * 1024;
                default:
                    throw new UnexpectedException($"Unit failed to be identified: {unit}");
            }
        }
    }
}

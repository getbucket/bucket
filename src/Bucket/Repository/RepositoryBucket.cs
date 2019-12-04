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
using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.IO;
using Bucket.Json;
using Bucket.Package;
using Bucket.Package.Loader;
using Bucket.Plugin;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Bucket.Util;
using GameBox.Console.EventDispatcher;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using BVersionParser = Bucket.Package.Version.VersionParser;
using SException = System.Exception;

namespace Bucket.Repository
{
    /// <summary>
    /// Represents bucket repository.
    /// </summary>
    public class RepositoryBucket : RepositoryArray, ILazyload
    {
        private readonly ConfigRepositoryBucket configRepository;
        private readonly IIO io;
        private readonly IEventDispatcher eventDispatcher;
        private readonly IVersionParser versionParser;
        private readonly ITransport transport;
        private readonly ICache cache;
        private readonly ILoaderPackage loader;
        private readonly bool uriIsIntelligent;
        private readonly IDictionary<int, IPackage> providersByUid;
        private IDictionary<string, ConfigMetadata> providers;
        private string uri;
        private string baseUri;
        private bool hasProviders;
        private ConfigRootData rootData;
        private bool degradedMode;
        private string notifyUri;
        private string searchUri;
        private string providersUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryBucket"/> class.
        /// </summary>
        public RepositoryBucket(
            ConfigRepositoryBucket configRepository,
            IIO io,
            Config config,
            ITransport transport = null,
            IEventDispatcher eventDispatcher = null,
            IVersionParser versionParser = null)
        {
            if (!Regex.IsMatch(configRepository.Uri, @"^[\w.]+\??://"))
            {
                // assume http as the default protocol
                configRepository.Uri = $"http://{configRepository.Uri}";
            }

            configRepository.Uri = configRepository.Uri.TrimEnd('/');

            if (configRepository.Uri.StartsWith("https?", StringComparison.OrdinalIgnoreCase))
            {
                configRepository.Uri = $"https{configRepository.Uri.Substring(6)}";
                uriIsIntelligent = true;
            }

            if (!Uri.TryCreate(configRepository.Uri, UriKind.RelativeOrAbsolute, out _))
            {
                throw new ConfigException($"Invalid url given for Bucket repository: {configRepository.Uri}");
            }

            // changes will not be applied to configRepository.
            uri = configRepository.Uri;

            // force url for gxpack.org to repo.gxpack.org
            // without converting other addresses.
            var match = Regex.Match(configRepository.Uri, @"^(?<proto>https?)://gxpack\.org/?$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                uri = $"{match.Groups["proto"].Value}://repo.gxpack.org";
            }

            baseUri = Regex.Replace(uri, @"(?:/[^/\\\\]+\.json)?(?:[?#].*)?$", string.Empty).TrimEnd('/');

            this.configRepository = configRepository;
            this.io = io;
            this.transport = transport ?? new TransportHttp(io, config);
            this.eventDispatcher = eventDispatcher;
            this.versionParser = versionParser ?? new BVersionParser();

            var cacheDir = Path.Combine(config.Get(Settings.CacheDir), CacheFileSystem.FormatCacheFolder(uri));
            cache = new CacheFileSystem(cacheDir, io, "a-z0-9.$~");
            loader = new LoaderPackage(versionParser);
            providersByUid = new Dictionary<int, IPackage>();
        }

        /// <inheritdoc />
        public bool IsLazyLoad => HasProviders();

        /// <inheritdoc />
        public override IPackage FindPackage(string name, IConstraint constraint)
        {
            name = name.ToLower();
            if (!HasProviders())
            {
                return base.FindPackage(name, constraint);
            }

            if (GetProviderNames().Contains(name))
            {
                return FilterPackages(WhatProvides(name), constraint, true).FirstOrDefault();
            }

            return base.FindPackage(name, constraint);
        }

        /// <inheritdoc />
        public override IPackage[] FindPackages(string name, IConstraint constraint = null)
        {
            name = name.ToLower();
            if (!HasProviders())
            {
                return base.FindPackages(name, constraint);
            }

            if (GetProviderNames().Contains(name))
            {
                return FilterPackages(WhatProvides(name), constraint).ToArray();
            }

            return base.FindPackages(name, constraint);
        }

        /// <inheritdoc />
        public override IPackage[] GetPackages()
        {
            if (HasProviders())
            {
                throw new UnexpectedException($"Bucket repositories that have providers can not load the complete list of packages, use {nameof(GetProviderNames)} instead.");
            }

            return base.GetPackages();
        }

        /// <inheritdoc />
        public override SearchResult[] Search(string query, SearchMode mode = SearchMode.Fulltext, string type = null)
        {
#pragma warning disable S1117
            var hasProviders = HasProviders();
#pragma warning restore S1117

            if (!string.IsNullOrEmpty(searchUri) && mode == SearchMode.Fulltext)
            {
                var requestUri = searchUri.Replace("%query%", query).Replace("%type%", type);
                var search = JsonFile.Parse<ConfigSearchResults>(transport.GetString(requestUri));
                if (search.Results == null || search.Results.Count <= 0)
                {
                    return Array.Empty<SearchResult>();
                }

                var results = new List<SearchResult>(search.Results.Count);
                foreach (var packageConfig in search.Results)
                {
                    if (packageConfig.Virtual)
                    {
                        continue;
                    }

                    var package = loader.Load(packageConfig, typeof(IPackageComplete));
                    results.Add(new SearchResult(package));
                }

                return results.ToArray();
            }

            if (hasProviders)
            {
                var results = new List<SearchResult>();
                var regex = $"(?:{string.Join("|", Regex.Split(query, @"\s+"))})";

                foreach (var name in GetProviderNames())
                {
                    if (Regex.IsMatch(name, regex))
                    {
                        results.Add(new SearchResult(name));
                    }
                }

                return results.ToArray();
            }

            return base.Search(query, mode, type);
        }

        /// <inheritdoc />
        public IPackage[] WhatProvides(string name, PredicatePackageAcceptable predicatePackageAcceptable = null)
        {
            // skip platform packages, root package and bucket-plugin-api
            if (Regex.IsMatch(name, RepositoryPlatform.RegexPlatform)
                || name == ConfigBucketBase.RootPackage
                || name == PluginManager.PluginRequire)
            {
                return Array.Empty<IPackage>();
            }

            AssertLoadProviderMapping();

            if (string.IsNullOrEmpty(providersUri))
            {
                return Array.Empty<IPackage>();
            }

            // package does not exist in this repo
            if (!providers.TryGetValue(name, out ConfigMetadata metadata))
            {
                return Array.Empty<IPackage>();
            }

            var hash = metadata["sha256"];
            var requestUri = providersUri.Replace("%package%", name).Replace("%hash%", hash);
            var cacheKey = $"provider-{name.Replace('/', '$')}.json";

            ConfigPackages data = null;
            if (!string.IsNullOrEmpty(hash) && cache.TryReadSha256(cacheKey, out string content, hash))
            {
                data = JsonFile.Parse<ConfigPackages>(content);
            }

            if (!data)
            {
                data = FetchFile<ConfigPackages>(requestUri, cacheKey, hash);
            }

            var result = new Dictionary<string, IPackage>();
            var versionsToLoad = new Dictionary<int, ConfigPackageBucket>();
            foreach (var versions in data.Packages)
            {
                foreach (var version in versions.Value)
                {
                    var packageConfig = version.Value;
                    var uid = packageConfig.Uid;
                    var normalizedName = packageConfig.Name.ToLower();

                    // only load the actual named package, not other packages
                    // that might find themselves in the same file
                    if (normalizedName != name)
                    {
                        continue;
                    }

                    if (versionsToLoad.ContainsKey(uid))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(packageConfig.VersionNormalized))
                    {
                        packageConfig.VersionNormalized = versionParser.Normalize(packageConfig.Version);
                    }

                    if (IsVersionAcceptable(predicatePackageAcceptable, null, normalizedName, packageConfig))
                    {
                        versionsToLoad[uid] = packageConfig;
                    }
                }
            }

            // load acceptable packages in the providers
            var loadedPackages = CreatePackages(versionsToLoad.Values, typeof(IPackageComplete));
            var uids = versionsToLoad.Keys.ToArray();

            var index = 0;
            foreach (var package in loadedPackages)
            {
                package.SetRepository(this);
                var uid = uids[index++];

                if (package is PackageAlias packageAlias)
                {
                    var aliased = packageAlias.GetAliasOf();
                    aliased.SetRepository(this);
                    result[uid.ToString()] = aliased;
                    result[$"{uid}-alias"] = package;
                }
                else
                {
                    result[uid.ToString()] = package;
                }
            }

            return result.Values.ToArray();
        }

        /// <inheritdoc />
        public ICollection<string> GetProviderNames()
        {
            AssertLoadProviderMapping();

            // todo: Lists should not be provided for lazy loading
            // when lazy loading is provided in the future.
            if (!string.IsNullOrEmpty(providersUri))
            {
                return providers.Keys;
            }

            return Array.Empty<string>();
        }

        internal string NormalizeUri(string uri)
        {
            if (string.IsNullOrEmpty(uri) || uri[0] != '/')
            {
                return uri;
            }

            var matched = Regex.Match(this.uri, @"^(?:[^:]+)+://(?:[^/]*)+");
            if (matched.Success)
            {
                return matched.Groups[0] + uri;
            }

            return this.uri;
        }

        /// <summary>
        /// Initializes <see cref="LoadRootServerFile"/> which is needed for the rest below to work.
        /// </summary>
        protected bool HasProviders()
        {
            LoadRootServerFile();
            return hasProviders;
        }

        /// <summary>
        /// Fetch the specified file and convert it to the corresponding data structure.
        /// </summary>
        protected T FetchFile<T>(string filename, string cacheKey = null, string sha256 = null)
        {
            if (string.IsNullOrEmpty(cacheKey))
            {
                cacheKey = filename;
                filename = $"{baseUri}/{filename}";
            }

            // url-encode $ signs in URLs as bad proxies choke on them
            var pos = filename.IndexOf('$');
            if (pos != -1 && Regex.IsMatch(filename, "^https?://.*", RegexOptions.IgnoreCase))
            {
                filename = $"{filename.Substring(0, pos)}%24{filename.Substring(pos + 1)}";
            }

            var allowUseHttp = uriIsIntelligent;
            void IntelligentAdjustmentSSL()
            {
                if (allowUseHttp)
                {
                    // Ssl is used by default, if it fails, we allow retry and then try again.
                    uri = DowngradeSSL(uri);
                    baseUri = DowngradeSSL(baseUri);
                    filename = DowngradeSSL(filename);
                    allowUseHttp = false;
                }
                else if (configRepository.AllowSSLDowngrade)
                {
                    // undo downgrade before trying again if http seems to be hijacked or
                    // modifying content somehow.
                    uri = UpgradeSSL(uri);
                    baseUri = UpgradeSSL(baseUri);
                    filename = UpgradeSSL(filename);
                }
            }

            var retries = 3;
            while (retries-- > 0)
            {
                try
                {
                    if (eventDispatcher != null)
                    {
                        var preFileDownloadEvent = new PreFileDownloadEventArgs(PluginEvents.PreFileDownload, transport, filename);
                        eventDispatcher.Dispatch(this, preFileDownloadEvent);
                    }

                    var jsonBody = transport.GetString(filename);
                    if (!string.IsNullOrEmpty(sha256) && sha256 != Security.Sha256(jsonBody))
                    {
                        IntelligentAdjustmentSSL();

                        if (retries > 0)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        throw new RepositorySecurityException($"The contents of {filename} do not match its signature. This could indicate a man-in-the-middle attack. Please report the error immediately.");
                    }

                    cache.Write(cacheKey, jsonBody);
                    return JsonFile.Parse<T>(jsonBody);
                }
                catch (SException ex)
                {
                    if (ex is TransportException transportException)
                    {
                        if (transportException.HttpStatusCode == HttpStatusCode.NotFound)
                        {
                            throw;
                        }

                        // When the http status code is not recognized, this
                        // means the server may not be able to connect.
                        if (transportException.HttpStatusCode == 0)
                        {
                            IntelligentAdjustmentSSL();
                        }
                    }

                    // todo: logic exception need throw exception immediately.
                    if (retries > 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    if (ex is RepositorySecurityException)
                    {
                        throw;
                    }

                    if (string.IsNullOrEmpty(cacheKey))
                    {
                        throw;
                    }

                    if (!cache.TryReadSha256(cacheKey, out string content) || string.IsNullOrEmpty(content))
                    {
                        throw;
                    }

                    if (!degradedMode)
                    {
                        io.WriteError($"<warning>{ex.Message}</warning>");
                        io.WriteError($"<warning>{uri} could not be fully loaded, package information was loaded from the local cache and may be out of date</warning>");
                    }

                    degradedMode = true;
                    return JsonFile.Parse<T>(content);
                }
            }

            throw new UnexpectedException("Unexpectedly, this code should not be executed.");
        }

        private static string UpgradeSSL(string origin)
        {
            return origin.Replace("http://", "https://");
        }

        private static string DowngradeSSL(string origin)
        {
            return origin.Replace("https://", "http://");
        }

        private static bool IsVersionAcceptable(PredicatePackageAcceptable predicatePackageAcceptable, IConstraint constraint, string name, ConfigPackageBucket config)
        {
            // todo: to load aliase package.
            var versions = new[] { config.VersionNormalized };

            foreach (var version in versions)
            {
                if (predicatePackageAcceptable != null
                    && !predicatePackageAcceptable(VersionParser.ParseStability(version), name))
                {
                    continue;
                }

                if (constraint != null && !constraint.Matches(new Constraint("==", version)))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Read server root server file.
        /// </summary>
        private ConfigRootData LoadRootServerFile()
        {
            if (rootData != null)
            {
                return rootData;
            }

            var jsonUriInstance = new Uri(uri);
            var jsonUri = jsonUriInstance.AbsoluteUri.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? uri : $"{uri}/packages.json";

            var data = FetchFile<ConfigRootData>(jsonUri, "packages.json");

            if (!string.IsNullOrEmpty(data.Notify))
            {
                notifyUri = NormalizeUri(data.Notify);
            }

            if (!string.IsNullOrEmpty(data.Search))
            {
                searchUri = NormalizeUri(data.Search);
            }

            if (configRepository.AllowSSLDowngrade)
            {
                uri = DowngradeSSL(uri);
                baseUri = DowngradeSSL(baseUri);
            }

            // todo: add mirrors support.
            if (!string.IsNullOrEmpty(data.ProvidersUri))
            {
                providersUri = NormalizeUri(data.ProvidersUri);
                hasProviders = true;
            }

            if (data.ProviderIncludes != null && data.ProviderIncludes.Count > 0)
            {
                hasProviders = true;
            }

            return rootData = data;
        }

        private IPackage[] CreatePackages(IEnumerable<ConfigPackageBucket> packages, Type expectedClass)
        {
            var result = new List<IPackage>();
            foreach (var packageConfig in packages)
            {
                try
                {
                    if (providersByUid.TryGetValue(packageConfig.Uid, out IPackage package))
                    {
                        result.Add(package);
                        continue;
                    }

                    if (string.IsNullOrEmpty(packageConfig.NotificationUri))
                    {
                        packageConfig.NotificationUri = notifyUri;
                    }

                    // todo: Maybe performance optimization can be done
                    // to avoid increasing the efficiency of link generation.
                    package = loader.Load(packageConfig, expectedClass);

                    // todo: add mirrors support.
                    result.Add(package);
                    providersByUid.Add(packageConfig.Uid, package);
                }
#pragma warning disable CA1031
                catch (SException ex)
#pragma warning restore CA1031
                {
                    throw new RuntimeException($"Could not load packages {packageConfig.Name} in {uri}: [{ex}] {ex.Message}", ex);
                }
            }

            return result.ToArray();
        }

        private IEnumerable<IPackage> FilterPackages(IEnumerable<IPackage> packages, IConstraint constraint = null, bool returnFirstMatch = false)
        {
            if (constraint == null)
            {
                foreach (var package in packages)
                {
                    yield return package;
                    if (returnFirstMatch)
                    {
                        yield break;
                    }
                }

                yield break;
            }

            foreach (var package in packages)
            {
                var packageConstraint = new Constraint("==", package.GetVersion());

                if (!constraint.Matches(packageConstraint))
                {
                    continue;
                }

                yield return package;
                if (returnFirstMatch)
                {
                    yield break;
                }
            }
        }

        private void LoadProviderMapping(ConfigProviderData data)
        {
            Guard.Requires<UnexpectedException>(data != null, "Param ConfigProviderData should not be null.");

            if (providers == null)
            {
                providers = new Dictionary<string, ConfigMetadata>();
            }

            if (data.Providers != null)
            {
                foreach (var item in data.Providers)
                {
                    providers[item.Key] = item.Value;
                }
            }

            if (string.IsNullOrEmpty(providersUri) || data.ProviderIncludes == null)
            {
                return;
            }

            foreach (var item in data.ProviderIncludes)
            {
                var include = item.Key;
                var metadata = item.Value;

                if (!metadata.TryGetValue("sha256", out string hash))
                {
                    throw new RepositorySecurityException("The repository must provide a checksum of sha256.");
                }

#pragma warning disable S1075
                var requestUri = baseUri + "/" + include.Replace("%hash%", hash).Trim('/');
#pragma warning restore S1075
                var cacheKey = include.Replace("%hash%", string.Empty).Replace("$", string.Empty);

                ConfigProviderData includedData;
                if (cache.TryReadSha256(cacheKey, out string content, hash))
                {
                    includedData = JsonFile.Parse<ConfigProviderData>(content);
                }
                else
                {
                    includedData = FetchFile<ConfigProviderData>(requestUri, cacheKey, hash);
                }

                LoadProviderMapping(includedData);
            }
        }

        private void AssertLoadProviderMapping()
        {
            if (providers == null)
            {
                LoadProviderMapping(LoadRootServerFile());
            }
        }

        private sealed class ConfigRootData
        {
            [JsonProperty("notify")]
            public string Notify { get; set; }

            [JsonProperty("search")]
            public string Search { get; set; }

            [JsonProperty("providers-url")]
            public string ProvidersUri { get; set; }

            [JsonProperty("provider-includes")]
            public IDictionary<string, ConfigMetadata> ProviderIncludes { get; set; }

            public static implicit operator ConfigProviderData(ConfigRootData data)
            {
                return new ConfigProviderData
                {
                    ProviderIncludes = data.ProviderIncludes,
                };
            }
        }

        private sealed class ConfigProviderData
        {
            [JsonProperty("providers")]
            public IDictionary<string, ConfigMetadata> Providers { get; set; }

            [JsonProperty("provider-includes")]
            public IDictionary<string, ConfigMetadata> ProviderIncludes { get; set; }
        }

        private sealed class ConfigPackages
        {
            [JsonProperty("packages")]
            public IDictionary<string, ConfigVersions> Packages { get; set; }

            public static implicit operator bool(ConfigPackages data)
            {
                return data != null;
            }
        }

        private sealed class ConfigMetadata : Dictionary<string, string>
        {
        }

        private sealed class ConfigPackageBucket : ConfigBucketBase
        {
            [JsonProperty("uid")]
            public int Uid { get; set; }

            /// <inheritdoc />
            public override bool ShouldDeserializeSource()
            {
                return true;
            }

            /// <inheritdoc />
            public override bool ShouldDeserializeDist()
            {
                return true;
            }

            /// <inheritdoc />
            public override bool ShouldSerializeSource()
            {
                return true;
            }

            /// <inheritdoc />
            public override bool ShouldSerializeDist()
            {
                return true;
            }
        }

        private sealed class ConfigSearchResult : ConfigBucketBase
        {
            [JsonProperty("virtual")]
            public bool Virtual { get; set; }
        }

        private sealed class ConfigSearchResults
        {
            [JsonProperty("results")]
            public IList<ConfigSearchResult> Results { get; set; }

            [JsonProperty("total")]
            public int Total { get; set; }

            [JsonProperty("next")]
            public string Next { get; set; }
        }

        private sealed class ConfigVersions : Dictionary<string, ConfigPackageBucket>
        {
        }
    }
}

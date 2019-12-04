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
using Bucket.IO;
using Bucket.Json;
using Bucket.Package;
using Bucket.Package.Loader;
using Bucket.Repository.Vcs;
using Bucket.Semver;
using GameBox.Console.Exception;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using BVersionParser = Bucket.Package.Version.VersionParser;
using SParseException = Bucket.Semver.ParseException;

namespace Bucket.Repository
{
    /// <summary>
    /// Represents a VCS repository.
    /// </summary>
    public class RepositoryVcs : RepositoryArray
    {
        private static bool registered = false;
        private readonly IDictionary<string, DriverCreater> drivers;
        private readonly IIO io;
        private readonly IVersionCache versionCache;
        private readonly ConfigRepositoryVcs configRepository;
        private readonly Config config;
        private readonly string type;
        private readonly string uri;
        private readonly bool isVerbose;
        private readonly bool isVeryVerbose;
        private readonly LinkedList<string> emptyReferences;
        private readonly IVersionParser versionParser;
        private ILoaderPackage loader;
        private IDriverVcs driverVcs;
        private string packageName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryVcs"/> class.
        /// </summary>
        /// <param name="configRepository">Represents the repository configuration file.</param>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The global config file.</param>
        /// <param name="drivers">The vcs drivers factory.</param>
        /// <param name="versionCache">The version cache implementation.</param>
        /// <param name="versionParser">The version parser instance.</param>
        public RepositoryVcs(
            ConfigRepositoryVcs configRepository,
            IIO io,
            Config config,
            IDictionary<string, DriverCreater> drivers = null,
            IVersionCache versionCache = null,
            IVersionParser versionParser = null)
        {
            this.configRepository = configRepository;
            this.config = config;
            this.io = io;
            this.versionCache = versionCache;
            this.versionParser = versionParser ?? new BVersionParser();
            this.drivers = drivers ?? CreateAndRegisterDefaultDrivers();
            emptyReferences = new LinkedList<string>();
            type = configRepository.Type;
            uri = configRepository.Uri;
            isVerbose = io.IsVerbose;
            isVeryVerbose = io.IsVeryVerbose;

            RegisterDefaultDrivers();
        }

        /// <summary>
        /// Represents a vcs driver creator.
        /// </summary>
        /// <param name="configRepository">The config repository.</param>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The global config.</param>
        /// <returns>Returns an new vcs driver creator.</returns>
        public delegate IDriverVcs DriverCreater(ConfigRepositoryVcs configRepository, IIO io, Config config);

        /// <summary>
        /// Gets a value indicating whether is there an invalid branch.
        /// </summary>
        public bool HasInvalidBranches { get; private set; }

        /// <summary>
        /// Gets the empty references in this repository.
        /// </summary>
        /// <returns>Returns an array of empty reference.</returns>
        public string[] GetEmptyReferences()
        {
            return emptyReferences.ToArray();
        }

        /// <summary>
        /// Gets the config with repository.
        /// </summary>
        /// <returns>Returns the config with repository.</returns>
        public ConfigRepository GetConfigRepository()
        {
            return configRepository;
        }

        /// <summary>
        /// Set a package loader to load package.
        /// </summary>
        /// <param name="loader">The package loader instance.</param>
        public void SetLoader(ILoaderPackage loader)
        {
            this.loader = loader;
        }

        /// <summary>
        /// Get the vcs driver. Create if the driver does not exist.
        /// </summary>
        /// <returns>Returns vcs driver instance.</returns>
        protected IDriverVcs GetDriver()
        {
            if (driverVcs != null)
            {
                return driverVcs;
            }

            if (drivers.TryGetValue(type, out DriverCreater factory))
            {
                return driverVcs = CreateDriver(factory);
            }

            // todo: bug! dictionary cannot guarantee order.
            foreach (var item in drivers)
            {
                if (DriverSupportChecker.IsSupport(item.Key, io, config, uri))
                {
                    return driverVcs = CreateDriver(item.Value);
                }
            }

            foreach (var item in drivers)
            {
                if (DriverSupportChecker.IsSupport(item.Key, io, config, uri, true))
                {
                    return driverVcs = CreateDriver(item.Value);
                }
            }

            return null;
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            var driver = GetDriver();
            if (driver == null)
            {
                throw new InvalidArgumentException($"No driver found to handle VCS repository {uri}");
            }

            if (loader == null)
            {
                loader = new LoaderPackage();
            }

            var rootIdentifier = driver.GetRootIdentifier();
            try
            {
                if (driver.HasBucketFile(rootIdentifier))
                {
                    var bucketInformation = driver.GetBucketInformation(rootIdentifier);
                    packageName = bucketInformation.Name;
                }
            }
#pragma warning disable CA1031
            catch (System.Exception ex)
#pragma warning restore CA1031
            {
                io.WriteError($"<error>Skipped parsing {rootIdentifier}, {ex.Message}</error>", verbosity: Verbosities.VeryVerbose);
            }

            foreach (var item in driver.GetTags())
            {
                var originTag = item.Key;
                var identifier = item.Value;
                var message = $"Reading {Factory.DefaultBucketFile} of <info>({packageName ?? uri.ToString()})</info> (tag:<comment>{originTag}</comment>)";

                if (isVeryVerbose)
                {
                    io.WriteError(message);
                }
                else if (isVerbose)
                {
                    io.OverwriteError(message, false);
                }

                // strip the release- prefix from tags if present.
                var tag = originTag.Trim().IndexOf("release-", StringComparison.OrdinalIgnoreCase) != 0 ?
                            originTag : originTag.Substring(8);

                var cachedPackage = GetCachedPackageVersion(tag, identifier, out bool skipped);

                if (cachedPackage != null)
                {
                    AddPackage(cachedPackage);
                    continue;
                }
                else if (skipped)
                {
                    emptyReferences.AddLast(identifier);
                    continue;
                }

                var parsedTag = ValidateTag(tag);
                if (string.IsNullOrEmpty(parsedTag))
                {
                    io.WriteError($"<warning>Skipped tag {tag}, invalid tag name.</warning>", verbosity: Verbosities.VeryVerbose);
                    continue;
                }

                try
                {
                    var bucketInformation = driver.GetBucketInformation(identifier);
                    if (bucketInformation == null)
                    {
                        io.WriteError($"<warning>Skipped tag {tag}, no {Factory.DefaultBucketFile} file.</warning>", verbosity: Verbosities.VeryVerbose);
                        emptyReferences.AddLast(identifier);
                        continue;
                    }

                    var version = bucketInformation.Version;
                    string versionNormalized;
                    if (!string.IsNullOrEmpty(version))
                    {
                        versionNormalized = versionParser.Normalize(bucketInformation.Version);
                    }
                    else
                    {
                        version = tag;
                        versionNormalized = parsedTag;
                    }

                    // make sure tag packages have no -dev flag
                    version = Regex.Replace(version, "[.-]?dev$", string.Empty, RegexOptions.IgnoreCase);
                    versionNormalized = Regex.Replace(versionNormalized, "(^dev-|[.-]?dev$)", string.Empty, RegexOptions.IgnoreCase);

                    // broken package, version doesn't match tag
                    if (versionNormalized != parsedTag)
                    {
                        io.WriteError($"<warning>Skipped tag {tag}, tag ({parsedTag}) does not match version ({versionNormalized}) in {Factory.DefaultBucketFile}</warning>", verbosity: Verbosities.VeryVerbose);
                        continue;
                    }

                    var tagPackageName = string.IsNullOrEmpty(bucketInformation.Name) ?
                        bucketInformation.Name : packageName;

                    var existingPackage = this.FindPackage(tagPackageName, versionNormalized);
                    if (existingPackage != null)
                    {
                        io.WriteError(
                            $"<warning>Skipped tag {tag}, it conflicts with an another tag ({existingPackage.GetVersionPretty()}) as both resolve to {versionNormalized} internally</warning>",
                            verbosity: Verbosities.VeryVerbose);
                        continue;
                    }

                    io.WriteError($"Importing tag {tag} ({versionNormalized})", verbosity: Verbosities.VeryVerbose);

                    bucketInformation.Version = version;
                    bucketInformation.VersionNormalized = versionNormalized;

                    AddPackage(loader.Load(PreProcess(driver, bucketInformation, identifier)));
                }
#pragma warning disable CA1031
                catch (System.Exception ex)
#pragma warning restore CA1031
                {
                    string errorMessage;
                    if (ex is TransportException transportException && transportException.HttpStatusCode == HttpStatusCode.NotFound)
                    {
                        emptyReferences.AddLast(identifier);
                        errorMessage = $"no {Factory.DefaultBucketFile} file was found";
                    }
                    else
                    {
                        errorMessage = ex.Message;
                    }

                    io.WriteError(
                        $"<warning>Skipped tag {tag}, {errorMessage}</warning>",
                        verbosity: Verbosities.VeryVerbose);

                    continue;
                }
            }

            if (!isVeryVerbose)
            {
                io.OverwriteError(string.Empty, false);
            }

            var branchs = driver.GetBranches();
            var hasMaster = branchs.ContainsKey("master");
            foreach (var item in branchs)
            {
                var branch = item.Key;
                var identifier = item.Value;
                var message = $"Reading {Factory.DefaultBucketFile} of <info>({packageName ?? uri.ToString()})</info> (branch:<comment>{branch}</comment>)";

                if (isVeryVerbose)
                {
                    io.WriteError(message);
                }
                else if (isVerbose)
                {
                    io.OverwriteError(message, false);
                }

                if (branch == "trunk" && hasMaster)
                {
                    io.WriteError(
                        $"<warning>Skipped branch {branch}, can not parse both master and trunk branches as they both resolve to {VersionParser.VersionMaster} internally</warning>",
                        verbosity: Verbosities.VeryVerbose);
                    continue;
                }

                var parsedBranch = ValidateBranch(branch);

                if (parsedBranch == null)
                {
                    io.WriteError($"<warning>Skipped branch {branch}, invalid name</warning>", verbosity: Verbosities.VeryVerbose);
                    continue;
                }

                // make sure branch packages have a dev flag
                string version;
                if ((parsedBranch.Length >= 4 && parsedBranch.Substring(0, 4) == "dev-") || parsedBranch == VersionParser.VersionMaster)
                {
                    version = $"dev-{branch}";
                }
                else
                {
                    var prefix = (branch.Length >= 1 && branch.Substring(0, 1) == "v") ? "v" : string.Empty;
                    version = prefix + Regex.Replace(parsedBranch, $"(\\.{VersionParser.VersionMax})+", ".x");
                }

                var cachedPackage = GetCachedPackageVersion(version, identifier, out bool skipped);

                if (cachedPackage != null)
                {
                    AddPackage(cachedPackage);
                    continue;
                }
                else if (skipped)
                {
                    emptyReferences.AddLast(identifier);
                    continue;
                }

                try
                {
                    var bucketInformation = driver.GetBucketInformation(identifier);
                    if (bucketInformation == null)
                    {
                        io.WriteError($"<warning>Skipped branch {branch}, no {Factory.DefaultBucketFile} file.</warning>", verbosity: Verbosities.VeryVerbose);
                        emptyReferences.AddLast(identifier);
                        continue;
                    }

                    io.WriteError($"Importing branch {branch} (version:{version})", verbosity: Verbosities.VeryVerbose);

                    bucketInformation.Version = version;
                    bucketInformation.VersionNormalized = parsedBranch;

                    var package = loader.Load(PreProcess(driver, bucketInformation, identifier));

                    if (loader is LoaderValidating loaderValidating)
                    {
                        // Only warnings may appear here, because the error has already thrown an exception.
                        // We do not tolerate any warnings.
                        var warnings = loaderValidating.GetWarnings();
                        if (warnings.Length > 0)
                        {
                            throw new InvalidPackageException(loaderValidating.GetErrors(), loaderValidating.GetErrors(), bucketInformation);
                        }
                    }

                    AddPackage(package);
                }
                catch (TransportException ex)
                {
                    if (ex.HttpStatusCode == HttpStatusCode.NotFound)
                    {
                        emptyReferences.AddLast(identifier);
                    }

                    io.WriteError($"<warning>Skipped branch {branch}, no {Factory.DefaultBucketFile} file was found.</warning>", verbosity: Verbosities.VeryVerbose);
                    continue;
                }
#pragma warning disable CA1031
                catch (System.Exception ex)
#pragma warning restore CA1031
                {
                    if (!isVeryVerbose)
                    {
                        io.WriteError(string.Empty);
                    }

                    HasInvalidBranches = true;
                    io.WriteError($"<error>Skipped branch {branch}, {ex.Message}</error>", verbosity: Verbosities.VeryVerbose);
                    io.WriteError(string.Empty);
                    continue;
                }
            }

            driver.Cleanup();

            if (!isVeryVerbose)
            {
                io.OverwriteError(string.Empty, false);
            }

            if (GetPackages().Length <= 0)
            {
                throw new InvalidRepositoryException($"No valid {Factory.DefaultBucketFile} was found in any branch or tag of {uri}, could not load a package from it.");
            }
        }

        /// <summary>
        /// Preprocess bucket configuration file.
        /// </summary>
        /// <param name="driver">The driver instance.</param>
        /// <param name="config">The bucket configuration.</param>
        /// <param name="identifier">>Any identifier to a specific branch/tag/commit.</param>
        /// <returns>Returns preprocessed bucket configuration instance.</returns>
        protected virtual ConfigBucket PreProcess(IDriverVcs driver, ConfigBucket config, string identifier)
        {
            // keep the name of the main identifier for all packages
            config.Name = string.IsNullOrEmpty(packageName) ? config.Name : packageName;

            if (config.Dist == null)
            {
                config.Dist = driver.GetDist(identifier);
            }

            if (config.Source == null)
            {
                config.Source = driver.GetSource(identifier);
            }

            return config;
        }

        private static void RegisterDefaultDrivers()
        {
            // todo: refactoring, bad smell.
            if (!registered)
            {
                DriverSupportChecker.RegisterChecker(DriverGit.DriverName, DriverGit.IsSupport);
                DriverSupportChecker.RegisterChecker(DriverGithub.DriverName, DriverGithub.IsSupport);
                registered = true;
            }
        }

        private IDictionary<string, DriverCreater> CreateAndRegisterDefaultDrivers()
        {
            // todo: refactoring, bad smell.
            return new Dictionary<string, DriverCreater>
            {
                { DriverGithub.DriverName, (configRepository, io, config) => new DriverGithub(EnsureConfigTypeCorrect<ConfigRepositoryGithub>(configRepository), io, config) },
                { DriverGit.DriverName, (configRepository, io, config) => new DriverGit(configRepository, io, config) },
            };
        }

        private T EnsureConfigTypeCorrect<T>(ConfigRepositoryVcs config)
            where T : class
        {
            // todo: refactoring, bad smell.
            if (config is T targetConfig)
            {
                return targetConfig;
            }

            return JsonFile.Parse<T>(config);
        }

        private IDriverVcs CreateDriver(DriverCreater factory)
        {
            var instance = factory(configRepository, io, config);
            instance.Initialize();
            return instance;
        }

        private IPackage GetCachedPackageVersion(string version, string identifier, out bool skipped)
        {
            skipped = false;

            if (versionCache == null)
            {
                return null;
            }

            var cachedPackage = versionCache.GetVersionPackage(version, identifier, out skipped);
            if (skipped)
            {
                io.WriteError($"<warning>Skipped {version}, no {Factory.DefaultBucketFile} file (cached from ref {identifier})</warning>", verbosity: Verbosities.VeryVerbose);
                return null;
            }

            if (cachedPackage == null)
            {
                return null;
            }

            var name = string.IsNullOrEmpty(packageName) ? uri.ToString() : packageName;
            var message = $"Found cached {Factory.DefaultBucketFile} of <info>{name}</info> (<comment>{version}</comment>)";
            if (isVeryVerbose)
            {
                io.WriteError(message);
            }
            else if (isVerbose)
            {
                io.OverwriteError(message, false);
            }

            var existingPackage = this.FindPackage(cachedPackage.Name, cachedPackage.VersionNormalized);
            if (existingPackage != null)
            {
                io.WriteError(
                    $"<warning>Skipped cached version {version}, it conflicts with an another tag ({existingPackage.GetVersionPretty()}) as both resolve to {cachedPackage.VersionNormalized} internally</warning>",
                    verbosity: Verbosities.VeryVerbose);

                return null;
            }

            return loader.Load(cachedPackage);
        }

        private string ValidateTag(string tag)
        {
            try
            {
                return versionParser.Normalize(tag);
            }
            catch (SParseException)
            {
                // ignore.
            }

            return null;
        }

        private string ValidateBranch(string branch)
        {
            try
            {
                return versionParser.NormalizeBranch(branch);
            }
            catch (SParseException)
            {
                // ignore.
            }

            return null;
        }
    }
}

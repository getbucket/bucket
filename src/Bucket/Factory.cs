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
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.Installer;
using Bucket.IO;
using Bucket.IO.Loader;
using Bucket.Json;
using Bucket.Package;
using Bucket.Package.Loader;
using Bucket.Plugin;
using Bucket.Repository;
using Bucket.Util;
using GameBox.Console.Exception;
using GameBox.Console.Formatter;
using GameBox.Console.Output;
using GameBox.Console.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using BEventDispatcher = Bucket.EventDispatcher.EventDispatcher;
using BVersionParser = Bucket.Package.Version.VersionParser;
using SException = System.Exception;

namespace Bucket
{
    /// <summary>
    /// Creates a configured instance of bucket.
    /// </summary>
    public class Factory
    {
        /// <summary>
        /// Gets the name of the bucket folder under the vendor.
        /// </summary>
        public static string DefaultVendorBucket => "bucket";

        /// <summary>
        /// Gets the default bucket file name.
        /// </summary>
        /// <returns>Returns the default bucket file name.</returns>
        public static string DefaultBucketFile => "bucket.json";

        /// <summary>
        /// Gets regular expression matching package name.
        /// </summary>
        public static string RegexPackageName => "^(?:(?<provide>[a-z][a-z0-9_-]*)/)?(?<package>[a-z][a-z0-9._-]*)$";

        /// <summary>
        /// Gets regular to replace invalid characters for package name.
        /// </summary>
        public static string RegexPackageNameIllegal => "[A-Za-z0-9_./-]+";

        /// <summary>
        /// Gets the bucket file name.
        /// </summary>
        /// <returns>Returns the bucket file.</returns>
        public static string GetBucketFile()
        {
            return Terminal.GetEnvironmentVariable(EnvironmentVariables.Bucket) ?? $"./{DefaultBucketFile}";
        }

        /// <summary>
        /// Creates an array of additional styles.
        /// </summary>
        public static IDictionary<string, IOutputFormatterStyle> CreateAdditionalStyles()
        {
            return new Dictionary<string, IOutputFormatterStyle>()
            {
                { "warning", new OutputFormatterStyle("black", "yellow") },
            };
        }

        /// <summary>
        /// Creates a <see cref="OutputConsole"/> instance.
        /// </summary>
        public static IOutput CreateOutput()
        {
            var styles = CreateAdditionalStyles();
            var formatter = new OutputFormatter(false, styles);

            return new OutputConsole(OutputOptions.OutputNormal, formatter);
        }

        /// <summary>
        /// Creates a <see cref="Bucket"/> object from specified <paramref name="localConfigFileName"/>.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="localConfigFileName">either a configuration filename to read from, if null it will read from the default filename.</param>
        /// <param name="disablePlugins">Whether plugins should not be loaded.</param>
        public static Bucket Create(IIO io, string localConfigFileName = null, bool disablePlugins = false)
        {
            var factory = new Factory();
            return factory.CreateBucket(io, localConfigFileName, disablePlugins);
        }

        /// <summary>
        /// Creates a <see cref="Bucket"/> object from global file.
        /// </summary>
        public static Bucket CreateGlobal(IIO io, bool disablePlugins = false)
        {
            var factory = new Factory();
            return factory.CreateGlobalBucket(io, factory.CreateConfig(io), disablePlugins, true);
        }

        /// <summary>
        /// Create the <see cref="Config"/> object.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="cwd">Current working directory.</param>
        public virtual Config CreateConfig(IIO io = null, string cwd = null)
        {
            cwd = cwd ?? Environment.CurrentDirectory;
            var config = new Config(true, cwd);
            var home = GetBucketHomeDirectory();

            var merged = new JObject() { { "config", new JObject() } };
            merged["config"][Settings.Home] = home;
            merged["config"][Settings.CacheDir] = GetCacheDirectory(home);
            config.Merge(merged);

            home = config.Get(Settings.Home);

            void MergeConfig(JsonFile file, bool isAuthFile = false)
            {
                if (file.Exists())
                {
                    io?.WriteError($"Loading config file {file.GetPath()}", true, Verbosities.Debug);
                    MergeObject(file.Read(), isAuthFile);
                }
            }

            void MergeObject(JObject conf, bool isAuthFile = false)
            {
                config.Merge(isAuthFile ? new JObject() { ["config"] = conf } : conf);
            }

            // load global source.
            var globalConfigFile = new JsonFile($"{home}/config.json", null, io);
            MergeConfig(globalConfigFile);
            config.SetSourceBucket(new JsonConfigSource(globalConfigFile));

            // load global auth source.
            var globalAuthFile = new JsonFile($"{home}/auth.json", null, io);
            MergeConfig(globalAuthFile, true);
            config.SetSourceAuth(new JsonConfigSource(globalAuthFile, true));

            // load auth from environment variable.
            string bucketAuth = Terminal.GetEnvironmentVariable(EnvironmentVariables.BucketAuth);
            if (string.IsNullOrEmpty(bucketAuth))
            {
                return config;
            }

            io?.WriteError($"Loading auth config from {EnvironmentVariables.BucketAuth}");
            MergeObject(JsonFile.Parse(bucketAuth), true);

            return config;
        }

        /// <summary>
        /// Creates a <see cref="Bucket"/> object from specified <paramref name="cwd"/>.
        /// </summary>
        /// <remarks>If no <paramref name="cwd"/> is given, the current environment path will be used.</remarks>
        /// <param name="io">The input/output instance.</param>
        /// <param name="localBucket">either a configuration filename to read from, if null it will read from the default filename.</param>
        /// <param name="disablePlugins">Whether plugins should not be loaded.</param>
        /// <param name="cwd">Current working directory.</param>
        /// <param name="fullLoad">Whether to initialize everything or only main project stuff (used when loading the global bucket and auth).</param>
        /// <exception cref="InvalidArgumentException">If local config file not exists.</exception>
        public Bucket CreateBucket(IIO io, object localBucket = null, bool disablePlugins = false, string cwd = null, bool fullLoad = true)
        {
            cwd = cwd ?? Environment.CurrentDirectory;
            var localFileSystem = new FileSystemLocal(cwd);
            var config = CreateConfig(io, cwd);
            string bucketFile = null;
            if (localBucket == null || localBucket is string)
            {
                bucketFile = localBucket?.ToString();
                bucketFile = string.IsNullOrEmpty(bucketFile) ? GetBucketFile() : bucketFile;

                var localBucketFile = new JsonFile(bucketFile, localFileSystem, io);
                if (!localBucketFile.Exists())
                {
                    string message;
                    if (bucketFile == "./bucket.json" || bucketFile == "bucket.json")
                    {
                        message = $"Bucket could not find a bucket.json file in {cwd}";
                    }
                    else
                    {
                        message = $"Bucket could not find the config file {bucketFile} in {cwd}";
                    }

                    var instructions = "To initialize a project, please create a bucket.json";
                    throw new InvalidArgumentException(message + Environment.NewLine + instructions);
                }

                localBucketFile.Validate(false);

                io.WriteError($"Loading bucket file: {localBucketFile.GetPath()}", true, Verbosities.Debug);
                config.Merge(localBucketFile.Read());
                config.SetSourceBucket(new JsonConfigSource(localBucketFile));

                var localAuthFile = new JsonFile("./auth.json", localFileSystem, io);
                if (localAuthFile.Exists())
                {
                    io.WriteError($"Loading bucket file: {localAuthFile.GetPath()}", true, Verbosities.Debug);
                    config.Merge(new JObject
                    {
                        ["config"] = localAuthFile.Read(),
                    });
                    config.SetSourceAuth(new JsonConfigSource(localAuthFile, true));
                }

                localBucket = localBucketFile.Read<ConfigBucket>();
            }
            else if (localBucket is ConfigBucket configBucket)
            {
                config.Merge(JObject.FromObject(configBucket));
            }
            else
            {
                throw new UnexpectedException($"Params \"{localBucket}\" only allow {nameof(ConfigBucket)} type or string path.");
            }

            if (fullLoad)
            {
                new LoaderIOConfiguration(io).Load(config);
            }

            // initialize bucket.
            var bucket = new Bucket();
            bucket.SetConfig(config);

            var transport = CreateTransport(io, config);

            // initialize event dispatcher.
            var dispatcher = new BEventDispatcher(bucket, io);
            bucket.SetEventDispatcher(dispatcher);

            // initialize repository manager
            var repositoryManager = RepositoryFactory.CreateManager(io, config, dispatcher);
            bucket.SetRepositoryManager(repositoryManager);

            InitializeLocalInstalledRepository(io, repositoryManager, config.Get(Settings.VendorDir));

            // load root package
            var versionParser = new BVersionParser();
            var loader = new LoaderPackageRoot(repositoryManager, config, io, versionParser);
            var package = loader.Load<IPackageRoot>((ConfigBucket)localBucket);
            bucket.SetPackage(package);

            // create installation manager
            var installationManager = CreateInstallationManager();
            bucket.SetInstallationManager(installationManager);

            if (fullLoad)
            {
                var downloadManager = DownloadFactory.CreateManager(io, config, transport, dispatcher);
                bucket.SetDownloadManager(downloadManager);

                // todo: initialize archive manager
            }

            // must happen after downloadManager is created since they read it out of bucket.
            InitializeDefaultInstallers(installationManager, bucket, io);

            if (fullLoad)
            {
                Bucket globalBucket = null;
                if (BaseFileSystem.GetNormalizePath(cwd) != BaseFileSystem.GetNormalizePath(config.Get(Settings.Home)))
                {
                    globalBucket = CreateGlobalBucket(io, config, disablePlugins, false);
                }

                var pluginManager = new PluginManager(io, bucket, globalBucket, disablePlugins);
                bucket.SetPluginManager(pluginManager);
                pluginManager.LoadInstalledPlugins();
            }

            // init locker if locker exists.
            if (fullLoad && !string.IsNullOrEmpty(bucketFile))
            {
                var lockFile = Path.GetExtension(bucketFile) == ".json" ?
                                bucketFile.Substring(0, bucketFile.Length - 5) + ".lock" :
                                bucketFile + ".lock";
                var locker = new Locker(
                    io,
                    new JsonFile(lockFile, localFileSystem, io), installationManager, localFileSystem.Read(bucketFile).ToText());
                bucket.SetLocker(locker);
            }

            if (fullLoad)
            {
                // Raises an event only when it is fully loaded.
                dispatcher.Dispatch(PluginEvents.Init, this);
            }

            if (fullLoad)
            {
                // once everything is initialized we can purge packages from
                // local repos if they have been deleted on the filesystem.
                PurgePackages(repositoryManager.GetLocalInstalledRepository(), installationManager);
            }

            return bucket;
        }

        /// <summary>
        /// Create the transport instance.
        /// </summary>
        public virtual ITransport CreateTransport(IIO io, Config config)
        {
            return new TransportHttp(io, config);
        }

        /// <summary>
        /// Get the cache directory.
        /// </summary>
        protected static string GetCacheDirectory(string home)
        {
            string cacheDir = Terminal.GetEnvironmentVariable(EnvironmentVariables.BucketCacheDir);

            if (!string.IsNullOrEmpty(cacheDir))
            {
                return cacheDir;
            }

            string homeEnv = Terminal.GetEnvironmentVariable(EnvironmentVariables.BucketHome);
            if (!string.IsNullOrEmpty(homeEnv))
            {
                return $"{homeEnv}/cache";
            }

            if (Platform.IsWindows)
            {
                cacheDir = Terminal.GetEnvironmentVariable("LOCALAPPDATA");

                if (string.IsNullOrEmpty(cacheDir))
                {
                    cacheDir = $"{home}/cache";
                }
                else
                {
                    cacheDir = $"{cacheDir}/Bucket";
                }

                cacheDir = cacheDir.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return cacheDir.TrimEnd(Path.AltDirectorySeparatorChar);
            }

            var userDir = GetUserDirectory();

            if (home == $"{userDir}/.bucket" && Directory.Exists($"{home}/cache"))
            {
                return $"{home}/cache";
            }

            // todo: maybe support XDG: https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html.
            return $"{home}/cache";
        }

        /// <summary>
        /// Get the bucket home directory.
        /// </summary>
        protected static string GetBucketHomeDirectory()
        {
            string home = Terminal.GetEnvironmentVariable(EnvironmentVariables.BucketHome);
            if (!string.IsNullOrEmpty(home))
            {
                return home;
            }

            if (Platform.IsWindows)
            {
                home = Terminal.GetEnvironmentVariable("APPDATA");
                if (string.IsNullOrEmpty(home))
                {
                    throw new RuntimeException($"The APPDATA or {EnvironmentVariables.BucketHome} environment variable must be set for bucket to run correctly.");
                }

                // Use / to avoid path problems under non-windows.
                home = home.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return home.TrimEnd(Path.AltDirectorySeparatorChar) + "/Bucket";
            }

            var userDir = GetUserDirectory();

            // todo: maybe support XDG: https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html.
            return userDir + "/.bucket";
        }

        /// <summary>
        /// Add a local repository for the manager.
        /// </summary>
        protected virtual void InitializeLocalInstalledRepository(IIO io, RepositoryManager manager, string vendorDir)
        {
            manager.SetLocalInstalledRepository(new RepositoryFileSystemInstalled(new JsonFile(vendorDir + $"/{DefaultVendorBucket}/installed.json", null, io)));
        }

        /// <summary>
        /// Add default installers instance to installation manager.
        /// </summary>
        /// <remarks>This method must be called after the bucket is fully initialized.</remarks>
        protected virtual void InitializeDefaultInstallers(InstallationManager installationManager, Bucket bucket, IIO io)
        {
            installationManager.AddInstaller(new InstallerLibrary(io, bucket, null));
            installationManager.AddInstaller(new InstallerLibrary(io, bucket));
            installationManager.AddInstaller(new InstallerMetaPackage(io));
            installationManager.AddInstaller(new InstallerPlugin(io, bucket));
        }

        /// <summary>
        /// Create bucket instance from global file.
        /// </summary>
        protected virtual Bucket CreateGlobalBucket(IIO io, Config config, bool disablePlugins, bool fullLoad)
        {
            Bucket bucket = null;
            try
            {
                bucket = CreateBucket(io, $"{config.Get(Settings.Home)}/bucket.json", disablePlugins, config.Get(Settings.Home), fullLoad);
            }
#pragma warning disable CA1031
            catch (SException ex)
#pragma warning restore CA1031
            {
                io.WriteError($"Failed to initialize global bucket: {ex.Message}", verbosity: Verbosities.Debug);
            }

            return bucket;
        }

        /// <summary>
        /// Create a new installation manager instance.
        /// </summary>
        protected virtual InstallationManager CreateInstallationManager()
        {
            return new InstallationManager();
        }

        /// <summary>
        /// Purge packages that are not installed.
        /// </summary>
        protected virtual void PurgePackages(IRepositoryInstalled repositoryInstalled, InstallationManager installationManager)
        {
            foreach (var package in repositoryInstalled.GetPackages())
            {
                if (!installationManager.IsPackageInstalled(repositoryInstalled, package))
                {
                    repositoryInstalled.RemovePackage(package);
                }
            }
        }

        /// <summary>
        /// Get the user directory.
        /// </summary>
        private static string GetUserDirectory()
        {
            string home = Terminal.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
            {
                throw new RuntimeException($"The HOME or {EnvironmentVariables.BucketHome} environment variable must be set for bucket to run correctly.");
            }

            // Use / to avoid path problems under non-windows.
            home = home.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return home.TrimEnd(Path.AltDirectorySeparatorChar);
        }
    }
}

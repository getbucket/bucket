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
using Bucket.Installer;
using Bucket.Package;
using Bucket.Plugin;
using Bucket.Repository;
using GameBox.Console.EventDispatcher;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Bucket
{
    /// <summary>
    /// Represents a base bucket object.
    /// </summary>
    public class Bucket
    {
        private static readonly FileVersionInfo FileVersionInfo;
        private IPackageRoot package;
        private IEventDispatcher eventDispatcher;
        private Config config;
        private DownloadManager downloadManager;
        private RepositoryManager repositoryManager;
        private PluginManager pluginManager;
        private InstallationManager installationManager;
        private Locker locker;

        /// <summary>
        /// Initializes static members of the <see cref="Bucket"/> class.
        /// </summary>
#pragma warning disable S3963
        static Bucket()
#pragma warning restore S3963
        {
            var assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        }

        /// <summary>
        /// Gets a value indicating whether the bucket is dev version.
        /// </summary>
        public static bool IsDev => Regex.IsMatch(GetVersionPretty(), @"[.-]dev(?:\+[0-9A-Za-z\-\.]+)?$");

#pragma warning disable CA2225
        public static implicit operator bool(Bucket bucket)
#pragma warning restore CA2225
        {
            return bucket != null;
        }

        /// <summary>
        /// Get the version description of the bucket file version.
        /// </summary>
        public static string GetVersion()
        {
            return FileVersionInfo.FileVersion;
        }

        /// <summary>
        /// Get the pretty version description of the bucket project version.
        /// </summary>
        public static string GetVersionPretty()
        {
            return ParseVersionPretty(FileVersionInfo.ProductVersion);
        }

        /// <summary>
        /// Get the release data of the bucket.
        /// </summary>
        public static DateTime GetReleaseData()
        {
            var releaseDate = new DateTime(2000, 1, 1, 0, 0, 0);
            releaseDate = releaseDate.AddDays(FileVersionInfo.FileBuildPart)
                                     .AddSeconds(FileVersionInfo.FilePrivatePart * 2);

            return releaseDate;
        }

        /// <summary>
        /// Get the release data string of the bucket.
        /// </summary>
        /// <remarks>eg:2019-01-01 12:02:12.</remarks>
        public static string GetReleaseDataPretty()
        {
            return GetReleaseData().ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Sets the event dispatcher in bucket object.
        /// </summary>
        public void SetEventDispatcher(IEventDispatcher dispatcher)
        {
            eventDispatcher = dispatcher;
        }

        /// <summary>
        /// Gets the event dispatcher.
        /// </summary>
        public IEventDispatcher GetEventDispatcher()
        {
            return eventDispatcher;
        }

        /// <summary>
        /// Sets the config object.
        /// </summary>
        public void SetConfig(Config config)
        {
            this.config = config;
        }

        /// <summary>
        /// Gets the config object.
        /// </summary>
        public Config GetConfig()
        {
            return config;
        }

        /// <summary>
        /// Sets the root package instance.
        /// </summary>
        public void SetPackage(IPackageRoot package)
        {
            this.package = package;
        }

        /// <summary>
        /// Gets the root package instance.
        /// </summary>
        public IPackageRoot GetPackage()
        {
            return package;
        }

        /// <summary>
        /// Sets the download manager instance.
        /// </summary>
        public void SetDownloadManager(DownloadManager manager)
        {
            downloadManager = manager;
        }

        /// <summary>
        /// Gets the download manager instance.
        /// </summary>
        public DownloadManager GetDownloadManager()
        {
            return downloadManager;
        }

        /// <summary>
        /// Sets the repository manager instance.
        /// </summary>
        public void SetRepositoryManager(RepositoryManager manager)
        {
            repositoryManager = manager;
        }

        /// <summary>
        /// Gets the repository manager instance.
        /// </summary>
        public RepositoryManager GetRepositoryManager()
        {
            return repositoryManager;
        }

        /// <summary>
        /// Sets the plugin manager instance.
        /// </summary>
        public void SetPluginManager(PluginManager manager)
        {
            pluginManager = manager;
        }

        /// <summary>
        /// Gets the plugin manager instance.
        /// </summary>
        public PluginManager GetPluginManager()
        {
            return pluginManager;
        }

        /// <summary>
        /// Sets the installation manager instance.
        /// </summary>
        public void SetInstallationManager(InstallationManager manager)
        {
            installationManager = manager;
        }

        /// <summary>
        /// Gets the installation manager instance.
        /// </summary>
        public InstallationManager GetInstallationManager()
        {
            return installationManager;
        }

        /// <summary>
        /// Sets the bucket locker instance.
        /// </summary>
        public void SetLocker(Locker locker)
        {
            this.locker = locker;
        }

        /// <summary>
        /// Gets the locker instance.
        /// </summary>
        public Locker GetLocker()
        {
            return locker;
        }

        internal static string ParseVersionPretty(string version)
        {
            // master-dev should not be supported for Microsoft's product version,
            // so, special version 0 is used to match the master daily compilation.
            var matched = Regex.Match(
                version,
                @"^0(?:\.0){0,3}[.-]dev(?<build>\+[0-9A-Za-z\-\.]+)?$",
                RegexOptions.IgnoreCase);

            if (matched.Success)
            {
                return $"master-dev{matched.Groups["build"].Value}";
            }

            return version;
        }
    }
}

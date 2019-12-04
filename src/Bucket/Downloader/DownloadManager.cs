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

using Bucket.Exception;
using Bucket.IO;
using Bucket.Package;
using Bucket.Util;
using GameBox.Console.Exception;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using SException = System.Exception;

namespace Bucket.Downloader
{
    /// <summary>
    /// Represents a download manager.
    /// </summary>
    public class DownloadManager
    {
        private readonly IIO io;
        private readonly IDictionary<string, IDownloader> downloaders;
        private IEnumerable<(string pattern, InstallationSource prefer)> preferences;
        private bool preferSource;
        private bool preferDist;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadManager"/> class.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="preferSource">Whether is prefer source.</param>
        public DownloadManager(IIO io = null, bool preferSource = false)
        {
            this.io = io ?? IONull.That;
            this.preferSource = preferSource;
            downloaders = new Dictionary<string, IDownloader>();
        }

        /// <summary>
        /// Set prefer the source download.
        /// </summary>
        public virtual DownloadManager SetPreferSource(bool preferSource = true)
        {
            this.preferSource = preferSource;
            return this;
        }

        /// <summary>
        /// Set prefer the dist download.
        /// </summary>
        public virtual DownloadManager SetPreferDist(bool preferDist = true)
        {
            this.preferDist = preferDist;
            return this;
        }

        /// <summary>
        /// Sets fine tuned preference settings for package level source/dist selection..
        /// </summary>
        /// <param name="preferences">An array of preferences by package patterns.</param>
        public virtual DownloadManager SetPreferences(IEnumerable<(string pattern, InstallationSource prefer)> preferences)
        {
            // todo: Sorting, sorting backwards with wildcards.
            this.preferences = preferences;
            return this;
        }

        /// <summary>
        /// Sets installer downloader for a specific installation type. Overwrite if the type already exists.
        /// </summary>
        /// <param name="type">The installation type.</param>
        /// <param name="downloader">The downloader instance.</param>
        public virtual DownloadManager SetDownloader(string type, IDownloader downloader)
        {
            downloaders[type.ToLower()] = downloader;
            return this;
        }

        /// <summary>
        /// Gets installer downloader for a specific installation type.
        /// </summary>
        /// <param name="type">The installation type.</param>
        /// <returns>Return the downloader instance.</returns>
        public virtual IDownloader GetDownloader(string type)
        {
            type = type.ToLower();
            if (!downloaders.TryGetValue(type, out IDownloader downloader))
            {
                throw new InvalidArgumentException($"Unknown downloader type: {type}. Available types: {string.Join(", ", downloaders.Keys)}.");
            }

            return downloader;
        }

        /// <summary>
        /// Gets downloader for already installed package.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <returns>Returns the downloader instance.</returns>
        public virtual IDownloader GetDownloaderForPackage(IPackage package)
        {
            var installationSource = package.GetInstallationSource();

            IDownloader downloader;
            if (installationSource == InstallationSource.Dist)
            {
                downloader = GetDownloader(package.GetDistType());
            }
            else if (installationSource == InstallationSource.Source)
            {
                downloader = GetDownloader(package.GetSourceType());
            }
            else
            {
                throw new InvalidArgumentException($"Package {package} does not have an installation source set.");
            }

            if (installationSource != downloader.InstallationSource)
            {
                throw new RuntimeException(
                   $"Downloader \"{downloader.GetType()}\" is a {downloader.InstallationSource} type downloader and can not be used to download {installationSource} for package {package}.");
            }

            return downloader;
        }

        /// <summary>
        /// Gets the downloader installation type for downloader instance.
        /// </summary>
        /// <param name="downloader">The downloader instance.</param>
        /// <returns>null if downloader not found.</returns>
        public virtual string GetDownloaderType(IDownloader downloader)
        {
            foreach (var item in downloaders)
            {
                if (item.Value == downloader)
                {
                    return item.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// Downloads package into <paramref name="cwd"/>.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="cwd">The target dir.</param>
        /// <param name="previousPackage">The previous package instance in case of updates.</param>
        /// <returns>Return an asynchronous task.</returns>
        public virtual Task Download(IPackage package, string cwd, IPackage previousPackage = null)
        {
            var sources = GetAvailableSources(package, previousPackage);
            InstallationSource source;

            Task DownloadAction(bool retry = false)
            {
                if (sources.Length <= 0)
                {
                    throw new RuntimeException($"Package {package} there is no valid source to try.");
                }

                source = Arr.Shift(ref sources);

                if (retry)
                {
                    io.WriteError($"    <warning>Now trying to download from {source}</warning>");
                }

                package.SetInstallationSource(source);
                var downloader = GetDownloaderForPackage(package);

                Guard.Requires<UnexpectedException>(
                    downloader != null,
                    $"Package {package} can not found downloader from {source}.");

                async Task WaitDownload()
                {
                    try
                    {
                        var task = downloader.Download(package, cwd);
                        await task;
                    }
#pragma warning disable CA1031
                    catch (SException ex)
#pragma warning restore CA1031
                    {
                        var task = HandleError(ex);
                        await task;
                    }
                }

                return WaitDownload();
            }

            Task HandleError(SException exception)
            {
                if (!(exception is RuntimeException runtimeException) || sources.Length == 0)
                {
                    ExceptionDispatchInfo.Capture(exception).Throw();
                    throw exception;
                }

                io.WriteError(
                    $"    <warning>Failed to download {package.GetNamePretty()} from {source}: {exception.Message}</warning>");

                return DownloadAction(true);
            }

            return DownloadAction();
        }

        /// <summary>
        /// Installs package into <paramref name="cwd"/>.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="cwd">The target dir.</param>
        public virtual void Install(IPackage package, string cwd)
        {
            var downloader = GetDownloaderForPackage(package);
            downloader?.Install(package, cwd);
        }

        /// <summary>
        /// Updates package from initial to target version.
        /// </summary>
        /// <param name="initial">The initial package version.</param>
        /// <param name="target">The target package version.</param>
        /// <param name="cwd">The target dir.</param>
        public virtual void Update(IPackage initial, IPackage target, string cwd)
        {
            if (target.GetInstallationSource() == null)
            {
                target.SetInstallationSource(initial.GetInstallationSource());
            }

            var targetDownloader = GetDownloaderForPackage(target);
            var initialDownloader = GetDownloaderForPackage(initial);
            var initialType = GetDownloaderType(initialDownloader);
            var targetType = GetDownloaderType(targetDownloader);

            if (initialType == targetType)
            {
                try
                {
                    targetDownloader.Update(initial, target, cwd);
                    return;
                }
                catch (RuntimeException ex)
                {
                    if (!io.IsInteractive)
                    {
                        throw;
                    }

                    io.WriteError($"<error> Update failed ({ex.Message})</error>");
                    if (!io.AskConfirmation("    Would you like to try reinstalling the package instead [<comment>yes</comment>]?", true))
                    {
                        throw;
                    }
                }
            }

            initialDownloader.Remove(initial, cwd);
            Install(target, cwd);
        }

        /// <summary>
        /// Removes package from <paramref name="cwd"/>.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="cwd">The target dir.</param>
        public virtual void Remove(IPackage package, string cwd)
        {
            var downloader = GetDownloaderForPackage(package);
            downloader?.Remove(package, cwd);
        }

        /// <summary>
        /// Determines the install preference of a package.
        /// </summary>
        protected InstallationSource ResolvePackageInstallPreference(IPackage package)
        {
            foreach (var (pattern, preference) in preferences ?? Array.Empty<(string, InstallationSource)>())
            {
                if (!Str.Is(pattern, package.GetName()))
                {
                    continue;
                }

                if (preference == InstallationSource.Dist || (!package.IsDev && preference == InstallationSource.Auto))
                {
                    return InstallationSource.Dist;
                }

                return InstallationSource.Source;
            }

            return package.IsDev ? InstallationSource.Source : InstallationSource.Dist;
        }

        private InstallationSource[] GetAvailableSources(IPackage package, IPackage previousPackage)
        {
            var sourceType = package.GetSourceType();
            var distType = package.GetDistType();

            var sources = Array.Empty<InstallationSource>();

            if (!string.IsNullOrEmpty(sourceType))
            {
                Arr.Push(ref sources, InstallationSource.Source);
            }

            if (!string.IsNullOrEmpty(distType))
            {
                Arr.Push(ref sources, InstallationSource.Dist);
            }

            if (sources.Length <= 0)
            {
                throw new InvalidArgumentException($"Package {package} must have a source or dist specified.");
            }

            // if we are updating, we want to keep the same source
            // as the previously installed package (if available in the new one)
            //
            // unless the previous package was stable dist (by default) and the new
            // package is dev, then we allow the new default to take over
            if (previousPackage != null &&
                Array.Exists(sources, (source) => previousPackage.GetInstallationSource() == source) &&
                !(!previousPackage.IsDev && previousPackage.GetInstallationSource() == InstallationSource.Dist && package.IsDev))
            {
                var previousSource = previousPackage.GetInstallationSource();
                Array.Sort(sources, (left, right) =>
                {
                    return left == previousSource ? -1 : 1;
                });

                return sources;
            }

            // reverse sources in case dist is the preferred source for this package
            if (!preferSource && (preferDist || ResolvePackageInstallPreference(package) == InstallationSource.Dist))
            {
                Array.Reverse(sources);
            }

            return sources;
        }
    }
}

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
using Bucket.Downloader.Transport;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Util;
using GameBox.Console.EventDispatcher;

namespace Bucket.Downloader
{
    /// <summary>
    /// Represents download factory.
    /// </summary>
    public static class DownloadFactory
    {
        /// <summary>
        /// Create a new <see cref="DownloadManager"/> instance.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The config instance.</param>
        /// <param name="transport">The transport instance.</param>
        /// <param name="eventDispatcher">The event dispatcher instance.</param>
        public static DownloadManager CreateManager(IIO io, Config config, ITransport transport = null, IEventDispatcher eventDispatcher = null)
        {
            var manager = new DownloadManager(io);

            InstallationSource GetInstallationSource(string prefer)
            {
                switch (prefer ?? "auto")
                {
                    case "dist":
                        return InstallationSource.Dist;
                    case "source":
                        return InstallationSource.Source;
                    case "auto":
                    default:
                        return InstallationSource.Auto;
                }
            }

            try
            {
                var prefer = GetInstallationSource(config.Get(Settings.PreferredInstall));
                switch (prefer)
                {
                    case InstallationSource.Dist:
                        manager.SetPreferDist();
                        break;
                    case InstallationSource.Source:
                        manager.SetPreferSource();
                        break;
                    default:
                        break;
                }
            }
            catch (ConfigException)
            {
                // Maybe the developer is using fine configuration.
                var preferred = config.Get<ConfigPreferred>(Settings.PreferredInstall);
                manager.SetPreferences(Arr.Map(preferred, (item) => (item.Key, GetInstallationSource(item.Value))));
            }

            transport = transport ?? new TransportHttp(io, config);
            var process = new BucketProcessExecutor(io);
            var fileSystem = new FileSystemLocal(process: process);

            ICache cache = null;
            if (config.Get(Settings.CacheFilesTTL) > 0)
            {
                cache = new CacheFileSystem(config.Get(Settings.CacheFilesDir), io, "a-z0-9_./", fileSystem);
            }

            manager.SetDownloader("git", new DownloaderGit(io, config, process, fileSystem));
            manager.SetDownloader("zip", new DownloaderZip(io, config, transport, eventDispatcher, cache, fileSystem, process));
            manager.SetDownloader("file", new DownloaderFile(io, config, transport, eventDispatcher, cache, fileSystem));

            return manager;
        }
    }
}

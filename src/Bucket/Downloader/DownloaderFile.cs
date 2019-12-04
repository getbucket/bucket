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
using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Plugin;
using Bucket.Util;
using GameBox.Console.EventDispatcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using BVersionParser = Bucket.Package.Version.VersionParser;
using SException = System.Exception;

namespace Bucket.Downloader
{
    /// <summary>
    /// Base downloader for files.
    /// </summary>
    public class DownloaderFile : IDownloader
    {
        private readonly IIO io;
        private readonly IFileSystem fileSystem;
        private readonly Config config;
        private readonly ITransport transport;
        private readonly IEventDispatcher eventDispatcher;
        private readonly ICache cache;
        private readonly IDictionary<string, string> lastCacheWrites;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderFile"/> class.
        /// </summary>
        public DownloaderFile(
            IIO io,
            Config config,
            ITransport transport,
            IEventDispatcher eventDispatcher = null,
            ICache cache = null,
            IFileSystem fileSystem = null)
        {
            this.io = io;
            this.config = config;
            this.transport = transport;
            this.eventDispatcher = eventDispatcher;
            this.cache = cache;
            this.fileSystem = fileSystem ?? new FileSystemLocal();

            if (CacheFileSystem.GCIsNecessary(cache))
            {
                cache.GC(config.Get(Settings.CacheFilesTTL), config.Get(Settings.CacheFilesMaxSize));
            }

            lastCacheWrites = new Dictionary<string, string>();
        }

        /// <inheritdoc />
        public virtual InstallationSource InstallationSource => InstallationSource.Dist;

        /// <inheritdoc />
        public Task Download(IPackage package, string cwd)
        {
            return Download(package, cwd, true);
        }

        /// <inheritdoc cref="IDownloader.Download(IPackage, string)" />
        public virtual Task Download(IPackage package, string cwd, bool output)
        {
            if (string.IsNullOrEmpty(package.GetDistUri()))
            {
                throw new RuntimeException("The given package is missing url information.");
            }

            var downloadedFilePath = GetDownloadedFilePath(package, cwd);
            fileSystem.Delete(cwd);

            void DoDownload(string uri)
            {
                var processedUri = ProcessUri(package, uri);
                if (eventDispatcher != null)
                {
                    var preFileDownloadEvent = new PreFileDownloadEventArgs(PluginEvents.PreFileDownload, transport, processedUri);
                    eventDispatcher.Dispatch(this, preFileDownloadEvent);
                }

                try
                {
                    var checksum = package.GetDistShasum();
                    var cacheKey = GetCacheKey(package, processedUri);

                    void DownloadFromHttp(int retries = 3)
                    {
                        SException processException = null;
                        while (retries-- > 0)
                        {
                            try
                            {
                                transport.Copy(
                                    processedUri,
                                    downloadedFilePath,
                                    new ReportDownloadProgress(io, $"{GetDownloadingPrompt(package)}: "));
                                break;
                            }
                            catch (TransportException ex)
                            {
                                // clean up immediately. otherwise it will lead to failure forever.
                                fileSystem.Delete(downloadedFilePath);

                                // if we got an http response with a proper code, then
                                // requesting again will probably not help, abort.
                                var serverProblems = new[]
                                {
                                    HttpStatusCode.InternalServerError,
                                    HttpStatusCode.BadGateway,
                                    HttpStatusCode.ServiceUnavailable,
                                    HttpStatusCode.GatewayTimeout,
                                };

                                if (ex.HttpStatusCode != 0
                                    && (!Array.Exists(serverProblems, (code) => code == ex.HttpStatusCode) || retries <= 0))
                                {
                                    throw;
                                }

                                processException = ex;
                                Thread.Sleep(500);
                            }
                        }

                        if (processException != null)
                        {
                            ExceptionDispatchInfo.Capture(processException).Throw();
                        }

                        if (cache != null && cache.CopyFrom(cacheKey, downloadedFilePath))
                        {
                            lastCacheWrites[package.GetName()] = cacheKey;
                        }
                    }

                    // use from cache if it is present and has a valid checksum
                    // or we have no checksum to check against
                    if (cache != null && cache.Sha1(cacheKey, checksum) && cache.CopyTo(cacheKey, downloadedFilePath))
                    {
                        if (output)
                        {
                            io.WriteError($"  - Loading <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>) from cache", false);
                        }
                    }
                    else
                    {
                        if (output)
                        {
                            io.WriteError(GetDownloadingPrompt(package), false);
                        }

                        DownloadFromHttp();
                    }

                    // Verify the file to ensure that the file is not damaged.
                    if (!fileSystem.Exists(downloadedFilePath, FileSystemOptions.File))
                    {
                        throw new UnexpectedException($"{uri} could not be saved to {downloadedFilePath} , make sure the directory is writable and you have internet connectivity.");
                    }

                    if (!string.IsNullOrEmpty(checksum))
                    {
                        using (var stream = fileSystem.Read(downloadedFilePath))
                        {
                            if (Security.Sha1(stream) != checksum)
                            {
                                throw new UnexpectedException($"The checksum verification of the file failed (downloaded from {uri})");
                            }
                        }
                    }
                }
                catch
                {
                    fileSystem.Delete(downloadedFilePath);
                    ClearLastCacheWrite(package);
                    throw;
                }
                finally
                {
                    if (output)
                    {
                        io.OverwriteError(string.Empty, false);
                    }
                }
            }

            void Start()
            {
                var uris = package.GetDistUris();

                // The performance popped from the tail will be better
                // than popping out from the head
                Array.Reverse(uris);
                while (uris.Length > 0)
                {
                    var uri = Arr.Pop(ref uris);
                    try
                    {
                        DoDownload(uri);
                        break;
                    }
                    catch (SException ex)
                    {
                        if (io.IsDebug)
                        {
                            io.WriteError(string.Empty);
                            io.WriteError($"Faild: [{ex.GetType()}] {ex.Message}");
                        }
                        else if (uris.Length > 0)
                        {
                            io.WriteError(string.Empty);
                            io.WriteError($" Failed, trying the next URL ({ex.Message})");
                        }

                        if (uris.Length <= 0)
                        {
                            throw;
                        }
                    }
                }
            }

            return Task.Run(Start);
        }

        /// <inheritdoc />
        public void Install(IPackage package, string cwd)
        {
            Install(package, cwd, true);
        }

        /// <inheritdoc cref="IDownloader.Install(IPackage, string)" />
        /// <param name="output">Whether is display the output message.</param>
        public virtual void Install(IPackage package, string cwd, bool output)
        {
            if (output)
            {
                io.WriteError($"  - Installing <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>)");
            }

            var uri = new Uri(package.GetDistUri());
            var downloadedFilePath = GetDownloadedFilePath(package, cwd);
            var installPath = Path.Combine(cwd, Path.GetFileName(uri.AbsolutePath));

            fileSystem.Delete(cwd);
            fileSystem.Move(downloadedFilePath, installPath);
        }

        /// <inheritdoc />
        public void Remove(IPackage package, string cwd)
        {
            Remove(package, cwd, true);
        }

        /// <inheritdoc cref="IDownloader.Remove(IPackage, string)" />
        /// <param name="output">Whether is display the output message.</param>
        public virtual void Remove(IPackage package, string cwd, bool output)
        {
            if (output)
            {
                io.WriteError($"  - Removing <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>)");
            }

            fileSystem.Delete(cwd);
            Guard.Requires<RuntimeException>(!fileSystem.Exists(cwd), $"Could not completely delete {cwd}, aborting.");
        }

        /// <inheritdoc />
        public virtual void Update(IPackage initial, IPackage target, string cwd)
        {
            var name = target.GetName();
            var from = initial.GetVersionPrettyFull();
            var to = target.GetVersionPrettyFull();

            var actionName = BVersionParser.IsUpgrade(initial.GetVersion(), target.GetVersion()) ? "Updating" : "Downgrading";
            io.WriteError($"  - {actionName} <info>{name}</info> (<comment>{from}</comment> => <comment>{to}</comment>): ", false);

            Remove(initial, cwd, false);
            Install(target, cwd, false);

            io.WriteError(string.Empty);
        }

        /// <summary>
        /// Gets file path for specific package.
        /// </summary>
        protected internal virtual string GetDownloadedFilePath(IPackage package, string cwd)
        {
            var uri = new Uri(package.GetDistUri());
            var filename = Security.Md5($"{cwd}{package}{package.GetDistReference()}{package.GetDistShasum()}");
            return Path.Combine(GetTempDirectory(), "download", $"{filename}{Path.GetExtension(uri.AbsolutePath)}");
        }

        /// <summary>
        /// Clear the last written cache.
        /// </summary>
        protected internal virtual void ClearLastCacheWrite(IPackage package)
        {
            if (cache == null || !lastCacheWrites.TryGetValue(package.GetName(), out string cacheKey))
            {
                return;
            }

            cache.Delete(cacheKey);
            lastCacheWrites.Remove(package.GetName());
        }

        /// <summary>
        /// Get the temp directory.
        /// </summary>
        protected virtual string GetTempDirectory()
        {
            // Avoid vendor being an absolute path, not a folder name.
            return Path.Combine(Environment.CurrentDirectory, Path.Combine(config.Get(Settings.VendorDir), Factory.DefaultVendorBucket, ".temp"));
        }

        /// <summary>
        /// Process the download url, compatibility adjustments can be made in it.
        /// </summary>
        protected virtual string ProcessUri(IPackage package, string uri)
        {
            if (!string.IsNullOrEmpty(package.GetDistReference()))
            {
                uri = BucketUri.UpdateDistReference(config, uri, package.GetDistReference());
            }

            return uri;
        }

        protected IFileSystem GetFileSystem()
        {
            return fileSystem;
        }

        private string GetCacheKey(IPackage package, string processedUri)
        {
            // we use the complete download url here to avoid conflicting entries
            // from different packages. To prevent conflicts between third-party
            // libraries and official libraries
            var cacheKey = Security.Sha1(processedUri);
            return $"{package.GetName()}/{cacheKey}.{package.GetDistType()}";
        }

        private string GetDownloadingPrompt(IPackage package)
        {
            return $"  - Downloading <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>)";
        }

        // todo: need implement IReportChange interface.
    }
}

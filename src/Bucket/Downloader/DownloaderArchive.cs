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
using System;
using System.IO;

namespace Bucket.Downloader
{
    /// <summary>
    /// Base downloader for archives.
    /// </summary>
    public abstract class DownloaderArchive : DownloaderFile
    {
        private readonly IIO io;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderArchive"/> class.
        /// </summary>
        protected DownloaderArchive(
            IIO io,
            Config config,
            ITransport transport,
            IEventDispatcher eventDispatcher = null,
            ICache cache = null,
            IFileSystem fileSystem = null)
            : base(io, config, transport, eventDispatcher, cache, fileSystem)
        {
            this.io = io;
            this.fileSystem = GetFileSystem();
        }

        /// <inheritdoc />
        public override void Install(IPackage package, string cwd, bool output)
        {
            if (output)
            {
                io.WriteError($"  - Installing <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>): Extracting archive");
            }
            else
            {
                io.WriteError("Extracting archive", false);
            }

            fileSystem.Delete(cwd);

            var temporaryDir = Path.Combine(
                    GetTempDirectory(),
                    "extract",
                    Security.Md5(Guid.NewGuid().ToString()).Substring(0, 7));

            var downloadedFilePath = GetDownloadedFilePath(package, cwd);

            try
            {
                FileSystemLocal.EnsureDirectory(temporaryDir);

                try
                {
                    Extract(package, downloadedFilePath, temporaryDir);
                }
                catch
                {
                    // remove cache if the file was corrupted.
                    ClearLastCacheWrite(package);
                    throw;
                }

                // Expand a single top-level directory for a
                // better experience.
                string ExtractSingleDirAtTopLevel(string path)
                {
                    var contents = fileSystem.GetContents(path);
                    var files = contents.GetFiles();
                    var dirs = contents.GetDirectories();

                    files = Arr.Filter(files, (file) => !file.EndsWith(".DS_Store", StringComparison.Ordinal));

                    if ((dirs.Length + files.Length) != 1 || files.Length == 1)
                    {
                        return path;
                    }

                    return ExtractSingleDirAtTopLevel(dirs[0]);
                }

                fileSystem.Move(ExtractSingleDirAtTopLevel(temporaryDir), cwd);
            }
            catch
            {
                fileSystem.Delete(cwd);
                throw;
            }
            finally
            {
                fileSystem.Delete(temporaryDir);
                fileSystem.Delete(downloadedFilePath);
            }
        }

        /// <summary>
        /// Extract file to target directory.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="file">The extracted file.</param>
        /// <param name="extractPath">Unzip the local saved directory.</param>
        protected internal abstract void Extract(IPackage package, string file, string extractPath);
    }
}

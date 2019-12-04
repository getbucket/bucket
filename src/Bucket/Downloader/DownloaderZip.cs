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

using Bucket.Archive;
using Bucket.Cache;
using Bucket.Configuration;
using Bucket.Downloader.Transport;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Util;
using GameBox.Console.EventDispatcher;
using GameBox.Console.Process;

namespace Bucket.Downloader
{
    /// <summary>
    /// The downloader for zip file.
    /// </summary>
    public class DownloaderZip : DownloaderArchive
    {
        private readonly ExtractorZip extractor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderZip"/> class.
        /// </summary>
        public DownloaderZip(
            IIO io,
            Config config,
            ITransport transport,
            IEventDispatcher eventDispatcher = null,
            ICache cache = null,
            IFileSystem fileSystem = null,
            IProcessExecutor process = null)
            : base(io, config, transport, eventDispatcher, cache, fileSystem)
        {
            extractor = new ExtractorZip(io, GetFileSystem(), process ?? new BucketProcessExecutor(io));
        }

        /// <inheritdoc />
        protected internal override void Extract(IPackage package, string file, string extractPath)
        {
            extractor.Extract(file, extractPath);
        }
    }
}

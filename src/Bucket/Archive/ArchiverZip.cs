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

using Bucket.FileSystem;
using System;
using System.IO;
using System.IO.Compression;

namespace Bucket.Archive
{
    /// <summary>
    /// Archive files as zip packages.
    /// </summary>
    public class ArchiverZip : IArchiver
    {
        private readonly FileSystemLocal fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiverZip"/> class.
        /// </summary>
        public ArchiverZip()
        {
            fileSystem = new FileSystemLocal();
        }

        /// <summary>
        /// Gets or sets the compression level.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Fastest;

        /// <inheritdoc />
        public string Archive(string sources, string target, string[] excludes = null, bool ignoreFilters = false)
        {
            sources = BaseFileSystem.GetNormalizePath(sources);

            using (var savedFile = fileSystem.Create(target))
            using (var zipArchive = new ZipArchive(savedFile, ZipArchiveMode.Create))
            {
                var finder = new ArchivableFilesFinder(sources, excludes, ignoreFilters, fileSystem);
                foreach (var file in finder)
                {
                    if (file.EndsWith("/", StringComparison.Ordinal))
                    {
                        zipArchive.CreateEntry(file);
                        continue;
                    }

                    var entry = zipArchive.CreateEntry(file, CompressionLevel);
                    using (var entryStream = entry.Open())
                    using (var archiveFile = fileSystem.Read(Path.Combine(sources, file)))
                    {
                        archiveFile.CopyTo(entryStream);
                    }
                }
            }

            return target;
        }
    }
}

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
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Util;
using GameBox.Console.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Bucket.Cache
{
    /// <summary>
    /// Represents a file system cache.
    /// </summary>
    public class CacheFileSystem : ICache
    {
        private static readonly Random Random = new Random();
        private readonly string whitelist;
        private readonly IIO io;
        private readonly IFileSystem fileSystem;
        private bool cacheCollected;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheFileSystem"/> class.
        /// </summary>
        /// <param name="cacheDirectory">Location of the cache.</param>
        /// <param name="io">The input/output instance used in debugger.</param>
        /// <param name="whitelist">List of characters that are allowed in path names (used in a regex character class).</param>
        /// <param name="fileSystem">Optional filesystem instance.</param>
        public CacheFileSystem(string cacheDirectory, IIO io, string whitelist = "a-z0-9.", IFileSystem fileSystem = null)
        {
            CacheDirectory = cacheDirectory;
            this.io = io;
            this.whitelist = whitelist;
            this.fileSystem = fileSystem ?? new FileSystemLocal(cacheDirectory);
            Enable = IsUsable(cacheDirectory);
        }

        /// <inheritdoc />
        public bool Enable { get; set; }

        /// <summary>
        /// Gets represents a cache directory.
        /// </summary>
        public string CacheDirectory { get; private set; }

        /// <summary>
        /// Formatted the cache folder.
        /// </summary>
        public static string FormatCacheFolder(string path)
        {
            return Regex.Replace(path, @"[^a-z0-9.]", "-", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Whether the path is usable.
        /// </summary>
        /// <param name="path">The specified path.</param>
        /// <returns>True if the path is usable.</returns>
        public static bool IsUsable(string path)
        {
            return !Regex.IsMatch(path, @"(^|[\\\\/])(\$null|nul|NUL|/dev/null)([\\\\/]|$)");
        }

        /// <summary>
        /// Whether is gc is necessary.
        /// </summary>
        /// <param name="cache">The cache system.</param>
        /// <returns>True if the gc is necessary.</returns>
        public static bool GCIsNecessary(ICache cache)
        {
            if (cache == null)
            {
                return false;
            }

            if (cache is CacheFileSystem cacheFileSystem)
            {
                return !cacheFileSystem.cacheCollected && Random.Next(0, 50) == 0;
            }

            return Random.Next(0, 50) == 0;
        }

        /// <inheritdoc />
        public Stream Read(string file, bool touch = true)
        {
            if (!Enable)
            {
                throw new RuntimeException("Cache system is not available. Enable is false.");
            }

            if (!TryRead(file, out Stream stream, touch))
            {
                var location = ValidatePathPrefix(ValidateWhiteList(file));
                throw new FileSystemException($"Cache file {location} is not found.");
            }

            return stream;
        }

        /// <summary>
        /// Try read the cache file. if the file not exists return false.
        /// </summary>
        /// <param name="file">The cache file name.</param>
        /// <param name="stream">The stream of the cache file.</param>
        /// <param name="touch">Whether is update the ttl.</param>
        /// <returns>True if the file readed.</returns>
        public bool TryRead(string file, out Stream stream, bool touch = true)
        {
            // todo: Try Read not conducive to external release of Stream.
            // Maybe consider removing this function.
            stream = null;

            if (!Enable)
            {
                return false;
            }

            file = ValidateWhiteList(file);
            var location = ValidatePathPrefix(file);

            if (!fileSystem.Exists(location))
            {
                return false;
            }

            io?.WriteError($"Reading {location} from cache.", verbosity: Verbosities.Debug);

            if (touch)
            {
                io?.WriteError($"Touch {location} from cache.", verbosity: Verbosities.Debug);
                var meta = fileSystem.GetMetaData(location);
                meta.LastAccessTime = DateTime.Now;
            }

            stream = fileSystem.Read(location);
            return true;
        }

        /// <summary>
        /// Whether the cache file is exists.
        /// </summary>
        /// <param name="file">The cache file name.</param>
        /// <returns>True if the file exists.</returns>
        public bool Contains(string file)
        {
            if (!Enable)
            {
                return false;
            }

            file = ValidateWhiteList(file);
            var location = ValidatePathPrefix(file);

            return fileSystem.Exists(location);
        }

        /// <inheritdoc />
        public void Write(string file, Stream stream)
        {
            if (!Enable)
            {
                return;
            }

            file = ValidateWhiteList(file);
            var location = ValidatePathPrefix(file);

            io?.WriteError($"Writing {location} into cache.", verbosity: Verbosities.Debug);

            try
            {
                fileSystem.Write(location, stream);
            }
            catch (System.Exception ex)
            {
                io?.WriteError($"<warning>Failed to write into cache({location}): {ex.Message}</warning>", verbosity: Verbosities.Debug);
                throw;
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (!Enable)
            {
                return;
            }

            var contents = fileSystem.GetContents(CacheDirectory);

            foreach (var content in Arr.Merge(contents.GetFiles(), contents.GetDirectories()))
            {
                fileSystem.Delete(content);
            }

            io?.WriteError($"Clear all cache {CacheDirectory}.", verbosity: Verbosities.Debug);
        }

        /// <inheritdoc />
        public void Delete(string file)
        {
            if (!Enable)
            {
                return;
            }

            file = ValidateWhiteList(file);
            var location = ValidatePathPrefix(file);

            if (!fileSystem.Exists(location))
            {
                return;
            }

            io?.WriteError($"Delete {location} into cache.", verbosity: Verbosities.Debug);

            fileSystem.Delete(location);
        }

        /// <inheritdoc />
        public bool GC(int ttl, int maxSize)
        {
            if (!Enable)
            {
                return false;
            }

            cacheCollected = true;

            var candidates = new SortSet<IMetaData, DateTime>();
            var directories = new Queue<string>();

            void AddCandidate(string file)
            {
                var meta = fileSystem.GetMetaData(file);
                candidates.Add(meta, meta.LastAccessTime);
            }

            if (!fileSystem.Exists(CacheDirectory, FileSystemOptions.Directory))
            {
                return true;
            }

            var contents = fileSystem.GetContents(CacheDirectory);

            Array.ForEach(contents.GetDirectories(), directories.Enqueue);
            Array.ForEach(contents.GetFiles(), AddCandidate);

            while (directories.Count > 0)
            {
#pragma warning disable S4158 // bug: Empty collections should not be accessed or iterated
                contents = fileSystem.GetContents(directories.Dequeue());
#pragma warning restore S4158

                Array.ForEach(contents.GetDirectories(), directories.Enqueue);
                Array.ForEach(contents.GetFiles(), AddCandidate);
            }

            var freeSpace = 0L;
            var deletedFiles = 0;

            // gc with ttl.
            var expire = DateTime.Now.AddSeconds(-ttl);
            foreach (var candidate in candidates)
            {
                if (candidate.LastAccessTime >= expire)
                {
                    // The sorset will have sorted the modification time.
                    break;
                }

                fileSystem.Delete(candidate.Path);
                candidates.Remove(candidate);
                freeSpace += candidate.Size;
                deletedFiles++;
            }

            void PromptFree()
            {
                io.WriteError($"Cache garbage collection completed, delete {deletedFiles} files, free {AbstractHelper.FormatMemory(freeSpace)} space.");
            }

            // gc with maxSize
            var totalSize = fileSystem.GetMetaData(CacheDirectory).Size;
            if (totalSize < maxSize)
            {
                PromptFree();
                return true;
            }

            foreach (var candidate in candidates)
            {
                if (totalSize < maxSize)
                {
                    break;
                }

                fileSystem.Delete(candidate.Path);
                totalSize -= candidate.Size;
                freeSpace += candidate.Size;
                deletedFiles++;
            }

            PromptFree();
            return true;
        }

        /// <summary>
        /// Verify if it is a file name in whitlist.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <returns>Returns secure whitelist name.</returns>
        protected virtual string ValidateWhiteList(string file)
        {
            if (string.IsNullOrEmpty(whitelist))
            {
                return file;
            }

            return Regex.Replace(file, $"[^{whitelist}]", "-", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Verify path prefix.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <returns>Return the path with the path prefix.</returns>
        protected virtual string ValidatePathPrefix(string file)
        {
            return Path.Combine(CacheDirectory, file);
        }
    }
}

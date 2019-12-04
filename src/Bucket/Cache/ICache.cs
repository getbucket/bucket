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

using System.IO;

namespace Bucket.Cache
{
    /// <summary>
    /// Represents a cache system.
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Gets or sets a value indicating whether the cache system enabled.
        /// </summary>
        bool Enable { get; set; }

        /// <summary>
        /// Gets the cache directory.
        /// </summary>
        string CacheDirectory { get; }

        /// <summary>
        /// Read the cache file.
        /// </summary>
        /// <param name="file">The cache file name.</param>
        /// <param name="touch">Whether is update the ttl.</param>
        /// <returns>Returns the stream of the cache file.</returns>
        Stream Read(string file, bool touch = true);

        /// <summary>
        /// Try read the cache file. if the file not exists return false.
        /// </summary>
        /// <param name="file">The cache file name.</param>
        /// <param name="stream">The stream of the cache file.</param>
        /// <param name="touch">Whether is update the ttl.</param>
        /// <returns>True if the file readed.</returns>
        bool TryRead(string file, out Stream stream, bool touch = true);

        /// <summary>
        /// Whether the cache file is exists.
        /// </summary>
        /// <param name="file">The cache file name.</param>
        /// <returns>True if the file exists.</returns>
        bool Contains(string file);

        /// <summary>
        /// Write in the cache file.
        /// </summary>
        /// <param name="file">The cache file name.</param>
        /// <param name="stream">The cache stream.</param>
        void Write(string file, Stream stream);

        /// <summary>
        /// Delete the cache file.
        /// </summary>
        /// <param name="file">The cache file name.</param>
        void Delete(string file);

        /// <summary>
        /// Empty all cache file.
        /// </summary>
        void Clear();

        /// <summary>
        /// Perform garbage collection.
        /// </summary>
        /// <param name="ttl">The time to live(The unit is seconds).</param>
        /// <param name="maxSize">The maximum size allowed by the total cache size(The unit is bytes).</param>
        /// <returns>True if execution of the GC program.</returns>
        bool GC(int ttl, int maxSize);
    }
}

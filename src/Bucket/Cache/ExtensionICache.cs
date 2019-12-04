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
using Bucket.Util;
using System.IO;
using System.Text;

namespace Bucket.Cache
{
    /// <summary>
    /// <see cref="ICache"/> extension method.
    /// </summary>
    public static class ExtensionICache
    {
        /// <summary>
        /// Try read the cache file. if the file not exists return false.
        /// </summary>
        /// <param name="cache">The cache system instance.</param>
        /// <param name="file">The cache file name.</param>
        /// <param name="content">The content of the cache file.</param>
        /// <param name="touch">Whether is update the ttl.</param>
        /// <returns>True if the file readed.</returns>
        public static bool TryRead(this ICache cache, string file, out string content, bool touch = true)
        {
            content = null;
            if (!cache.Contains(file))
            {
                return false;
            }

            using (var stream = cache.Read(file, touch))
            {
                content = stream.ToText(closed: false);
            }

            return true;
        }

        /// <summary>
        /// Try read the cache and check the checksum.
        /// </summary>
        /// <param name="cache">The cache system instance.</param>
        /// <param name="file">The cache file name.</param>
        /// <param name="content">The content of the cache file.</param>
        /// <param name="sha256">The checksum code.</param>
        /// <param name="touch">Whether is touch the file.</param>
        /// <returns>True if cache exists and verifies the checksum. otherwise false.</returns>
        public static bool TryReadSha256(this ICache cache, string file, out string content, string sha256 = null, bool touch = true)
        {
            content = null;
            if (!cache.Sha256(file, sha256))
            {
                return false;
            }

            using (var stream = cache.Read(file, touch))
            {
                content = stream.ToText(closed: false);
            }

            return true;
        }

        /// <summary>
        /// Verify that the cache file and checksum are equal.
        /// </summary>
        /// <param name="cache">The cache system instance.</param>
        /// <param name="file">The cache file name.</param>
        /// <param name="sha256">The checksum code.</param>
        /// <returns>True if verifies the checksum. False if file not exists or not match checksum.</returns>
        public static bool Sha256(this ICache cache, string file, string sha256)
        {
            if (!cache.Contains(file))
            {
                return false;
            }

            using (var stream = cache.Read(file, false))
            {
                return string.IsNullOrEmpty(sha256) || Security.Sha256(stream) == sha256;
            }
        }

        /// <summary>
        /// Verify that the cache file and checksum are equal.
        /// </summary>
        /// <param name="cache">The cache system instance.</param>
        /// <param name="file">The cache file name.</param>
        /// <param name="sha1">The checksum code.</param>
        /// <returns>True if verifies the checksum. False if file not exists or not match checksum.</returns>
        public static bool Sha1(this ICache cache, string file, string sha1)
        {
            if (!cache.Contains(file))
            {
                return false;
            }

            using (var stream = cache.Read(file, false))
            {
                return string.IsNullOrEmpty(sha1) || Security.Sha1(stream) == sha1;
            }
        }

        /// <summary>
        /// Write in the cache file.
        /// </summary>
        /// <param name="cache">The cache system instance.</param>
        /// <param name="file">The cache file name.</param>
        /// <param name="content">The cache stream.</param>
        public static void Write(this ICache cache, string file, string content)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                cache.Write(file, stream);
            }
        }

        /// <summary>
        /// Copy a file out of the cache to local disk.
        /// </summary>
        /// <param name="cache">The cache system instance.</param>
        /// <param name="file">The cache file name.</param>
        /// <param name="target">Absolute path to the local disk.</param>
        /// <returns>True if the copy successful.</returns>
        public static bool CopyTo(this ICache cache, string file, string target)
        {
            if (!cache.Enable)
            {
                return false;
            }

            var local = new FileSystemLocal();
            using (var stream = cache.Read(file))
            {
                local.Write(target, stream);
            }

            return true;
        }

        /// <summary>
        /// Copy a local file into the cache.
        /// </summary>
        /// <param name="cache">The cache system instance.</param>
        /// <param name="file">The cache file name.</param>
        /// <param name="source">The source file absolute path.</param>
        /// <returns>True if the copy successful.</returns>
        public static bool CopyFrom(this ICache cache, string file, string source)
        {
            if (!cache.Enable)
            {
                return false;
            }

            var local = new FileSystemLocal();
            using (var stream = local.Read(source))
            {
                cache.Write(file, stream);
            }

            return true;
        }
    }
}

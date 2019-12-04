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

using System;

namespace Bucket.FileSystem
{
    /// <summary>
    /// The metadata from file or folder.
    /// </summary>
    public interface IMetaData
    {
        /// <summary>
        /// Gets the file or folder name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the file or folder path.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets the file or folder parent path.
        /// </summary>
        string ParentDirectory { get; }

        /// <summary>
        /// Gets the mime type.
        /// </summary>
        string MimeType { get; }

        /// <summary>
        /// Gets a value indicating whether is directory.
        /// </summary>
        bool IsDirectory { get; }

        /// <summary>
        /// Gets file or folder size (bytes).
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets the last modified time of the file or folder.
        /// </summary>
        DateTime LastModified { get; }

        /// <summary>
        /// Gets or sets the last access time.
        /// </summary>
        DateTime LastAccessTime { get; set; }

        /// <summary>
        /// Refresh the cache.
        /// </summary>
        void Refresh();
    }
}

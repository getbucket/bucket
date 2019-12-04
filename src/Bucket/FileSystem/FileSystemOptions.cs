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
    /// Represents a file system option.
    /// </summary>
    [Flags]
    public enum FileSystemOptions
    {
        /// <summary>
        /// Operate the file.
        /// </summary>
        File = 1,

        /// <summary>
        /// Operate the directory.
        /// </summary>
        Directory = 2,
    }
}

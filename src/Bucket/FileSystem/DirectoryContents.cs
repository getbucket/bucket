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

using Bucket.Util;
using System;
using System.Linq;

namespace Bucket.FileSystem
{
    /// <summary>
    /// Represents the contents of a folder.
    /// </summary>
    public struct DirectoryContents : IEquatable<DirectoryContents>
    {
        private string[] directories;
        private string[] files;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryContents"/> struct.
        /// </summary>
        /// <param name="directories">An array of folders.</param>
        /// <param name="files">An array of files.</param>
        public DirectoryContents(string[] directories, string[] files)
        {
            this.directories = directories;
            this.files = files;
        }

        /// <summary>
        /// Overlay two folder contents.
        /// </summary>
        /// <param name="left">The left content.</param>
        /// <param name="right">The right content.</param>
        /// <returns>Returns new directory contents.</returns>
        public static DirectoryContents operator +(DirectoryContents left, DirectoryContents right)
        {
            return Add(left, right);
        }

        /// <summary>
        /// Subtract content from the right folder.
        /// </summary>
        /// <param name="left">The left content.</param>
        /// <param name="right">The right content.</param>
        /// <returns>Returns new directory contents.</returns>
        public static DirectoryContents operator -(DirectoryContents left, DirectoryContents right)
        {
            return Subtract(left, right);
        }

        public static bool operator ==(DirectoryContents left, DirectoryContents right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DirectoryContents left, DirectoryContents right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Overlay two folder contents.
        /// </summary>
        /// <param name="left">The left content.</param>
        /// <param name="right">The right content.</param>
        /// <returns>Returns new directory contents.</returns>
        public static DirectoryContents Add(DirectoryContents left, DirectoryContents right)
        {
            return new DirectoryContents
            {
                files = Arr.Merge(left.files, right.files),
                directories = Arr.Merge(left.directories, right.directories),
            };
        }

        /// <summary>
        /// Subtract content from the right folder.
        /// </summary>
        /// <param name="left">The left content.</param>
        /// <param name="right">The right content.</param>
        /// <returns>Returns new directory contents.</returns>
        public static DirectoryContents Subtract(DirectoryContents left, DirectoryContents right)
        {
            return new DirectoryContents
            {
                files = Arr.Difference(left.files, right.files),
                directories = Arr.Difference(left.directories, right.directories),
            };
        }

        /// <summary>
        /// Gets an array of folders.
        /// </summary>
        /// <returns>Returns an array of folders.</returns>
        public string[] GetDirectories()
        {
            return directories ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets an array of files.
        /// </summary>
        /// <returns>Returns an array of files.</returns>
        public string[] GetFiles()
        {
            return files ?? Array.Empty<string>();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is DirectoryContents content))
            {
                return false;
            }

            return Equals(content);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return 0;
        }

        /// <inheritdoc />
        public bool Equals(DirectoryContents other)
        {
            if (!Enumerable.SequenceEqual(directories, other.directories)
                || !Enumerable.SequenceEqual(files, other.files))
            {
                return false;
            }

            return true;
        }
    }
}

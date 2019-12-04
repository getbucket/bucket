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

using Bucket.Archive.Filter;
using Bucket.FileSystem;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Bucket.Archive
{
    /// <summary>
    /// Get all files that can be archived.
    /// </summary>
    public class ArchivableFilesFinder : IEnumerable<string>
    {
        private readonly IFileSystem fileSystem;
        private readonly string sources;
        private readonly IEnumerable<BaseExcludeFilter> filters;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchivableFilesFinder"/> class.
        /// </summary>
        public ArchivableFilesFinder(string sources, string[] excludes = null, bool ignoreFilters = false, IFileSystem fileSystem = null)
        {
            this.fileSystem = fileSystem ?? new FileSystemLocal();
            this.sources = BaseFileSystem.GetNormalizePath(sources);

            if (ignoreFilters)
            {
                filters = Array.Empty<BaseExcludeFilter>();
            }
            else
            {
                filters = new[]
                {
                    new ExcludeFilterBucket(excludes),
                };
            }
        }

        /// <inheritdoc />
        public IEnumerator<string> GetEnumerator()
        {
            string RelativePath(string path)
            {
                path = BaseFileSystem.GetNormalizePath(path);
                if (path.StartsWith(sources, StringComparison.Ordinal))
                {
                    return path.Substring(sources.Length);
                }

                return path;
            }

            IEnumerable<string> Iterative(DirectoryContents contents)
            {
                foreach (var dirPath in contents.GetDirectories())
                {
                    // Folders all end with "/".
                    var relativePath = $"{RelativePath(dirPath.TrimEnd('/', '\\'))}/";
                    if (Filter(relativePath))
                    {
                        continue;
                    }

                    yield return relativePath;
                    foreach (var subRelativePath in Iterative(fileSystem.GetContents(dirPath)))
                    {
                        yield return subRelativePath;
                    }
                }

                foreach (var filePath in contents.GetFiles())
                {
                    var relativePath = RelativePath(filePath);
                    if (!Filter(relativePath))
                    {
                        yield return relativePath;
                    }
                }
            }

            foreach (var path in Iterative(fileSystem.GetContents(sources)))
            {
                // Eliminate the pre-separator of the relative path.
                yield return path.TrimStart('/');
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Checks all exclude patterns in filter.
        /// </summary>
        /// <param name="relativePath">The file or dir's path.</param>
        /// <returns>The file should be excluded.</returns>
        protected virtual bool Filter(string relativePath)
        {
            var exclude = false;
            foreach (var filter in filters)
            {
                exclude = filter.Filter(relativePath, exclude);
            }

            return exclude;
        }
    }
}

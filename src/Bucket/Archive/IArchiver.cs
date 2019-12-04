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

namespace Bucket.Archive
{
    /// <summary>
    /// All archiver implementations must implement this interface.
    /// </summary>
    public interface IArchiver
    {
        /// <summary>
        /// Create an archive from the sources.
        /// </summary>
        /// <param name="sources">The sources directory.</param>
        /// <param name="target">The target file.</param>
        /// <param name="excludes">A list of patterns for files to archives to exclude.</param>
        /// <param name="ignoreFilters">Whether is ignore the file filters.</param>
        /// <returns>The path to the written archive file.</returns>
        string Archive(string sources, string target, string[] excludes = null, bool ignoreFilters = false);
    }
}

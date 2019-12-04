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

namespace Bucket.FileSystem
{
    /// <summary>
    /// Represents a full path reporter.
    /// </summary>
    public interface IReportPath
    {
        /// <summary>
        /// Apply the root path prefix to the specified path.
        /// </summary>
        /// <param name="path">The specified path.</param>
        /// <returns>Returns path to which the root path has been applied.</returns>
        string ApplyRootPath(string path);

        /// <summary>
        /// Remove the path prefix of the specified path.
        /// </summary>
        /// <param name="path">The specified path.</param>
        /// <returns>Removed the prefix with the path.</returns>
        string RemoveRootPath(string path);
    }
}

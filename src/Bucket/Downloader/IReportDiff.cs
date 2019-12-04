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

using Bucket.Package;

namespace Bucket.Downloader
{
    /// <summary>
    /// Represents a diff reporter.
    /// </summary>
    public interface IReportDiff
    {
        /// <summary>
        /// Get changes without push.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="cwd">The specific folder.</param>
        /// <returns>Return changes log.</returns>
        string GetUnpushedChanges(IPackage package, string cwd);
    }
}

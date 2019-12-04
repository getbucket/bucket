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
    /// Reporter is able to monitor file changes.
    /// </summary>
    public interface IReportChange
    {
        /// <summary>
        /// Checks for changes to the local copy.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="cwd">The specific folder.</param>
        /// <returns>Returns changes or null.</returns>
        string GetLocalChanges(IPackage package, string cwd);
    }
}

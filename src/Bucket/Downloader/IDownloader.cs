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
using System.Threading.Tasks;

namespace Bucket.Downloader
{
    /// <summary>
    /// Represents a download interface.
    /// </summary>
    public interface IDownloader
    {
        /// <summary>
        /// Gets a value indicates installation source.
        /// </summary>
        InstallationSource InstallationSource { get; }

        /// <summary>
        /// This should do any network-related tasks to prepare for install/update.
        /// </summary>
        /// <param name="package">Which package do related tasks.</param>
        /// <param name="cwd">Download to target dir.</param>
        /// <returns>Return an asynchronous task.</returns>
        Task Download(IPackage package, string cwd);

        /// <summary>
        /// Install specific package into specific folder.
        /// </summary>
        /// <param name="package">The specific package.</param>
        /// <param name="cwd">Install to target dir.</param>
        void Install(IPackage package, string cwd);

        /// <summary>
        /// Updates specific package in specific folder from <paramref name="initial"/> to <paramref name="target"/> version..
        /// </summary>
        /// <param name="initial">The initial package.</param>
        /// <param name="target">The target package.</param>
        /// <param name="cwd">Update from target dir.</param>
        void Update(IPackage initial, IPackage target, string cwd);

        /// <summary>
        /// Removes specific package from specific folder.
        /// </summary>
        /// <param name="package">The specific package.</param>
        /// <param name="cwd">Removed from target dir.</param>
        void Remove(IPackage package, string cwd);
    }
}

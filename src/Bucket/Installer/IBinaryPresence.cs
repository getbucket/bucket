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

namespace Bucket.Installer
{
    /// <summary>
    /// Interface for the package installation manager that handle binary installation.
    /// </summary>
    public interface IBinaryPresence
    {
        /// <summary>
        /// Make sure binaries are installed for a given package.
        /// </summary>
        /// <param name="package">The package instance.</param>
        void EnsureBinariesPresence(IPackage package);
    }
}

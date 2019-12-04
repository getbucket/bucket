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

namespace Bucket.Repository
{
    /// <summary>
    /// Represents a repository that can be written. Usually used as a local repository.
    /// </summary>
    public interface IRepositoryWriteable : IRepository
    {
        /// <summary>
        /// Write repository data to media.
        /// </summary>
        void Write();

        /// <summary>
        /// Adds a new package to the repository.
        /// </summary>
        /// <param name="package">The added package instance.</param>
        void AddPackage(IPackage package);

        /// <summary>
        /// Removes package from repository.
        /// </summary>
        /// <param name="package">The package instance.</param>
        void RemovePackage(IPackage package);

        /// <summary>
        /// Get unique packages (at most one package of each name), with aliases resolved and removed.
        /// </summary>
        IPackage[] GetCanonicalPackages();

        /// <summary>
        /// Forces a reload of all packages.
        /// </summary>
        void Reload();
    }
}

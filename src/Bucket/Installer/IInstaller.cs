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
using Bucket.Repository;
using System.Threading.Tasks;

namespace Bucket.Installer
{
    /// <summary>
    /// Interface for the package installation.
    /// </summary>
    public interface IInstaller
    {
        /// <summary>
        /// Decides if the installer supports the given type.
        /// </summary>
        /// <param name="packageType">The package type.</param>
        /// <returns>True if the installer support.</returns>
        bool IsSupports(string packageType);

        /// <summary>
        /// Whether the provided package is installed.
        /// </summary>
        /// <param name="repository">Detect in a given repository.</param>
        /// <param name="package">The provide package.</param>
        /// <returns>True if the provided package is installed.</returns>
        bool IsInstalled(IRepositoryInstalled repository, IPackage package);

        /// <summary>
        /// Downloads the files needed to later install the given package.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="previousPackage">The previous package instance in case of an update.</param>
        /// <returns>Return an asynchronous task.</returns>
        Task Download(IPackage package, IPackage previousPackage);

        /// <summary>
        /// Installs specific package.
        /// </summary>
        /// <param name="repository">The repository in which to check.</param>
        /// <param name="package">The package instance.</param>
        void Install(IRepositoryInstalled repository, IPackage package);

        /// <summary>
        /// Updates specific package.
        /// </summary>
        /// <param name="repository">The repository in which to check.</param>
        /// <param name="initial">Already installed package version.</param>
        /// <param name="target">The version of the package you want to update.</param>
        void Update(IRepositoryInstalled repository, IPackage initial, IPackage target);

        /// <summary>
        /// Uninstalls specific package.
        /// </summary>
        /// <param name="repository">The repository in which to check.</param>
        /// <param name="package">The package instance.</param>
        void Uninstall(IRepositoryInstalled repository, IPackage package);

        /// <summary>
        /// Gets the installation path of a package.
        /// </summary>
        /// <param name="package">The package instance.</param>
        string GetInstallPath(IPackage package);
    }
}

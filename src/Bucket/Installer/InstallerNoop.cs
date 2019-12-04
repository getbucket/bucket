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
using GameBox.Console.Exception;
using System.Threading.Tasks;

namespace Bucket.Installer
{
    /// <summary>
    /// Does not install anything but marks packages installed in the repository.
    /// It's very useful to dry run.
    /// </summary>
    public sealed class InstallerNoop : IInstaller
    {
        /// <inheritdoc />
        public Task Download(IPackage package, IPackage previousPackage)
        {
            return Task.Delay(0);
        }

        /// <inheritdoc />
        public string GetInstallPath(IPackage package)
        {
            return package.GetNamePretty();
        }

        /// <inheritdoc />
        public void Install(IRepositoryInstalled repository, IPackage package)
        {
            if (!IsInstalled(repository, package))
            {
                repository.AddPackage((IPackage)package.Clone());
            }
        }

        /// <inheritdoc />
        public bool IsInstalled(IRepositoryInstalled repository, IPackage package)
        {
            return repository.HasPackage(package);
        }

        /// <inheritdoc />
        public bool IsSupports(string packageType)
        {
            return true;
        }

        /// <inheritdoc />
        public void Uninstall(IRepositoryInstalled repository, IPackage package)
        {
            if (!IsInstalled(repository, package))
            {
                throw new InvalidArgumentException($"Package is not installed: {package}");
            }

            repository.RemovePackage(package);
        }

        /// <inheritdoc />
        public void Update(IRepositoryInstalled repository, IPackage initial, IPackage target)
        {
            Uninstall(repository, initial);
            Install(repository, target);
        }
    }
}

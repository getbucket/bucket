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

using Bucket.IO;
using Bucket.Package;
using Bucket.Package.Version;
using Bucket.Repository;
using GameBox.Console.Exception;
using System.Threading.Tasks;

namespace Bucket.Installer
{
    /// <summary>
    /// Represents a metadata package installer.
    /// </summary>
    /// <remarks>
    /// According to: https://help.ubuntu.com/community/MetaPackages
    /// proposed concept.
    /// </remarks>
    public sealed class InstallerMetaPackage : IInstaller
    {
        /// <summary>
        /// Indicates the meta package type.
        /// </summary>
        public const string PackageType = "metapackage";
        private readonly IIO io;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallerMetaPackage"/> class.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        public InstallerMetaPackage(IIO io)
        {
            this.io = io;
        }

        /// <inheritdoc />
        public Task Download(IPackage package, IPackage previousPackage)
        {
            return Task.Delay(0);
        }

        /// <inheritdoc />
        public string GetInstallPath(IPackage package)
        {
            return string.Empty;
        }

        /// <inheritdoc />
        public void Install(IRepositoryInstalled repository, IPackage package)
        {
            io.WriteError($"  - Installing <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>)");
            repository.AddPackage((IPackage)package.Clone());
        }

        /// <inheritdoc />
        public bool IsInstalled(IRepositoryInstalled repository, IPackage package)
        {
            return repository.HasPackage(package);
        }

        /// <inheritdoc />
        public bool IsSupports(string packageType)
        {
            return packageType == PackageType;
        }

        /// <inheritdoc />
        public void Uninstall(IRepositoryInstalled repository, IPackage package)
        {
            if (!repository.HasPackage(package))
            {
                throw new InvalidArgumentException($"Package is not installed: {package}");
            }

            io.WriteError($"  - Removing <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>)");
            repository.RemovePackage(package);
        }

        /// <inheritdoc />
        public void Update(IRepositoryInstalled repository, IPackage initial, IPackage target)
        {
            if (!repository.HasPackage(initial))
            {
                throw new InvalidArgumentException($"Package is not installed: {initial}");
            }

            var name = target.GetName();
            var from = initial.GetVersionPrettyFull();
            var to = target.GetVersionPrettyFull();
            var actionName = VersionParser.IsUpgrade(initial.GetVersion(), target.GetVersion()) ? "Updating" : "Downgrading";

            io.WriteError($"  - {actionName} <info>{name}</info> (<comment>{from}</comment> => <comment>{to}</comment>)");

            repository.RemovePackage(initial);
            repository.AddPackage((IPackage)target.Clone());
        }
    }
}

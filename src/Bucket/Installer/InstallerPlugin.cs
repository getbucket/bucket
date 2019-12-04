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

using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Plugin;
using Bucket.Repository;
using SException = System.Exception;

namespace Bucket.Installer
{
    /// <summary>
    /// Represents a plugin installer.
    /// </summary>
    public class InstallerPlugin : InstallerLibrary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstallerPlugin"/> class.
        /// </summary>
        public InstallerPlugin(IIO io, Bucket bucket, IFileSystem fileSystem = null, InstallerBinary installerBinary = null)
            : base(io, bucket, PluginManager.PluginType, fileSystem, installerBinary)
        {
            // noop.
        }

        /// <inheritdoc />
        public override bool IsSupports(string packageType)
        {
            return packageType == PluginManager.PluginType;
        }

        /// <inheritdoc />
        public override void Install(IRepositoryInstalled repository, IPackage package)
        {
            base.Install(repository, package);

            try
            {
                GetBucket().GetPluginManager().ActivatePackages(package, true);
            }
            catch (SException)
            {
                // Rollback installation
                GetIO().WriteError("Plugin installation failed, rolling back.");
                Uninstall(repository, package);
                throw;
            }
        }

        /// <inheritdoc />
        public override void Update(IRepositoryInstalled repository, IPackage initial, IPackage target)
        {
            base.Update(repository, initial, target);

            try
            {
                GetBucket().GetPluginManager().DeactivatePackage(initial);
                GetBucket().GetPluginManager().ActivatePackages(target, true);
            }
            catch (SException)
            {
                // Rollback installation
                GetIO().WriteError("Plugin initialization failed, rolling back.");
                Uninstall(repository, target);
                Install(repository, initial);
                throw;
            }
        }

        /// <inheritdoc />
        public override void Uninstall(IRepositoryInstalled repository, IPackage package)
        {
            GetBucket().GetPluginManager().UninstallPackage(package);
            base.Uninstall(repository, package);
        }
    }
}

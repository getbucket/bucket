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

using Bucket.Configuration;
using Bucket.Downloader;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Repository;
using GameBox.Console.Exception;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Bucket.Installer
{
    /// <summary>
    /// Represents a library installer.
    /// </summary>
    public class InstallerLibrary : IInstaller, IBinaryPresence
    {
        private readonly IIO io;
        private readonly Bucket bucket;
        private readonly string type;
        private readonly string vendorDir;
        private readonly IFileSystem fileSystem;
        private readonly DownloadManager downloadManager;
        private readonly InstallerBinary installerBinary;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallerLibrary"/> class.
        /// </summary>
        public InstallerLibrary(IIO io, Bucket bucket, string type = "library", IFileSystem fileSystem = null, InstallerBinary installerBinary = null)
        {
            this.io = io;
            this.bucket = bucket;
            this.type = type;
            this.fileSystem = fileSystem ?? new FileSystemLocal();
            downloadManager = bucket.GetDownloadManager();

            var config = bucket.GetConfig();
            vendorDir = config.Get(Settings.VendorDir);
            this.installerBinary = installerBinary ??
                new InstallerBinary(io, config.Get(Settings.BinDir), config.Get(Settings.BinCompat), fileSystem);
        }

        /// <inheritdoc />
        public virtual Task Download(IPackage package, IPackage previousPackage)
        {
            var cwd = GetInstallPath(package);
            return downloadManager.Download(package, cwd, previousPackage);
        }

        /// <inheritdoc />
        public virtual string GetInstallPath(IPackage package)
        {
            var vendor = GetVendorDir();
            return (string.IsNullOrEmpty(vendor) ? string.Empty : $"{vendor}/") + package.GetNamePretty();
        }

        /// <inheritdoc />
        public virtual void EnsureBinariesPresence(IPackage package)
        {
            installerBinary.Install(package, GetInstallPath(package), false);
        }

        /// <inheritdoc />
        public virtual void Install(IRepositoryInstalled repository, IPackage package)
        {
            var installPath = GetInstallPath(package);

            // remove the binaries if it appears the package files are missing.
            if (!fileSystem.Exists(installPath, FileSystemOptions.Directory) &&
                repository.HasPackage(package))
            {
                installerBinary.Remove(package);
            }

            InstallLibaray(package);
            installerBinary.Install(package, installPath);

            if (!repository.HasPackage(package))
            {
                repository.AddPackage((IPackage)package.Clone());
            }
        }

        /// <inheritdoc />
        public virtual bool IsInstalled(IRepositoryInstalled repository, IPackage package)
        {
            if (!repository.HasPackage(package))
            {
                return false;
            }

            return fileSystem.Exists(GetInstallPath(package), FileSystemOptions.Directory);
        }

        /// <inheritdoc />
        public virtual bool IsSupports(string packageType)
        {
            return packageType == type || string.IsNullOrEmpty(type);
        }

        /// <inheritdoc />
        public virtual void Uninstall(IRepositoryInstalled repository, IPackage package)
        {
            if (!repository.HasPackage(package))
            {
                throw new InvalidArgumentException($"Package is not installed: {package}");
            }

            RemoveLibrary(package);
            installerBinary.Remove(package);
            repository.RemovePackage(package);

            var installPath = GetInstallPath(package);

            // If the package comes with a vendor, we also need to
            // check if the provider directory is empty.
            if (package.GetName().Contains("/"))
            {
                var venderDir = Path.GetDirectoryName(installPath);
                if (fileSystem.IsEmptyDirectory(venderDir))
                {
                    fileSystem.Delete(venderDir);
                }
            }
        }

        /// <inheritdoc />
        public virtual void Update(IRepositoryInstalled repository, IPackage initial, IPackage target)
        {
            if (!repository.HasPackage(initial))
            {
                throw new InvalidArgumentException($"Package is not installed: {initial}");
            }

            installerBinary.Remove(initial);
            UpdateLibrary(initial, target);
            installerBinary.Install(target, GetInstallPath(target));

            repository.RemovePackage(initial);
            if (!repository.HasPackage(target))
            {
                repository.AddPackage((IPackage)target.Clone());
            }
        }

        /// <summary>
        /// Installation library file.
        /// </summary>
        /// <param name="package">The package instance.</param>
        protected virtual void InstallLibaray(IPackage package)
        {
            var cwd = GetInstallPath(package);
            downloadManager.Install(package, cwd);
        }

        /// <summary>
        /// Removed the library file.
        /// </summary>
        /// <param name="package">The package instance.</param>
        protected virtual void RemoveLibrary(IPackage package)
        {
            var cwd = GetInstallPath(package);
            downloadManager.Remove(package, cwd);
        }

        /// <summary>
        /// Update the library file.
        /// </summary>
        protected virtual void UpdateLibrary(IPackage initial, IPackage target)
        {
            var initialPath = GetInstallPath(initial);
            var targetPath = GetInstallPath(target);

            if (initialPath != targetPath)
            {
                // if the target and initial dirs intersect, we force a
                // remove + install to avoid the rename wiping the target
                // dir as part of the initial dir cleanup.
                if (initialPath.StartsWith(targetPath, StringComparison.Ordinal) ||
                    targetPath.StartsWith(initialPath, StringComparison.Ordinal))
                {
                    RemoveLibrary(initial);
                    InstallLibaray(target);
                    return;
                }

                fileSystem.Move(initialPath, targetPath);
            }

            downloadManager.Update(initial, target, targetPath);
        }

        /// <summary>
        /// Get an absolute path to represent a vendor dir.
        /// </summary>
        protected string GetVendorDir()
        {
            return Path.Combine(Environment.CurrentDirectory, vendorDir).TrimEnd('/', '\\');
        }

        /// <summary>
        /// Gets the bucket instance.
        /// </summary>
        protected Bucket GetBucket()
        {
            return bucket;
        }

        /// <summary>
        /// Gets the io instance.
        /// </summary>
        protected IIO GetIO()
        {
            return io;
        }
    }
}

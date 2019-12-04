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
using Bucket.Exception;
using Bucket.Installer;
using Bucket.IO;
using Bucket.Package.Dumper;
using Bucket.Semver;
using Bucket.Util;
using Bucket.Util.SCM;
using GameBox.Console.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bucket.Package
{
    /// <summary>
    /// Represents a locker build container.
    /// </summary>
    internal sealed class LockerBuilder
    {
        private readonly IProcessExecutor process;
        private readonly DumperPackage dumper;
        private readonly Func<ConfigLocker, bool> doSetLockData;
        private readonly InstallationManager installationManager;
        private readonly IPackage[] packages;
        private IPackage[] packagesDev;
        private Stabilities? minimumStability;
        private bool? preferStable;
        private bool? preferLowest;
        private ConfigAlias[] aliases;
        private IDictionary<string, Stabilities> stabilityFlags;
        private IDictionary<string, string> platforms;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockerBuilder"/> class.
        /// </summary>
        public LockerBuilder(IPackage[] packages, Func<ConfigLocker, bool> doSetLockData, InstallationManager installationManager, IIO io = null)
        {
            this.packages = packages;
            this.doSetLockData = doSetLockData;
            this.installationManager = installationManager;
            dumper = new DumperPackage();
            process = new BucketProcessExecutor(io);
        }

        /// <summary>
        /// Sets the packages for dev mode.
        /// </summary>
        public LockerBuilder SetPackagesDev(IPackage[] packagesDev)
        {
            this.packagesDev = packagesDev;
            return this;
        }

        /// <summary>
        /// Sets the minimun stability.
        /// </summary>
        public LockerBuilder SetMinimumStability(Stabilities? minimumStability)
        {
            this.minimumStability = minimumStability;
            return this;
        }

        /// <summary>
        /// Sets whether is prefer stable.
        /// </summary>
        public LockerBuilder SetPreferStable(bool? preferStable)
        {
            this.preferStable = preferStable;
            return this;
        }

        /// <summary>
        /// Sets whether is prefer lowest.
        /// </summary>
        public LockerBuilder SetPreferLowest(bool? preferLowest)
        {
            this.preferLowest = preferLowest;
            return this;
        }

        /// <summary>
        /// Sets the package aliases.
        /// </summary>
        public LockerBuilder SetAliases(ConfigAlias[] aliases)
        {
            this.aliases = aliases;
            return this;
        }

        /// <summary>
        /// Sets the stability flags relationship of the require(include dev) package.
        /// </summary>
        public LockerBuilder SetStabilityFlags(IDictionary<string, Stabilities> stabilityFlags)
        {
            this.stabilityFlags = stabilityFlags;
            return this;
        }

        /// <summary>
        /// Sets the manually configured platform package information.
        /// </summary>
        public LockerBuilder SetPlatforms(IDictionary<string, string> platforms)
        {
            this.platforms = platforms;
            return this;
        }

        /// <summary>
        /// Save the data in lock file.
        /// </summary>
        public bool Save()
        {
            // Even empty objects should be written.
            var locker = new ConfigLocker
            {
                Packages = LockPackages(packages),
                PackagesDev = LockPackages(packagesDev),
                Aliases = aliases ?? Array.Empty<ConfigAlias>(),
                StabilityFlags = NormalizeStabilityFlags(stabilityFlags),
                Platforms = platforms ?? new Dictionary<string, string>(),
            };

            if (minimumStability != null)
            {
                locker.MinimumStability = minimumStability.Value;
            }

            if (preferStable != null)
            {
                locker.PreferStable = preferStable.Value;
            }

            if (preferLowest != null)
            {
                locker.PreferLowest = preferLowest.Value;
            }

            return doSetLockData(locker);
        }

        private static IDictionary<string, Stabilities> NormalizeStabilityFlags(IDictionary<string, Stabilities> collection)
        {
            if (collection == null)
            {
                return new Dictionary<string, Stabilities>();
            }

            if (collection is SortedDictionary<string, Stabilities>)
            {
                return collection;
            }

            return new SortedDictionary<string, Stabilities>(collection);
        }

        private ConfigLockerPackage[] LockPackages(IPackage[] packages)
        {
            if (packages.Empty())
            {
                return Array.Empty<ConfigLockerPackage>();
            }

            var locked = new LinkedList<ConfigLockerPackage>();

            foreach (var package in packages)
            {
                if (package is PackageAlias)
                {
                    continue;
                }

                var name = package.GetNamePretty();
                var version = package.GetVersionPretty();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                {
                    throw new RuntimeException($"Package \"{package}\" has no version or name and can not be locked.");
                }

                var dumped = dumper.Dump<ConfigLockerPackage>(package);

                if (package.IsDev && package.GetInstallationSource() == InstallationSource.Source)
                {
                    dumped.ReleaseDate = GetPackageTimeFromSource(package);
                }

                locked.AddLast(dumped);
            }

            var ret = locked.ToArray();

            Array.Sort(ret, (x, y) =>
            {
                var comparison = string.CompareOrdinal(x.Name, y.Name);
                if (comparison != 0)
                {
                    return comparison;
                }

                return string.CompareOrdinal(x.Version, y.Version);
            });

            return ret;
        }

        private DateTime? GetPackageTimeFromSource(IPackage package)
        {
            var installedPath = Path.Combine(Environment.CurrentDirectory, installationManager.GetInstalledPath(package));
            var sourceType = package.GetSourceType();
            DateTime? ret = null;

            if (string.IsNullOrEmpty(installedPath) || !Array.Exists(new[] { "git" }, (item) => item == sourceType))
            {
                return null;
            }

            var sourceReference = package.GetSourceReference() ?? package.GetDistReference();

            DateTime? GetGitDateTime()
            {
                Git.CleanEnvironment();
                if (process.Execute(
                    $"git log -n1 --pretty=%aD {ProcessExecutor.Escape(sourceReference)}",
                    out string output, installedPath) == 0 &&
                    DateTime.TryParse(output.Trim(), out DateTime dateTime))
                {
                    return dateTime;
                }

                return null;
            }

            switch (sourceReference)
            {
                case "git":
                    ret = GetGitDateTime();
                    break;
                default:
                    break;
            }

            return ret;
        }
    }
}

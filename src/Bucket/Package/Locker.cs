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
using Bucket.Json;
using Bucket.Package.Loader;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Bucket.Package
{
    /// <summary>
    /// Represents a bucket.lock file.
    /// The bucket.lock file suggestion be commit to your vcs.
    /// </summary>
    /// <remarks>
    /// Used to lock versions to ensure that different devices
    /// use the same version of dependency packages.
    /// </remarks>
    public class Locker
    {
        private readonly IIO io;
        private readonly JsonFile lockFile;
        private readonly InstallationManager installationManager;
        private readonly string contentHash;
        private readonly LoaderPackage loader;
        private ConfigLocker cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="Locker"/> class.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="lockFile">The lock file loader instance.</param>
        /// <param name="installationManager">The installation manager instance.</param>
        /// <param name="bucketFileContents">The contents of the bucket.json file.</param>
        public Locker(IIO io, JsonFile lockFile, InstallationManager installationManager, string bucketFileContents)
        {
            this.io = io;
            this.lockFile = lockFile;
            this.installationManager = installationManager;
            contentHash = GetContentHash(bucketFileContents);
            loader = new LoaderPackage(null);
        }

        /// <summary>
        /// Gets the md5 hash of the sorted content of the bucket file.
        /// </summary>
        /// <param name="bucketFileContents">The contents of the bucket.json file.</param>
        public static string GetContentHash(string bucketFileContents)
        {
            var content = JsonFile.Parse(bucketFileContents);

            var relevantKeys = new string[]
            {
                "name", "version", "require", "require-dev", "conflict", "replace", "provide", "minimum-stability",
                "prefer-stable", "repositories", "extra",
            };

            Array.Sort(relevantKeys);

            var relevantContent = new JObject();
            void AddRelevantContent(string key)
            {
                if (content.ContainsKey(key))
                {
                    // JObject in the order of addition.
                    relevantContent.Add(key, content[key]);
                }
            }

            Array.ForEach(relevantKeys, AddRelevantContent);
            return Security.Md5(relevantContent.ToString());
        }

        /// <summary>
        /// Gets the bucket.lock configuration.
        /// </summary>
        public virtual ConfigLocker GetLockerData()
        {
            if (cache != null)
            {
                return cache;
            }

            if (!lockFile.Exists())
            {
                throw new RuntimeException("No lockfile found. Unable to read locked packages.");
            }

            return cache = lockFile.Read<ConfigLocker>();
        }

        /// <summary>
        /// Whether the locker file is exists.
        /// </summary>
        public virtual bool IsLocked()
        {
            if (!lockFile.Exists())
            {
                return false;
            }

            var locker = GetLockerData();

            // An element of 0 is also considered to be exists.
            return locker.Packages != null;
        }

        /// <summary>
        /// Whether the lock file is still up to date with the current hash.
        /// </summary>
        public virtual bool IsFresh()
        {
            // don't use GetLockerData() because it may use cached data.
            var locker = lockFile.Read<ConfigLocker>();

            if (!string.IsNullOrEmpty(locker.ContentHash))
            {
                return contentHash == locker.ContentHash;
            }

            return false;
        }

        /// <summary>
        /// Get locked repository it represents an array of locked packages.
        /// </summary>
        /// <param name="includeDev">Whether is include the dev package.</param>
        public virtual IRepository GetLockedRepository(bool includeDev = false)
        {
            var locker = GetLockerData();
            var repository = new RepositoryLock();

            var lockedPackages = new List<ConfigLockerPackage>(locker.Packages ?? Array.Empty<ConfigLockerPackage>());
            if (includeDev)
            {
                if (locker.PackagesDev == null)
                {
                    throw new RuntimeException("The lock file does not contain require-dev information, run install with the --no-dev option or run update to install those packages.");
                }

                lockedPackages.AddRange(locker.PackagesDev);
            }

            if (lockedPackages.Count <= 0)
            {
                return repository;
            }

            foreach (var package in lockedPackages)
            {
                repository.AddPackage(loader.Load(package));
            }

            return repository;
        }

        /// <summary>
        /// Gets the mininum stability.
        /// </summary>
        public virtual Stabilities GetMinimumStability()
        {
            return GetLockerData().MinimumStability;
        }

        /// <summary>
        /// Gets the stability flags relationship of the require(include dev) package.
        /// </summary>
        public virtual IDictionary<string, Stabilities> GetStabilityFlags()
        {
            return GetLockerData().StabilityFlags;
        }

        /// <summary>
        /// Whether is prefer stable.
        /// </summary>
        public virtual bool GetPreferStable()
        {
            return GetLockerData().PreferStable;
        }

        /// <summary>
        /// Whether is prefer lowest.
        /// </summary>
        public virtual bool GetPreferLowest()
        {
            return GetLockerData().PreferLowest;
        }

        /// <summary>
        /// Gets an array of package alias.
        /// </summary>
        public virtual ConfigAlias[] GetAliases()
        {
            return GetLockerData().Aliases ?? Array.Empty<ConfigAlias>();
        }

        /// <summary>
        /// Gets the manually configured platform package information.
        /// </summary>
        public virtual IDictionary<string, string> GetPlatforms()
        {
            return GetLockerData().Platforms;
        }

        /// <summary>
        /// Returns the platform requirements stored in the lock file.
        /// </summary>
        public virtual Link[] GetPlatformRequirements()
        {
            return loader.ParseLinks(ConfigBucketBase.RootPackage, "1.0.0", "requires", GetPlatforms());
        }

        /// <summary>
        /// Whether the locker are equal.
        /// </summary>
        internal static bool LockerAreEqual(ConfigLocker x, ConfigLocker y)
        {
            // todo: need optimization.
            return Security.Md5(JsonConvert.SerializeObject(x)) == Security.Md5(JsonConvert.SerializeObject(y));
        }

        /// <summary>
        /// Start setting a locker file data.
        /// </summary>
        /// <param name="packages">An array of packages.</param>
        internal virtual LockerBuilder BeginSetLockData(IPackage[] packages)
        {
            return new LockerBuilder(packages, SetLockData, installationManager, io);
        }

        /// <summary>
        /// Locks provided data into lockfile.
        /// </summary>
        /// <param name="locker">The lock data.</param>
        private bool SetLockData(ConfigLocker locker)
        {
            locker.ContentHash = contentHash;

            if (locker.Packages.Empty() && locker.PackagesDev.Empty())
            {
                lockFile.Delete();
                return false;
            }

            bool isLocked;
            try
            {
                isLocked = IsLocked();
            }
            catch (RuntimeException)
            {
                isLocked = false;
            }

            if (!isLocked || !LockerAreEqual(locker, GetLockerData()))
            {
                cache = null;
                lockFile.Write(locker);
                return true;
            }

            return false;
        }
    }
}

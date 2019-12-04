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

using Bucket.Exception;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.Package
{
    /// <summary>
    /// Base class for packages providing name storage and default match implementation.
    /// </summary>
    public abstract class BasePackage : IPackage
    {
        /// <summary>
        /// A string is represented as its own version.
        /// </summary>
        public const string SelfVersion = "self.version";

        private readonly string name;
        private readonly string namePretty;
        private IRepository repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePackage"/> class.
        /// </summary>
        /// <param name="name">The package name.</param>
        protected BasePackage(string name)
        {
            Id = -1;
            repository = null;
            namePretty = name;
            this.name = name.ToLower();
        }

        /// <inheritdoc />
        public virtual bool IsDev => GetStability() == Stabilities.Dev;

        /// <inheritdoc />
        public int Id { get; set; }

        /// <summary>
        /// Build a regex pattern from a package name, expanding * globs as required.
        /// </summary>
        /// <param name="packageNamePattern">The package name pattern.</param>
        /// <param name="wrap">Wrap the cleaned string by the given string.</param>
        public static string PackageNameToRegexPattern(string packageNamePattern, string wrap = "(?i:^{0}$)")
        {
            return string.Format(wrap, Str.AsteriskWildcard(packageNamePattern));
        }

        /// <inheritdoc />
        public virtual string GetName()
        {
            return name;
        }

        /// <inheritdoc />
        public virtual string GetNamePretty()
        {
            return namePretty;
        }

        /// <inheritdoc />
        public virtual string[] GetNames()
        {
            var names = new List<string>()
            {
                GetName(),
            };

            foreach (var link in GetProvides())
            {
                names.Add(link.GetTarget());
            }

            foreach (var link in GetReplaces())
            {
                names.Add(link.GetTarget());
            }

            return names.Distinct().ToArray();
        }

        /// <inheritdoc />
        public virtual string GetNameUnique()
        {
            return $"{GetName()}-{GetVersion()}";
        }

        /// <inheritdoc />
        public virtual string GetPrettyString()
        {
            return $"{GetNamePretty()}{Str.Space}{GetVersionPretty()}";
        }

        /// <inheritdoc />
        public virtual string GetVersionPrettyFull(bool truncate = true)
        {
            if (!IsDev || GetSourceType() != "git")
            {
                return GetVersionPretty();
            }

            // if source reference is a sha1 hash -- truncate
            var reference = GetSourceReference();
            if (truncate && reference.Length == 40)
            {
                return GetVersionPretty() + Str.Space + reference.Substring(0, 7);
            }

            return GetVersionPretty() + Str.Space + reference;
        }

        /// <inheritdoc />
        public virtual void SetRepository(IRepository repository)
        {
            if (this.repository != null && this.repository != repository)
            {
                throw new UnexpectedException("A package can only be added to one repository.");
            }

            this.repository = repository;
        }

        /// <inheritdoc />
        public virtual IRepository GetRepository()
        {
            return repository;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is IPackage package))
            {
                return false;
            }

            return Equals(package);
        }

        /// <inheritdoc />
        public virtual bool Equals(IPackage other)
        {
            IPackage self = this;
            if (self is PackageAlias packageAlias)
            {
                self = packageAlias.GetAliasOf();
            }

            if (other is PackageAlias objPackageAlias)
            {
                other = objPackageAlias.GetAliasOf();
            }

            return ReferenceEquals(self, other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            IPackage self = this;
            if (self is PackageAlias packageAlias)
            {
                self = packageAlias.GetAliasOf();
            }

            return self.ToString().GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetNameUnique();
        }

        /// <inheritdoc />
        public virtual object Clone()
        {
            var clone = (BasePackage)MemberwiseClone();
            clone.repository = null;
            clone.Id = -1;
            return clone;
        }

        /// <inheritdoc />
        public abstract Stabilities GetStability();

        /// <inheritdoc />
        public abstract string GetSourceType();

        /// <inheritdoc />
        public abstract void SetSourceType(string sourceType);

        /// <inheritdoc />
        public abstract string GetSourceReference();

        /// <inheritdoc />
        public abstract void SetSourceReference(string sourceReference);

        /// <inheritdoc />
        public abstract string GetVersion();

        /// <inheritdoc />
        public abstract string GetVersionPretty();

        /// <inheritdoc />
        public abstract Link[] GetReplaces();

        /// <inheritdoc />
        public abstract Link[] GetProvides();

        /// <inheritdoc />
        public abstract string[] GetArchives();

        /// <inheritdoc />
        public abstract dynamic GetExtra();

        /// <inheritdoc />
        public abstract string[] GetBinaries();

        /// <inheritdoc />
        public abstract Link[] GetConflicts();

        /// <inheritdoc />
        public abstract Link[] GetRequires();

        /// <inheritdoc />
        public abstract Link[] GetRequiresDev();

        /// <inheritdoc />
        public abstract string GetDistShasum();

        /// <inheritdoc />
        public abstract void SetDistShasum(string distShasum);

        /// <inheritdoc />
        public abstract string GetDistReference();

        /// <inheritdoc />
        public abstract void SetDistReference(string distReference);

        /// <inheritdoc />
        public abstract string GetDistType();

        /// <inheritdoc />
        public abstract void SetDistType(string distType);

        /// <inheritdoc />
        public abstract string GetDistUri();

        /// <inheritdoc />
        public abstract void SetDistUri(string distUri);

        /// <inheritdoc />
        public abstract string[] GetDistUris();

        /// <inheritdoc />
        public abstract string[] GetDistMirrors();

        /// <inheritdoc />
        public abstract void SetDistMirrors(string[] mirrors);

        /// <inheritdoc />
        public abstract InstallationSource? GetInstallationSource();

        /// <inheritdoc />
        public abstract string GetNotificationUri();

        /// <inheritdoc />
        public abstract DateTime? GetReleaseDate();

        /// <inheritdoc />
        public abstract string GetSourceUri();

        /// <inheritdoc />
        public abstract void SetSourceUri(string sourceUri);

        /// <inheritdoc />
        public abstract string[] GetSourceUris();

        /// <inheritdoc />
        public abstract string[] GetSourceMirrors();

        /// <inheritdoc />
        public abstract void SetSourceMirrors(string[] mirrors);

        /// <inheritdoc />
        public abstract IDictionary<string, string> GetSuggests();

        /// <inheritdoc />
        public abstract void SetInstallationSource(InstallationSource? source);

        /// <inheritdoc />
        public abstract string GetPackageType();
    }
}

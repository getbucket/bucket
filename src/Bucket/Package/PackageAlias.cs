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

using Bucket.Repository;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Bucket.Util;
using System;
using System.Collections.Generic;

namespace Bucket.Package
{
    /// <summary>
    /// Indicates that the current package is an alias package.
    /// </summary>
    public class PackageAlias : BasePackage
    {
        private readonly IPackage aliasOf;
        private readonly string version;
        private readonly string versionPretty;
        private readonly Stabilities stability;
        private Link[] requires;
        private Link[] requiresDev;
        private Link[] conflicts;
        private Link[] provides;
        private Link[] replaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageAlias"/> class.
        /// </summary>
        /// <param name="aliasOf">The package this package is an alias of.</param>
        /// <param name="version">The version the alias must report.</param>
        /// <param name="versionPretty">The alias's non-normalized version.</param>
        public PackageAlias(IPackage aliasOf, string version, string versionPretty)
            : base(aliasOf.GetName())
        {
            this.aliasOf = aliasOf;
            this.version = version;
            this.versionPretty = versionPretty;
            stability = VersionParser.ParseStability(version);

            requires = ReplaceSelfVersion(aliasOf.GetRequires(), nameof(requires), true);
            requiresDev = ReplaceSelfVersion(aliasOf.GetRequiresDev(), nameof(requiresDev), true);
            conflicts = ReplaceSelfVersion(aliasOf.GetConflicts(), nameof(conflicts));
            provides = ReplaceSelfVersion(aliasOf.GetProvides(), nameof(provides));
            replaces = ReplaceSelfVersion(aliasOf.GetReplaces(), nameof(replaces));
        }

        /// <summary>
        /// Gets a value indicating whether this is an alias created by an aliasing in the requirements of the root package.
        /// </summary>
        public bool IsRootPackageAlias { get; private set; }

        /// <summary>
        /// Gets the package is an alias of.
        /// </summary>
        /// <returns>Returns the package is an alias of.</returns>
        public IPackage GetAliasOf()
        {
            return aliasOf;
        }

        /// <summary>
        /// Sets whether this is an alias created by an aliasing in the requirements of the root package or not.
        /// </summary>
        /// <param name="isRootPackageAlias">True if this is an alias created by root package.</param>
        public void SetRootPackageAlias(bool isRootPackageAlias = true)
        {
            IsRootPackageAlias = isRootPackageAlias;
        }

        /// <inheritdoc />
        public override Stabilities GetStability()
        {
            return stability;
        }

        /// <inheritdoc />
        public override string GetVersion()
        {
            return version;
        }

        /// <inheritdoc />
        public override string GetVersionPretty()
        {
            return versionPretty;
        }

        /// <inheritdoc />
        public override Link[] GetRequires()
        {
            return requires;
        }

        /// <inheritdoc />
        public override Link[] GetRequiresDev()
        {
            return requiresDev;
        }

        /// <inheritdoc />
        public override Link[] GetProvides()
        {
            return provides;
        }

        /// <inheritdoc />
        public override Link[] GetConflicts()
        {
            return conflicts;
        }

        /// <inheritdoc />
        public override Link[] GetReplaces()
        {
            return replaces;
        }

        /// <inheritdoc />
        public override string[] GetArchives()
        {
            return GetAliasOf().GetArchives();
        }

        /// <inheritdoc />
        public override dynamic GetExtra()
        {
            return GetAliasOf().GetExtra();
        }

        /// <inheritdoc />
        public override string[] GetBinaries()
        {
            return GetAliasOf().GetBinaries();
        }

        /// <inheritdoc />
        public override string GetDistShasum()
        {
            return GetAliasOf().GetDistShasum();
        }

        /// <inheritdoc />
        public override void SetDistShasum(string distShasum)
        {
            GetAliasOf().SetDistShasum(distShasum);
        }

        /// <inheritdoc />
        public override string GetDistReference()
        {
            return GetAliasOf().GetDistReference();
        }

        /// <inheritdoc />
        public override void SetDistReference(string distReference)
        {
            GetAliasOf().SetDistReference(distReference);
        }

        /// <inheritdoc />
        public override string GetDistType()
        {
            return GetAliasOf().GetDistType();
        }

        /// <inheritdoc />
        public override void SetDistType(string distType)
        {
            GetAliasOf().SetDistType(distType);
        }

        /// <inheritdoc />
        public override string GetDistUri()
        {
            return GetAliasOf().GetDistUri();
        }

        /// <inheritdoc />
        public override void SetDistUri(string distUri)
        {
            GetAliasOf().SetDistUri(distUri);
        }

        /// <inheritdoc />
        public override string[] GetDistUris()
        {
            return GetAliasOf().GetDistUris();
        }

        /// <inheritdoc />
        public override string[] GetDistMirrors()
        {
            return GetAliasOf().GetDistMirrors();
        }

        /// <inheritdoc />
        public override void SetDistMirrors(string[] mirrors)
        {
            GetAliasOf().SetDistMirrors(mirrors);
        }

        /// <inheritdoc />
        public override InstallationSource? GetInstallationSource()
        {
            return GetAliasOf().GetInstallationSource();
        }

        /// <inheritdoc />
        public override string GetNotificationUri()
        {
            return GetAliasOf().GetNotificationUri();
        }

        /// <inheritdoc />
        public override DateTime? GetReleaseDate()
        {
            return GetAliasOf().GetReleaseDate();
        }

        /// <inheritdoc />
        public override IRepository GetRepository()
        {
            return GetAliasOf().GetRepository();
        }

        /// <inheritdoc />
        public override string GetSourceReference()
        {
            return GetAliasOf().GetSourceReference();
        }

        /// <inheritdoc />
        public override void SetSourceReference(string sourceReference)
        {
            GetAliasOf().SetSourceReference(sourceReference);
        }

        /// <inheritdoc />
        public override string GetSourceType()
        {
            return GetAliasOf().GetSourceType();
        }

        /// <inheritdoc />
        public override void SetSourceType(string sourceType)
        {
            GetAliasOf().SetSourceType(sourceType);
        }

        /// <inheritdoc />
        public override string GetSourceUri()
        {
            return GetAliasOf().GetSourceUri();
        }

        /// <inheritdoc />
        public override void SetSourceUri(string sourceUri)
        {
            GetAliasOf().SetSourceUri(sourceUri);
        }

        /// <inheritdoc />
        public override string[] GetSourceUris()
        {
            return GetAliasOf().GetSourceUris();
        }

        /// <inheritdoc />
        public override string[] GetSourceMirrors()
        {
            return GetAliasOf().GetSourceMirrors();
        }

        /// <inheritdoc />
        public override void SetSourceMirrors(string[] mirrors)
        {
            GetAliasOf().SetSourceMirrors(mirrors);
        }

        /// <inheritdoc />
        public override IDictionary<string, string> GetSuggests()
        {
            return GetAliasOf().GetSuggests();
        }

        /// <inheritdoc />
        public override string GetPackageType()
        {
            return GetAliasOf().GetPackageType();
        }

        /// <inheritdoc />
        public override void SetInstallationSource(InstallationSource? source)
        {
            GetAliasOf().SetInstallationSource(source);
        }

        /// <summary>
        /// Set the conflicting packages.
        /// </summary>
        /// <param name="conflicts">An array of package links.</param>
        public virtual void SetConflicts(Link[] conflicts)
        {
            this.conflicts = conflicts;
        }

        /// <summary>
        /// Set the provided virtual packages.
        /// </summary>
        /// <param name="provides">An array of package links.</param>
        public virtual void SetProvides(Link[] provides)
        {
            this.provides = provides;
        }

        /// <summary>
        /// Set the packages this one replaces.
        /// </summary>
        /// <param name="replaces">An array of package links.</param>
        public virtual void SetReplaces(Link[] replaces)
        {
            this.replaces = replaces;
        }

        /// <summary>
        /// Set the require packages.
        /// </summary>
        /// <param name="requires">An array of package links.</param>
        public virtual void SetRequires(Link[] requires)
        {
            this.requires = requires;
        }

        /// <summary>
        /// Set the require packages with developer mode.
        /// </summary>
        /// <param name="requiresDev">An array of package links.</param>
        public virtual void SetRequiresDev(Link[] requiresDev)
        {
            this.requiresDev = requiresDev;
        }

        /// <summary>
        /// Gets the package is an alias of.
        /// </summary>
        /// <typeparam name="T">The type of alias of.</typeparam>
        /// <returns>Returns the package is an alias of.</returns>
        protected T GetAliasOf<T>()
            where T : IPackage
        {
            return (T)aliasOf;
        }

        /// <summary>
        /// Replace this.version flags from pretty constraint.
        /// </summary>
        /// <param name="links">An array of links.</param>
        /// <param name="description">The description with new link.</param>
        /// <param name="isRequiresField">Whether is requires field.</param>
        /// <returns>Returns an array of new links.</returns>
        protected Link[] ReplaceSelfVersion(Link[] links, string description, bool isRequiresField = false)
        {
            if (isRequiresField)
            {
                for (var i = 0; i < links.Length; i++)
                {
                    if (links[i].GetPrettyConstraint() == SelfVersion)
                    {
                        links[i] = new Link(links[i].GetSource(), links[i].GetTarget(), new Constraint("=", GetVersion()), description, GetVersionPretty());
                    }
                }

                return links;
            }

            var newLinks = new List<Link>();
            foreach (var link in links)
            {
                if (link.GetPrettyConstraint() == SelfVersion)
                {
                    newLinks.Add(new Link(link.GetSource(), link.GetTarget(), new Constraint("=", GetVersion()), description, GetVersionPretty()));
                }
            }

            return Arr.Merge(links, newLinks.ToArray());
        }
    }
}

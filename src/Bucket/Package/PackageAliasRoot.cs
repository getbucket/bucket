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
using Bucket.Semver;
using System.Collections.Generic;

namespace Bucket.Package
{
    /// <summary>
    /// An alias package representing the root.
    /// </summary>
    public class PackageAliasRoot : PackageAliasComplete, IPackageRoot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageAliasRoot"/> class.
        /// </summary>
        /// <param name="aliasOf">The package this package is an alias of.</param>
        /// <param name="version">The version the alias must report.</param>
        /// <param name="versionPretty">The alias's non-normalized version.</param>
        public PackageAliasRoot(IPackageRoot aliasOf, string version, string versionPretty)
            : base(aliasOf, version, versionPretty)
        {
        }

        /// <inheritdoc />
        public bool? IsPreferStable => GetAliasOf<IPackageRoot>().IsPreferStable;

        /// <inheritdoc />
        public ConfigAlias[] GetAliases()
        {
            return GetAliasOf<IPackageRoot>().GetAliases();
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetReferences()
        {
            return GetAliasOf<IPackageRoot>().GetReferences();
        }

        /// <inheritdoc />
        public IDictionary<string, Stabilities> GetStabilityFlags()
        {
            return GetAliasOf<IPackageRoot>().GetStabilityFlags();
        }

        /// <inheritdoc />
        public Stabilities? GetMinimumStability()
        {
            return GetAliasOf<IPackageRoot>().GetMinimumStability();
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetPlatforms()
        {
            return GetAliasOf<IPackageRoot>().GetPlatforms();
        }

        /// <inheritdoc />
        public void SetRepositories(ConfigRepository[] repositories)
        {
            GetAliasOf<IPackageRoot>().SetRepositories(repositories);
        }

        /// <inheritdoc />
        public void SetSuggests(IDictionary<string, string> suggests)
        {
            GetAliasOf<IPackageRoot>().SetSuggests(suggests);
        }

        /// <inheritdoc />
        public override void SetConflicts(Link[] conflicts)
        {
            conflicts = ReplaceSelfVersion(conflicts, "conflicts", false);
            base.SetConflicts(conflicts);
            GetAliasOf<IPackageRoot>().SetConflicts(conflicts);
        }

        /// <inheritdoc />
        public override void SetRequires(Link[] requires)
        {
            requires = ReplaceSelfVersion(requires, "requires", false);
            base.SetRequires(requires);
            GetAliasOf<IPackageRoot>().SetRequires(requires);
        }

        /// <inheritdoc />
        public override void SetRequiresDev(Link[] requiresDev)
        {
            requiresDev = ReplaceSelfVersion(requiresDev, "requiresDev", false);
            base.SetRequiresDev(requiresDev);
            GetAliasOf<IPackageRoot>().SetRequiresDev(requiresDev);
        }

        /// <inheritdoc />
        public override void SetReplaces(Link[] replaces)
        {
            replaces = ReplaceSelfVersion(replaces, "replaces", false);
            base.SetReplaces(replaces);
            GetAliasOf<IPackageRoot>().SetReplaces(replaces);
        }

        /// <inheritdoc />
        public override void SetProvides(Link[] provides)
        {
            provides = ReplaceSelfVersion(provides, "provides", false);
            base.SetProvides(provides);
            GetAliasOf<IPackageRoot>().SetProvides(provides);
        }
    }
}

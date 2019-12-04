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
using System.Collections.Generic;

namespace Bucket.Package
{
    /// <summary>
    /// Indicates that the current package is an alias package
    /// and additional metadata that is not used by the solver.
    /// </summary>
    public class PackageAliasComplete : PackageAlias, IPackageComplete
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageAliasComplete"/> class.
        /// </summary>
        /// <param name="aliasOf">The package this package is an alias of.</param>
        /// <param name="version">The version the alias must report.</param>
        /// <param name="versionPretty">The alias's non-normalized version.</param>
        public PackageAliasComplete(IPackageComplete aliasOf, string version, string versionPretty)
            : base(aliasOf, version, versionPretty)
        {
        }

        /// <inheritdoc />
        public bool IsDeprecated => GetAliasOf<IPackageComplete>().IsDeprecated;

        /// <inheritdoc />
        public ConfigAuthor[] GetAuthors()
        {
            return GetAliasOf<IPackageComplete>().GetAuthors();
        }

        /// <inheritdoc />
        public string GetDescription()
        {
            return GetAliasOf<IPackageComplete>().GetDescription();
        }

        /// <inheritdoc />
        public string GetHomepage()
        {
            return GetAliasOf<IPackageComplete>().GetHomepage();
        }

        /// <inheritdoc />
        public string[] GetKeywords()
        {
            return GetAliasOf<IPackageComplete>().GetKeywords();
        }

        /// <inheritdoc />
        public string[] GetLicenses()
        {
            return GetAliasOf<IPackageComplete>().GetLicenses();
        }

        /// <inheritdoc />
        public string GetReplacementPackage()
        {
            return GetAliasOf<IPackageComplete>().GetReplacementPackage();
        }

        /// <inheritdoc />
        public ConfigRepository[] GetRepositories()
        {
            return GetAliasOf<IPackageComplete>().GetRepositories();
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetScripts()
        {
            return GetAliasOf<IPackageComplete>().GetScripts();
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetSupport()
        {
            return GetAliasOf<IPackageComplete>().GetSupport();
        }
    }
}

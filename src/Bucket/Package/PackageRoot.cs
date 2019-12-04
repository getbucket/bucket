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
using System;
using System.Collections.Generic;

namespace Bucket.Package
{
    /// <summary>
    /// The root package represents the project's bucket.json and contains additional metadata.
    /// </summary>
    public class PackageRoot : PackageComplete, IPackageRoot
    {
        private ConfigAlias[] aliases;
        private IDictionary<string, string> references;
        private IDictionary<string, Stabilities> stabilityFlags;
        private IDictionary<string, string> platforms;
        private Stabilities? minimumStability;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRoot"/> class.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="version">Normalized version.</param>
        /// <param name="versionPretty">The package non-normalized version(Human readable).</param>
        public PackageRoot(string name, string version, string versionPretty)
            : base(name, version, versionPretty)
        {
        }

        /// <inheritdoc />
        public bool? IsPreferStable { get; private set; }

        /// <inheritdoc />
        public ConfigAlias[] GetAliases()
        {
            return aliases ?? Array.Empty<ConfigAlias>();
        }

        /// <summary>
        /// Sets an array of package names and their aliases.
        /// </summary>
        /// <param name="aliases">An array of package aliases.</param>
        public void SetAliases(ConfigAlias[] aliases)
        {
            this.aliases = aliases;
        }

        /// <summary>
        /// Sets the reference relationship of the require(include dev) package.
        /// </summary>
        public void SetReferences(IDictionary<string, string> references)
        {
            this.references = references;
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetReferences()
        {
            return references;
        }

        /// <summary>
        /// Sets the stability flags relationship of the require(include dev) package.
        /// </summary>
        public void SetStabilityFlags(IDictionary<string, Stabilities> stabilityFlags)
        {
            this.stabilityFlags = stabilityFlags;
        }

        /// <inheritdoc />
        public IDictionary<string, Stabilities> GetStabilityFlags()
        {
            return stabilityFlags;
        }

        /// <inheritdoc />
        public Stabilities? GetMinimumStability()
        {
            return minimumStability;
        }

        /// <summary>
        /// Sets the minimum stability of the package.
        /// </summary>
        /// <param name="minimumStability">The minimum stability of the package.</param>
        public void SetMinimunStability(Stabilities? minimumStability)
        {
            this.minimumStability = minimumStability;
        }

        /// <summary>
        /// Sets whether is prefer stable.
        /// </summary>
        /// <param name="preferStable">True if prefer stable.</param>
        public void SetPreferStable(bool? preferStable)
        {
            IsPreferStable = preferStable;
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetPlatforms()
        {
            return platforms;
        }

        /// <summary>
        /// Sets the manually configured platform package information.
        /// </summary>
        public void SetPlatforms(IDictionary<string, string> platforms)
        {
            this.platforms = platforms;
        }
    }
}

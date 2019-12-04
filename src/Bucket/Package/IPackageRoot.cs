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
    /// Defines additional fields that are only needed for the root package.
    /// </summary>
    public interface IPackageRoot : IPackageComplete
    {
        /// <summary>
        /// Gets a value indicating whether is prefer stable.
        /// </summary>
        bool? IsPreferStable { get; }

        /// <summary>
        /// Gets an array of package names and their aliases.
        /// </summary>
        /// <returns>Returns an array of package names and their aliases.</returns>
        ConfigAlias[] GetAliases();

        /// <summary>
        /// Gets the reference relationship of the require(include dev) package.
        /// </summary>
        /// <remarks>The dictionary key is package lowername.</remarks>
        IDictionary<string, string> GetReferences();

        /// <summary>
        /// Gets the stability flags relationship of the require(include dev) package.
        /// </summary>
        /// <remarks>The dictionary key is package lowername.</remarks>
        IDictionary<string, Stabilities> GetStabilityFlags();

        /// <summary>
        /// Gets the minimum stability of the package.
        /// </summary>
        /// <returns>Returns the minimum stability of the package.</returns>
        Stabilities? GetMinimumStability();

        /// <summary>
        /// Gets the manually configured platform package information.
        /// </summary>
        IDictionary<string, string> GetPlatforms();

        /// <summary>
        /// Sets the requires packages.
        /// </summary>
        /// <param name="requires">An array of the requires package link.</param>
        void SetRequires(Link[] requires);

        /// <summary>
        /// Sets the requires packages with dev mode.
        /// </summary>
        /// <param name="requires">An array of the requires package links.</param>
        void SetRequiresDev(Link[] requires);

        /// <summary>
        /// Sets the conflict packages.
        /// </summary>
        /// <param name="conflicts">An array of the conflict package links.</param>
        void SetConflicts(Link[] conflicts);

        /// <summary>
        /// Sets the replace packages.
        /// </summary>
        /// <param name="replaces">An array of the replace package links.</param>
        void SetReplaces(Link[] replaces);

        /// <summary>
        /// Sets the provides package.
        /// </summary>
        /// <param name="provides">An array of the provides package links.</param>
        void SetProvides(Link[] provides);

        /// <summary>
        /// Sets the suggested packages.
        /// </summary>
        /// <param name="suggests">An array of the suggested packages.</param>
        void SetSuggests(IDictionary<string, string> suggests);

        /// <summary>
        /// Sets an array of repository configurations.
        /// </summary>
        /// <param name="repositories">An array of repository configurations.</param>
        void SetRepositories(ConfigRepository[] repositories);
    }
}

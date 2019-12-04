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

using Bucket.Package;
using Bucket.Semver;
using BVersionParser = Bucket.Package.Version.VersionParser;

namespace Bucket.Repository
{
    /// <summary>
    /// <see cref="IRepository"/>'s extension function.
    /// </summary>
    public static class ExtensionRepository
    {
        private static IVersionParser versionParser = new BVersionParser();

        /// <summary>
        /// Searches for the first match of a package by name and version.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <param name="name">The package name.</param>
        /// <param name="version">The package version to match against.</param>
        /// <returns>Returns the first match of a package.</returns>
        public static IPackage FindPackage(this IRepository repository, string name, string version)
        {
            var constraint = versionParser.ParseConstraints(version);
            return repository.FindPackage(name, constraint);
        }

        /// <summary>
        /// Searches for all packages matching a name and optionally a version.
        /// </summary>
        /// <param name="repository">The repository instance.</param>
        /// <param name="name">The package name.</param>
        /// <param name="version">The package version to match against.</param>
        /// <returns>Returns an array represents all packages matching.</returns>
        public static IPackage[] FindPackages(this IRepository repository, string name, string version)
        {
            var constraint = versionParser.ParseConstraints(version);
            return repository.FindPackages(name, constraint);
        }
    }
}

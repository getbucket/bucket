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
using Bucket.Semver.Constraint;

namespace Bucket.Repository
{
    /// <summary>
    /// <see cref="IRepository"/> is the interface implemented by all Repository classes.
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Gets the number of packages in the repository.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Checks if specified package is in the repository.
        /// </summary>
        /// <param name="package">The specified package.</param>
        /// <returns>True if the specified in the repository.</returns>
        bool HasPackage(IPackage package);

        /// <summary>
        /// Searches for the first match of a package by name and version.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="constraint">The package version constraint to match against.</param>
        /// <returns>Returns the first match of a package.</returns>
        IPackage FindPackage(string name, IConstraint constraint);

        /// <summary>
        /// Searches for all packages matching a name and optionally a version.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="constraint">The package version or version constraint to match against.</param>
        /// <returns>Returns an array represents all packages matching.</returns>
        IPackage[] FindPackages(string name, IConstraint constraint = null);

        /// <summary>
        /// Return all packages in the repository.
        /// </summary>
        /// <returns>Returns an array of all package.</returns>
        IPackage[] GetPackages();

        /// <summary>
        /// Searches the repository for packages containing the query.
        /// </summary>
        /// <param name="query">The search query. Allow incoming a search pattern.</param>
        /// <param name="mode">How to search on, implementations should do a best effort only.</param>
        /// <param name="type">The type of package to search for. Defaults to all types of packages.</param>
        /// <returns>An array of the searched packages.</returns>
        SearchResult[] Search(string query, SearchMode mode = SearchMode.Fulltext, string type = null);
    }
}

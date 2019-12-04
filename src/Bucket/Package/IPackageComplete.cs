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
    /// Defines package metadata that is not necessarily needed for solving and installing packages.
    /// </summary>
    public interface IPackageComplete : IPackage
    {
        /// <summary>
        /// Gets a value indicating whether if the package is deprecated or not.
        /// </summary>
        bool IsDeprecated { get; }

        /// <summary>
        /// Gets an array of repository configurations.
        /// </summary>
        /// <returns>Returns an array of repository configurations.</returns>
        ConfigRepository[] GetRepositories();

        /// <summary>
        /// Gets the package homepage.
        /// </summary>
        /// <returns>Returns the package homepage.</returns>
        string GetHomepage();

        /// <summary>
        /// Gets the package license. e.g. MIT, BSD, GPL.
        /// </summary>
        /// <returns>Returns the package license.</returns>
        string[] GetLicenses();

        /// <summary>
        /// Gets the package description.
        /// </summary>
        /// <returns>Returns the package description.</returns>
        string GetDescription();

        /// <summary>
        /// Gets an array of keywords relating to the package.
        /// </summary>
        /// <returns>Returns an array of keywords relating to the package.</returns>
        string[] GetKeywords();

        /// <summary>
        /// Gets an array of the package authors.
        /// </summary>
        /// <returns>Returns the package authors.</returns>
        ConfigAuthor[] GetAuthors();

        /// <summary>
        /// Gets an map of the package support information.
        /// </summary>
        IDictionary<string, string> GetSupport();

        /// <summary>
        /// Gets an map of the scripts.
        /// </summary>
        IDictionary<string, string> GetScripts();

        /// <summary>
        /// If the package is deprecated and has a suggested replacement, this method returns it.
        /// </summary>
        string GetReplacementPackage();
    }
}

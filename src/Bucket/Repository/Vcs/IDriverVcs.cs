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

#pragma warning disable CA1040

using Bucket.Configuration;
using System.Collections.Generic;

namespace Bucket.Repository.Vcs
{
    /// <summary>
    /// Represents a Vcs driver.
    /// </summary>
    public interface IDriverVcs
    {
        /// <summary>
        /// Initializes the driver (git clone, svn checkout, fetch info etc).
        /// </summary>
        void Initialize();

        /// <summary>
        /// Return true if the repository has a bucket file for a given identifier,
        /// false otherwise.
        /// </summary>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <returns>Whether the repository has a bucket file for a given identifier.</returns>
        bool HasBucketFile(string identifier);

        /// <summary>
        /// Gets the root identifier (trunk, master ..)
        /// </summary>
        /// <returns>Returns the root identifier.</returns>
        string GetRootIdentifier();

        /// <summary>
        /// Gets the bucket.json information.
        /// </summary>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <returns>Returns object Containing all infos from the bucket.json file.</returns>
        ConfigBucket GetBucketInformation(string identifier);

        /// <summary>
        /// Gets the dist resource.
        /// </summary>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <returns>Returns an resource instance.</returns>
        ConfigResource GetDist(string identifier);

        /// <summary>
        /// Gets the source resource.
        /// </summary>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <returns>Returns an resource instance.</returns>
        ConfigResource GetSource(string identifier);

        /// <summary>
        /// Gets an array of tags in the repository.
        /// </summary>
        /// <remarks>Key is tags, Value is identifier.</remarks>
        /// <returns>Returns an array of tags in the repository.</returns>
        IReadOnlyDictionary<string, string> GetTags();

        /// <summary>
        /// Gets an array of branchs in the repository.
        /// </summary>
        /// <remarks>Key is branch, Value is identifier.</remarks>
        /// <returns>Returns an array of branchs in the repository.</returns>
        IReadOnlyDictionary<string, string> GetBranches();

        /// <summary>
        /// Cleanup the driver.
        /// </summary>
        void Cleanup();
    }
}

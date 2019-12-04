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
using System.Collections.Generic;

namespace Bucket.DependencyResolver.Policy
{
    /// <summary>
    /// Represents a version select policy.
    /// </summary>
    public interface IPolicy
    {
        /// <summary>
        /// Compliance with operating conditions.
        /// </summary>
        /// <param name="left">The left vaule.</param>
        /// <param name="operatorString">The operator string.</param>
        /// <param name="right">The right value.</param>
        /// <returns>True if compliance with operating conditions.</returns>
        bool VersionCompare(IPackage left, string operatorString, IPackage right);

        /// <summary>
        /// Find packages that the specified package can be updated package(Means equivalent package).
        /// </summary>
        /// <param name="pool">A package pool.</param>
        /// <param name="installedMap">Installed package mapping table.</param>
        /// <param name="package">The specified package instance.</param>
        /// <returns>Returns an array of package can be updated.</returns>
        IPackage[] FindUpdatePackages(Pool pool, IDictionary<int, IPackage> installedMap, IPackage package);

        /// <summary>
        /// Select from a given literals that the most suitable package.
        /// </summary>
        /// <param name="pool">A package pool.</param>
        /// <param name="installedMap">Installed the package map.</param>
        /// <param name="literals">Available packages to be selected.(Use literals to represent).</param>
        /// <param name="requirePackageName">Indicates the name of the require package. (This will make the function a more appropriate sort).</param>
        /// <returns>Returns an array of literals representing the most appropriate requires.</returns>
        int[] SelectPreferredPackages(Pool pool, IDictionary<int, IPackage> installedMap, int[] literals, string requirePackageName = null);
    }
}

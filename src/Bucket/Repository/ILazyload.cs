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
using System.Collections.Generic;

namespace Bucket.Repository
{
    /// <summary>
    /// Assert whether the package is available.
    /// </summary>
    /// <param name="stability">The package stability.</param>
    /// <param name="name">The package name.</param>
    /// <returns>True if the package is acceptable.</returns>
#pragma warning disable SA1402
    public delegate bool PredicatePackageAcceptable(Stabilities stability, params string[] name);
#pragma warning restore SA1402

    /// <summary>
    /// Indicates that the current repository can be lazily loaded.
    /// </summary>
    public interface ILazyload
    {
        /// <summary>
        /// Gets a value indicating whether is lazy load.
        /// </summary>
        bool IsLazyLoad { get; }

        /// <summary>
        /// Gets a list of the providers name.
        /// </summary>
        /// <remarks>The return value must not be null.</remarks>
        ICollection<string> GetProviderNames();

        /// <summary>
        /// Get the package that matches the name.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="predicatePackageAcceptable">An predicate callback Indicates if the package is available.</param>
        IPackage[] WhatProvides(string name, PredicatePackageAcceptable predicatePackageAcceptable = null);
    }
}

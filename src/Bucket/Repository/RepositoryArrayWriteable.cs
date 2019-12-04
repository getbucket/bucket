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

using Bucket.Exception;
using Bucket.Package;
using Bucket.Util;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.Repository
{
    /// <summary>
    /// An stores packages in an array it can be writeable.
    /// </summary>
    public class RepositoryArrayWriteable : RepositoryArray, IRepositoryWriteable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryArrayWriteable"/> class.
        /// </summary>
        /// <param name="packages">Initializes package array.</param>
        public RepositoryArrayWriteable(IEnumerable<IPackage> packages = null)
            : base(packages)
        {
        }

        /// <inheritdoc />
        public virtual IPackage[] GetCanonicalPackages()
        {
            var packages = GetPackages();

            // get at most one package of each name(first one), preferring non-aliased ones.
            var packagesByName = new Dictionary<string, IPackage>();
            foreach (var package in packages)
            {
                if (!packagesByName.TryGetValue(package.GetName(), out IPackage oldPackage) || oldPackage is PackageAlias)
                {
                    packagesByName[package.GetName()] = package;
                }
            }

            var canonicalPackages = new LinkedList<IPackage>();
            foreach (var item in packagesByName)
            {
                var candidate = item.Value;
                while (candidate is PackageAlias packageAlias)
                {
                    candidate = packageAlias.GetAliasOf();
                }

                Guard.Requires<UnexpectedException>(candidate != null, $"Candidate package should not be null: {item.Key}({item.Value})");
                canonicalPackages.AddLast(candidate);
            }

            return canonicalPackages.ToArray();
        }

        /// <inheritdoc />
        public virtual void Reload()
        {
            // no code.
        }

        /// <inheritdoc />
        public virtual void Write()
        {
            // no code.
        }
    }
}

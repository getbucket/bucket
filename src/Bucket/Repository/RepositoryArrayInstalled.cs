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

namespace Bucket.Repository
{
    /// <summary>
    /// A repository that represents the packages that have been installed.
    /// </summary>
    public class RepositoryArrayInstalled : RepositoryArrayWriteable, IRepositoryInstalled
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryArrayInstalled"/> class.
        /// </summary>
        /// <param name="packages">Initializes package array.</param>
        public RepositoryArrayInstalled(IEnumerable<IPackage> packages = null)
            : base(packages)
        {
        }
    }
}

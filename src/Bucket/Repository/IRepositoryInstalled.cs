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

namespace Bucket.Repository
{
    /// <summary>
    /// The repository represents all installed packages.
    /// </summary>
    public interface IRepositoryInstalled : IRepositoryWriteable
    {
    }
}

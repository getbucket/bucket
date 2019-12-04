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

using Bucket.DependencyResolver.Rules;

namespace Bucket.DependencyResolver.Operation
{
    /// <summary>
    /// Solver operation interface.
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Gets job command.
        /// </summary>
        JobCommand JobCommand { get; }

        /// <summary>
        /// Gets the reason why do this operation.
        /// </summary>
        /// <returns>Returns the reason why do this operation.</returns>
        Rule GetReason();
    }
}

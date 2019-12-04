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
using Bucket.Package;

namespace Bucket.DependencyResolver.Operation
{
    internal abstract class BaseOperation : IOperation
    {
        private readonly Rule reason;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseOperation"/> class.
        /// </summary>
        /// <param name="reason">The reason of the operation.</param>
        protected BaseOperation(Rule reason)
        {
            this.reason = reason;
        }

        /// <inheritdoc />
        public abstract JobCommand JobCommand { get; }

        /// <summary>
        /// Get the main package of operations.
        /// </summary>
        /// <returns>The main package instance.</returns>
        public abstract IPackage GetPackage();

        /// <inheritdoc />
        public Rule GetReason()
        {
            return reason;
        }
    }
}

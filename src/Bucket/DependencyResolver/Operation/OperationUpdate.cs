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
    /// <summary>
    /// Solver update operation.
    /// </summary>
    internal sealed class OperationUpdate : BaseOperation
    {
        private readonly IPackage initial;
        private readonly IPackage target;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationUpdate"/> class.
        /// </summary>
        /// <param name="initial">The initial package instance.</param>
        /// <param name="target">The target update package instance.</param>
        /// <param name="reason">Why update the package.</param>
        public OperationUpdate(IPackage initial, IPackage target, Rule reason = null)
            : base(reason)
        {
            this.initial = initial;
            this.target = target;
        }

        /// <inheritdoc />
        public override JobCommand JobCommand => JobCommand.Update;

        public IPackage GetInitialPackage()
        {
            return initial;
        }

        /// <inheritdoc />
        public override IPackage GetPackage()
        {
            throw new System.NotSupportedException($"{nameof(OperationUpdate)} not support GetPackage().");
        }

        public IPackage GetTargetPackage()
        {
            return target;
        }

        public override string ToString()
        {
            return $"Updating {initial.GetNamePretty()} ({initial.GetVersionPrettyFull()}) to {target.GetNamePretty()} ({target.GetVersionPrettyFull()})";
        }
    }
}

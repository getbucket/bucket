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
    /// Solver install operation.
    /// </summary>
    internal sealed class OperationInstall : BaseOperation
    {
        private readonly IPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationInstall"/> class.
        /// </summary>
        /// <param name="package">The install package instance.</param>
        /// <param name="reason">Why install the package.</param>
        public OperationInstall(IPackage package, Rule reason = null)
            : base(reason)
        {
            this.package = package;
        }

        /// <inheritdoc />
        public override JobCommand JobCommand => JobCommand.Install;

        /// <inheritdoc />
        public override IPackage GetPackage()
        {
            return package;
        }

        public override string ToString()
        {
            return $"Installing {package.GetNamePretty()} ({package.GetVersionPrettyFull()})";
        }
    }
}

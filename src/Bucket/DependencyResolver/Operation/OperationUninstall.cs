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
    /// Solver uninstall operation.
    /// </summary>
    internal sealed class OperationUninstall : BaseOperation
    {
        private readonly IPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationUninstall"/> class.
        /// </summary>
        /// <param name="package">The uninstall package instance.</param>
        /// <param name="reason">Why uninstall the package.</param>
        public OperationUninstall(IPackage package, Rule reason = null)
            : base(reason)
        {
            this.package = package;
        }

        /// <inheritdoc />
        public override JobCommand JobCommand => JobCommand.Uninstall;

        /// <inheritdoc />
        public override IPackage GetPackage()
        {
            return package;
        }

        public override string ToString()
        {
            return $"Uninstalling {package.GetNamePretty()} ({package.GetVersionPrettyFull()})";
        }
    }
}

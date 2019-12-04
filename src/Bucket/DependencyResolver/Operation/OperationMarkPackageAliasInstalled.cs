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
    /// Solver mark alias package installed operation.
    /// </summary>
    internal sealed class OperationMarkPackageAliasInstalled : BaseOperation
    {
        private readonly PackageAlias package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationMarkPackageAliasInstalled"/> class.
        /// </summary>
        /// <param name="package">The mark installed package instance.</param>
        /// <param name="reason">Why install the package.</param>
        public OperationMarkPackageAliasInstalled(PackageAlias package, Rule reason = null)
            : base(reason)
        {
            this.package = package;
        }

        /// <inheritdoc />
        public override JobCommand JobCommand => JobCommand.MarkPackageAliasInstalled;

        /// <inheritdoc />
        public override IPackage GetPackage()
        {
            return package;
        }

        public override string ToString()
        {
            return $"Marking {package.GetNamePretty()} ({package.GetVersionPrettyFull()}) as installed, alias of {package.GetAliasOf().GetNamePretty()} ({package.GetAliasOf().GetVersionPrettyFull()})";
        }
    }
}

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

using Bucket.DependencyResolver;
using Bucket.DependencyResolver.Operation;
using Bucket.DependencyResolver.Policy;
using Bucket.IO;
using Bucket.Repository;

namespace Bucket.Installer
{
    /// <summary>
    /// Represents a package event.
    /// </summary>
    public class PackageEventArgs : InstallerEventArgs
    {
        private readonly IOperation operation;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageEventArgs"/> class.
        /// </summary>
        public PackageEventArgs(
            string eventName,
            Bucket bucket,
            IIO io,
            bool devMode,
            IPolicy policy,
            Pool pool,
            RepositoryComposite repositoryInstalled,
            Request request,
            IOperation[] operations,
            IOperation operation)
            : base(eventName, bucket, io, devMode, policy, pool, repositoryInstalled, request, operations)
        {
            this.operation = operation;
        }

        /// <summary>
        /// Get the current package operation.
        /// </summary>
        public IOperation GetOperation() => operation;
    }
}

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
using Bucket.EventDispatcher;
using Bucket.IO;
using Bucket.Repository;

namespace Bucket.Installer
{
    /// <summary>
    /// An event for all installer.
    /// </summary>
    public class InstallerEventArgs : BucketEventArgs
    {
        private readonly Bucket bucket;
        private readonly IIO io;
        private readonly IPolicy policy;
        private readonly Pool pool;
        private readonly RepositoryComposite repositoryInstalled;
        private readonly Request request;
        private readonly IOperation[] operations;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallerEventArgs"/> class.
        /// </summary>
        public InstallerEventArgs(
            string eventName,
            Bucket bucket,
            IIO io,
            bool devMode,
            IPolicy policy,
            Pool pool,
            RepositoryComposite repositoryInstalled,
            Request request,
            IOperation[] operations)
            : base(eventName)
        {
            IsDevMode = devMode;
            this.bucket = bucket;
            this.io = io;
            this.policy = policy;
            this.pool = pool;
            this.repositoryInstalled = repositoryInstalled;
            this.request = request;
            this.operations = operations;
        }

        /// <summary>
        /// Gets a value indicating whether is dev mode.
        /// </summary>
        public bool IsDevMode { get; }

        /// <summary>
        /// Get bucket instance.
        /// </summary>
        public Bucket GetBucket() => bucket;

        /// <summary>
        /// Get input/output instance.
        /// </summary>
        public IIO GetIO() => io;

        /// <summary>
        /// Get the policy instance.
        /// </summary>
        public IPolicy GetPolicy() => policy;

        /// <summary>
        /// Get the pool instance.
        /// </summary>
        public Pool GetPool() => pool;

        /// <summary>
        /// Get the installed repository instance.
        /// </summary>
        public RepositoryComposite GetRepositoryInstalled() => repositoryInstalled;

        /// <summary>
        /// Get the request instance.
        /// </summary>
        public Request GetRequest() => request;

        /// <summary>
        /// Get an array of installer operations.
        /// </summary>
        public IOperation[] GetOperations() => operations;
    }
}

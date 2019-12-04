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

using Bucket.EventDispatcher;
using Bucket.IO;

namespace Bucket.Script
{
    /// <summary>
    /// The script event args class.
    /// </summary>
    public class ScriptEventArgs : BucketEventArgs
    {
        private readonly Bucket bucket;
        private readonly IIO io;
        private BucketEventArgs baseEventArgs;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptEventArgs"/> class.
        /// </summary>
        /// <param name="name">The event name.</param>
        /// <param name="bucket">The bucket instance.</param>
        /// <param name="io">The input/output instance.</param>
        /// <param name="devMode">Whether is operations with dev packages.</param>
        /// <param name="args">Arguments passed by the user.</param>
        public ScriptEventArgs(string name, Bucket bucket, IIO io, bool devMode = false, string[] args = null)
            : base(name, args)
        {
            this.bucket = bucket;
            this.io = io;
            IsDevMode = devMode;
        }

        /// <summary>
        /// Gets a value indicating whether is dev mode (operations with dev packages).
        /// </summary>
        public bool IsDevMode { get; }

        /// <summary>
        /// Gets the bucket instance.
        /// </summary>
        public Bucket GetBucket()
        {
            return bucket;
        }

        /// <summary>
        /// Gets the input/output instance.
        /// </summary>
        public IIO GetIO()
        {
            return io;
        }

        /// <summary>
        /// Gets the base event args instance.
        /// </summary>
        public BucketEventArgs GetBaseEventArgs()
        {
            return baseEventArgs;
        }

        /// <summary>
        /// Set the base event args instance.
        /// </summary>
        public void SetBaseEventArgs(BucketEventArgs eventArgs)
        {
            baseEventArgs = CalculateBaseEvent(eventArgs);
        }

        private BucketEventArgs CalculateBaseEvent(BucketEventArgs eventArgs)
        {
            if (eventArgs is ScriptEventArgs scriptEventArgs && scriptEventArgs.GetBaseEventArgs() != null)
            {
                return CalculateBaseEvent(scriptEventArgs.GetBaseEventArgs());
            }

            return eventArgs;
        }
    }
}

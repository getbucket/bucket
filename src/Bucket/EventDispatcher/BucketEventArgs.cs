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

using GameBox.Console;
using GameBox.Console.EventDispatcher;
using System;

namespace Bucket.EventDispatcher
{
    /// <summary>
    /// Represents the base class for classes that contain bucket event data,
    /// and provides a value to use for events that do not include event data.
    /// </summary>
    public class BucketEventArgs : EventArgs, IStoppableEvent
    {
        private readonly string[] args;

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketEventArgs"/> class.
        /// </summary>
        /// <param name="name">The event name.</param>
        /// <param name="args">Arguments passed by the user.</param>
        public BucketEventArgs(string name, string[] args = null)
        {
            Name = name;
            this.args = args ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets a value represents the event name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value represents event exit code.
        /// </summary>
        public int ExitCode { get; private set; } = ExitCodes.Normal;

        /// <inheritdoc />
        public bool IsPropagationStopped { get; private set; } = false;

        /// <summary>
        /// Returns the event's arguments.
        /// </summary>
        public virtual string[] GetArguments()
        {
            return args;
        }

        /// <summary>
        /// Sets a event exit code.
        /// </summary>
        /// <param name="code">The event exit code.</param>
        public virtual void SetExitCode(int code)
        {
            ExitCode = code;
        }

        /// <summary>
        /// Stop the event propagation.
        /// </summary>
        public void StopPropagation()
        {
            IsPropagationStopped = true;
        }
    }
}

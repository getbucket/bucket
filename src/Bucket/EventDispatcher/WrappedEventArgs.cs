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

using System;

namespace Bucket.EventDispatcher
{
    /// <summary>
    /// Wrapped a basic event.
    /// </summary>
    internal sealed class WrappedEventArgs : BucketEventArgs
    {
        private readonly EventArgs eventArgs;

        /// <summary>
        /// Initializes a new instance of the <see cref="WrappedEventArgs"/> class.
        /// </summary>
        /// <param name="name">The event name.</param>
        /// <param name="eventArgs">The basic event args instance.</param>
        public WrappedEventArgs(string name, EventArgs eventArgs)
            : base(name, null)
        {
            this.eventArgs = eventArgs;
        }

        public EventArgs GetBaseEventArgs()
        {
            return eventArgs;
        }
    }
}

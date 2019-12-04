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

using GameBox.Console.EventDispatcher;

namespace Bucket.EventDispatcher
{
    /// <summary>
    /// Represents a base event system extension for bucket projcet.
    /// </summary>
    public static class ExtensionIEventDispatcher
    {
        /// <summary>
        /// Dispatches an event to all registered listeners.
        /// </summary>
        /// <param name="dispatcher">The name of the event.</param>
        /// <param name="sender">The source of the event.</param>
        /// <param name="eventArgs">The event object to pass to the event listeners.</param>
        public static void Dispatch(this IEventDispatcher dispatcher, object sender, BucketEventArgs eventArgs)
        {
            dispatcher.Dispatch(eventArgs.Name, sender, eventArgs);
        }
    }
}

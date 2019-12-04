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
using GameBox.Console.Input;

namespace Bucket.Plugin
{
    /// <summary>
    /// The pre command run event.
    /// </summary>
    public class PreCommandRunEventArgs : BucketEventArgs
    {
        private readonly IInput input;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreCommandRunEventArgs"/> class.
        /// </summary>
        public PreCommandRunEventArgs(string eventName, IInput input, string commandName)
            : base(eventName)
        {
            CommandName = commandName;
            this.input = input;
        }

        /// <summary>
        /// Gets a value indicate the name of the command being run.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// Returns the command input interface.
        /// </summary>
        public IInput GetInput() => input;
    }
}

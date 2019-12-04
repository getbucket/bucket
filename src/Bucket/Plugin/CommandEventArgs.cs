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
using GameBox.Console.Output;

namespace Bucket.Plugin
{
    /// <summary>
    /// An event for all commands.
    /// </summary>
    public class CommandEventArgs : BucketEventArgs
    {
        private readonly IInput input;
        private readonly IOutput output;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandEventArgs"/> class.
        /// </summary>
        public CommandEventArgs(string eventName, string commandName, IInput input, IOutput output, string[] args = null)
            : base(eventName, args)
        {
            CommandName = commandName;
            this.input = input;
            this.output = output;
        }

        /// <summary>
        /// Gets a value indicate the name of the command being run.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// Returns the command input interface.
        /// </summary>
        public IInput GetInput() => input;

        /// <summary>
        /// Returns the command output interface.
        /// </summary>
        public IOutput GetOutput() => output;
    }
}

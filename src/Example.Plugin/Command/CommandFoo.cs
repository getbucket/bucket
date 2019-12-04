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

using Bucket.Command;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;

namespace Example.Plugin.Command
{
    /// <summary>
    /// The test command.
    /// </summary>
    public class CommandFoo : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("foo")
                .SetDescription("This is a plugin command foo.");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            output.Write("Command foo");
            return ExitCodes.Normal;
        }
    }
}

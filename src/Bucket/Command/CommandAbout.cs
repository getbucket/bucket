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
using GameBox.Console.Input;
using GameBox.Console.Output;

namespace Bucket.Command
{
    /// <summary>
    /// This command display bucket information about.
    /// </summary>
    public class CommandAbout : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("about")
                .SetDescription("Shows the short information about Bucket.")
                .SetHelp(@"
<info>{environment.executable_file} {command.name}</info>
");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            GetIO().Write(
@"
<info>Bucket - Package Dependency Manager</info>
<comment>Bucket is a dependency manager tracking local dependencies of your projects and libraries.
See https://github.com/getbucket/bucket/wiki for more information.</comment>
");
            return ExitCodes.Normal;
        }
    }
}

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
    /// This command shows all activated plugins.
    /// </summary>
    public class CommandPlugin : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("plugin")
                .SetDescription("Shows all activated plugins.")
                .SetHelp(
@"The <info>plugin</info> shows all activated plugins.");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            var io = GetIO();
            var bucket = GetBucket(false);
            if (!bucket)
            {
                bucket = Factory.CreateGlobal(io, input.HasRawOption("--no-plugins"));
            }

            if (!bucket)
            {
                return ExitCodes.Normal;
            }

            var pluginManager = bucket.GetPluginManager();
            foreach (var plugin in pluginManager.GetPlugins())
            {
                io.Write(plugin.Name);
            }

            return ExitCodes.Normal;
        }
    }
}

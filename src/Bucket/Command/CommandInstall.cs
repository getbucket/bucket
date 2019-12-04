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
using Bucket.Plugin;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;

namespace Bucket.Command
{
    /// <summary>
    /// This command installs the project dependencies.
    /// </summary>
    public class CommandInstall : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("install")
                .SetAlias("i")
                .SetDescription("Installs the project dependencies from the bucket.lock file if present, or falls back on the bucket.json.")
                .SetDefinition(new IInputDefinition[]
                {
                    new InputArgument("packages", InputArgumentModes.IsArray | InputArgumentModes.Optional, "Should not be provided, use bucket require instead to add a given package to bucket.json."),
                    new InputOption("prefer-source", null, InputOptionModes.ValueNone, "Forces installation from package sources when possible, including VCS information."),
                    new InputOption("prefer-dist", null, InputOptionModes.ValueNone, "Forces installation from package dist even for dev versions."),
                    new InputOption("no-dev", null, InputOptionModes.ValueNone, "Disables installation of require-dev packages."),
                    new InputOption("dry-run", null, InputOptionModes.ValueNone, "Outputs the operations but will not execute anything (implicitly enables --verbose)."),
                    new InputOption("no-scripts", null, InputOptionModes.ValueNone, "Skips the execution of all scripts defined in bucket.json file."),
                    new InputOption("no-progress", null, InputOptionModes.ValueNone, "Do not output download progress."),
                    new InputOption("no-suggest", null, InputOptionModes.ValueNone, "Do not show package suggestions."),
                    new InputOption("ignore-platform-reqs", null, InputOptionModes.ValueNone, "Ignore platform requirements."),
                }).SetHelp(
@"The <info>install</info> command reads the bucket.lock file from
the current directory, processes it, and downloads and installs all the
libraries and dependencies outlined in that file. If the file does not
exist it will look for bucket.json and do the same.

<info>bucket {command.name}</info>");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            var io = GetIO();
            string[] args = input.GetArgument("packages");
            if (!args.Empty())
            {
                io.WriteError($"<error>Invalid argument {string.Join(Str.Space, args)}. Use \"bucket require {string.Join(Str.Space, args)}\" instead to add packages to your bucket.json.</error>");
                return ExitCodes.GeneralException;
            }

            var bucket = GetBucket(true, input.GetOption("no-plugins"));

            // todo: set no-progress.
            var commandEvent = new CommandEventArgs(PluginEvents.Command, "install", input, output);
            bucket.GetEventDispatcher().Dispatch(this, commandEvent);

            var installer = new BucketInstaller(io, bucket);

            var config = bucket.GetConfig();
            var (preferSource, preferDist) = GetPreferredInstallOptions(config, input);

            installer.SetDryRun(input.GetOption("dry-run"))
                .SetVerbose(input.GetOption("verbose"))
                .SetPreferSource(preferSource)
                .SetPreferDist(preferDist)
                .SetDevMode(!input.GetOption("no-dev"))
                .SetRunScripts(!input.GetOption("no-scripts"))
                .SetSkipSuggest(input.GetOption("no-suggest"))
                .SetIgnorePlatformRequirements(input.GetOption("ignore-platform-reqs"));

            if (input.GetOption("no-plugins"))
            {
                installer.DisablePlugins();
            }

            return installer.Run();
        }
    }
}

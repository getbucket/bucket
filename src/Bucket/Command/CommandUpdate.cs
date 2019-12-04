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
using Bucket.Package;
using Bucket.Plugin;
using Bucket.Util;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.Command
{
    /// <summary>
    /// This command updates the project dependencies.
    /// </summary>
    public class CommandUpdate : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("update")
                .SetAlias("u", "upgrade")
                .SetDescription("Upgrades your dependencies to the latest version according to bucket.json, and updates the bucket.lock file.")
                .SetDefinition(new IInputDefinition[]
                {
                    new InputArgument("packages", InputArgumentModes.IsArray | InputArgumentModes.Optional, "Packages that should be updated, if not provided all packages are."),
                    new InputOption("prefer-source", null, InputOptionModes.ValueNone, "Forces installation from package sources when possible, including VCS information."),
                    new InputOption("prefer-dist", null, InputOptionModes.ValueNone, "Forces installation from package dist even for dev versions."),
                    new InputOption("no-dev", null, InputOptionModes.ValueNone, "Disables installation of require-dev packages."),
                    new InputOption("dry-run", null, InputOptionModes.ValueNone, "Outputs the operations but will not execute anything (implicitly enables --verbose)."),
                    new InputOption("lock", null, InputOptionModes.ValueNone, "Only updates the lock file hash to suppress warning about the lock file being out of date."),
                    new InputOption("no-scripts", null, InputOptionModes.ValueNone, "Skips the execution of all scripts defined in bucket.json file."),
                    new InputOption("no-progress", null, InputOptionModes.ValueNone, "Do not output download progress."),
                    new InputOption("no-suggest", null, InputOptionModes.ValueNone, "Do not show package suggestions."),
                    new InputOption("with-dependencies", null, InputOptionModes.ValueNone, "Add also dependencies of whitelisted packages to the whitelist, except those defined in root package."),
                    new InputOption("with-all-dependencies", null, InputOptionModes.ValueNone, "Add also all dependencies of whitelisted packages to the whitelist, including those defined in root package."),
                    new InputOption("prefer-stable", null, InputOptionModes.ValueNone, "Prefer stable versions of dependencies."),
                    new InputOption("prefer-lowest", null, InputOptionModes.ValueNone, "Prefer lowest versions of dependencies."),
                    new InputOption("ignore-platform-reqs", null, InputOptionModes.ValueNone, "Ignore platform requirements."),
                    new InputOption("root-requires", null, InputOptionModes.ValueNone, "Restricts the update to your first degree dependencies."),
                })
                .SetHelp(
@"The <info>update</info> command reads the bucket.json file from the
current directory, processes it, and updates, removes or installs all the
dependencies.

<info>bucket {command.name}</info>

To limit the update operation to a few packages, you can list the package(s)
you want to update as such:

<info>bucket {command.name} foo/package bar/package [...]</info>

You may also use an asterisk (*) pattern to limit the update operation to package(s)
from a specific vendor:

<info>bucket {command.name} foo/package bar/* [...]</info>");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            var bucket = GetBucket(true, input.GetOption("no-plugins"));
            string[] packages = input.GetArgument("packages") ?? Array.Empty<string>();

            packages = ProcessRootRequires(bucket.GetPackage(), input, packages);

            // todo: set no-progress.
            // todo: add gui interactive select the packages.
            var commandEvent = new CommandEventArgs(PluginEvents.Command, "update", input, output);
            bucket.GetEventDispatcher().Dispatch(this, commandEvent);

            var io = GetIO();
            var installer = new BucketInstaller(io, bucket);

            var config = bucket.GetConfig();
            var (preferSource, preferDist) = GetPreferredInstallOptions(config, input);

            ISet<string> GetUpdateWhitlist()
            {
                if (input.GetOption("lock"))
                {
                    return new HashSet<string>(new[] { "lock" });
                }

                return packages.Empty() ? null : new HashSet<string>(packages);
            }

            installer.SetDryRun(input.GetOption("dry-run"))
                .SetVerbose(input.GetOption("verbose"))
                .SetPreferSource(preferSource)
                .SetPreferDist(preferDist)
                .SetDevMode(!input.GetOption("no-dev"))
                .SetRunScripts(!input.GetOption("no-scripts"))
                .SetSkipSuggest(input.GetOption("no-suggest"))
                .SetIgnorePlatformRequirements(input.GetOption("ignore-platform-reqs"))
                .SetUpdate(true)
                .SetUpdateWhitelist(GetUpdateWhitlist())
                .SetWhitelistAllDependencies(input.GetOption("with-all-dependencies"))
                .SetWhitelistTransitiveDependencies(input.GetOption("with-dependencies"))
                .SetPreferStable(input.GetOption("prefer-stable"))
                .SetPreferLowest(input.GetOption("prefer-lowest"));

            if (input.GetOption("no-plugins"))
            {
                installer.DisablePlugins();
            }

            return installer.Run();
        }

        /// <summary>
        /// Process the update to your first degree dependencies.
        /// </summary>
        protected virtual string[] ProcessRootRequires(IPackageRoot packageRoot, IInput input, string[] packages)
        {
            if (!input.GetOption("root-requires"))
            {
                return packages;
            }

            var requires = Arr.Map(packageRoot.GetRequires(), (require) => require.GetSource());
            if (!input.GetOption("no-dev"))
            {
                var requiresDev = Arr.Map(packageRoot.GetRequiresDev(), (require) => require.GetSource());
                requires = Arr.Merge(requires, requiresDev);
            }

            if (!packages.Empty())
            {
                packages = packages.Intersect(requires).ToArray();
            }
            else
            {
                packages = requires;
            }

            return packages;
        }
    }
}

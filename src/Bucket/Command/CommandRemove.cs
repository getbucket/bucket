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

using Bucket.Configuration;
using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.Json;
using Bucket.Package;
using Bucket.Plugin;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Bucket.Command
{
    /// <summary>
    /// This command remove the project dependencies.
    /// </summary>
    public class CommandRemove : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("remove")
                .SetAlias("uninstall")
                .SetDescription("Removes a package from the require or require-dev.")
                .SetDefinition(new IInputDefinition[]
                {
                    new InputArgument("packages", InputArgumentModes.IsArray | InputArgumentModes.Required, "Packages that should be removed."),
                    new InputOption("dev", null, InputOptionModes.ValueNone, "Removes a package from the require-dev section."),
                    new InputOption("no-progress", null, InputOptionModes.ValueNone, "Do not output download progress."),
                    new InputOption("no-update", null, InputOptionModes.ValueNone, "Disables the automatic update of the dependencies."),
                    new InputOption("no-scripts", null, InputOptionModes.ValueNone, "Skips the execution of all scripts defined in bucket.json file."),
                    new InputOption("update-no-dev", null, InputOptionModes.ValueNone, "Run the dependency update with the --no-dev option."),
                    new InputOption("no-update-with-dependencies", null, InputOptionModes.ValueNone, "Does not allow inherited dependencies to be updated with explicit dependencies."),
                    new InputOption("ignore-platform-reqs", null, InputOptionModes.ValueNone, "Ignore platform requirements."),
                })
                .SetHelp(
@"The <info>{command.name}</info> command removes a package from the current
list of installed packages

<info>bucket {command.name}</info>");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            string[] packages = input.GetArgument("packages");
            packages = Arr.Map(packages, (package) => package.ToLower());

            var file = Factory.GetBucketFile();
            var filePath = Path.Combine(Environment.CurrentDirectory, file);

            var jsonFile = new JsonFile(filePath);
            var configBucket = jsonFile.Read<ConfigBucket>();
            var backup = File.ReadAllText(filePath);

            var source = new JsonConfigSource(jsonFile);
            var io = GetIO();

            var requireKey = input.GetOption("dev") ? LinkType.RequireDev : LinkType.Require;
            var alternativeRequireKey = input.GetOption("dev") ? LinkType.Require : LinkType.RequireDev;

            var requireMapping = new Dictionary<string, string>();
            var alternativeRequireMapping = new Dictionary<string, string>();

            void EstablishCaseMapping(IDictionary<string, string> mapping, LinkType type)
            {
                var collection = type == LinkType.Require ? configBucket.Requires : configBucket.RequiresDev;

                if (collection == null)
                {
                    return;
                }

                foreach (var item in collection)
                {
                    mapping[item.Key.ToLower()] = item.Key;
                }
            }

            EstablishCaseMapping(requireMapping, requireKey);
            EstablishCaseMapping(alternativeRequireMapping, alternativeRequireKey);

            bool TryGetMatchPackages(IDictionary<string, string> mapping, string package, out IEnumerable<string> matches)
            {
                var result = new List<string>();
                var regexPackage = BasePackage.PackageNameToRegexPattern(package);
                foreach (var item in mapping)
                {
                    if (Regex.IsMatch(item.Key, regexPackage, RegexOptions.IgnoreCase))
                    {
                        result.Add(item.Value);
                    }
                }

                matches = result;
                return result.Count > 0;
            }

            foreach (var package in packages)
            {
                if (requireMapping.TryGetValue(package, out string originalName))
                {
                    source.RemoveLink(requireKey, originalName);
                }
                else if (alternativeRequireMapping.TryGetValue(package, out originalName))
                {
                    io.WriteError($"<warning>{originalName} could not be found in {Str.LowerDashes(requireKey.ToString())} but it is present in {Str.LowerDashes(alternativeRequireKey.ToString())}</warning>");
                    if (io.IsInteractive && io.AskConfirmation($"Do you want to remove it from {Str.LowerDashes(alternativeRequireKey.ToString())} [<comment>yes</comment>]? ", true))
                    {
                        source.RemoveLink(alternativeRequireKey, originalName);
                    }
                }
                else if (TryGetMatchPackages(requireMapping, package, out IEnumerable<string> matches))
                {
                    foreach (var match in matches)
                    {
                        source.RemoveLink(requireKey, match);
                    }
                }
                else if (TryGetMatchPackages(alternativeRequireMapping, package, out matches))
                {
                    foreach (var match in matches)
                    {
                        io.WriteError($"<warning>{match} could not be found in {Str.LowerDashes(requireKey.ToString())} but it is present in {Str.LowerDashes(alternativeRequireKey.ToString())}</warning>");
                        if (io.IsInteractive && io.AskConfirmation($"Do you want to remove it from {Str.LowerDashes(alternativeRequireKey.ToString())} [<comment>yes</comment>]? ", true))
                        {
                            source.RemoveLink(alternativeRequireKey, match);
                        }
                    }
                }
                else
                {
                    io.WriteError($"<warning>{package} is not required in your bucket.json and has not been removed</warning>");
                }
            }

            if (input.GetOption("no-update"))
            {
                return ExitCodes.Normal;
            }

            // todo: implement installer uninstall.
            ResetBucket();

            var bucket = GetBucket(true, input.GetOption("no-plugins"));

            // todo: set no-progress.
            var commandEvent = new CommandEventArgs(PluginEvents.Command, "remove", input, output);
            bucket.GetEventDispatcher().Dispatch(this, commandEvent);

            ISet<string> GetRemoveWhitlist()
            {
                Guard.Requires<UnexpectedException>(packages.Length > 0);
                return new HashSet<string>(packages);
            }

            var installer = new BucketInstaller(io, bucket);
            var status = installer
                .SetVerbose(input.GetOption("verbose"))
                .SetDevMode(!input.GetOption("update-no-dev"))
                .SetRunScripts(!input.GetOption("no-scripts"))
                .SetUpdate(true)
                .SetUpdateWhitelist(GetRemoveWhitlist())
                .SetWhitelistTransitiveDependencies(!input.GetOption("no-update-with-dependencies"))
                .SetIgnorePlatformRequirements(input.GetOption("ignore-platform-reqs"))
                .Run();

            if (status != 0)
            {
                io.WriteError($"{Environment.NewLine}<error>Removal failed, reverting {file} to its original content.</error>");
                File.WriteAllText(filePath, backup);
            }

            return status;
        }
    }
}

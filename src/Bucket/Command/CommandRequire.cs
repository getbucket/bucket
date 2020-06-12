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
using Bucket.DependencyResolver;
using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.IO;
using Bucket.Json;
using Bucket.Package;
using Bucket.Package.Version;
using Bucket.Plugin;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;
using GameBox.Console.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using BVersionParser = Bucket.Package.Version.VersionParser;
using SException = System.Exception;

namespace Bucket.Command
{
    /// <summary>
    /// This command add some new dependencies to require(-dev) property.
    /// </summary>
    public class CommandRequire : BaseCommand
    {
        private static readonly string NewBucketContent = new ConfigBucket();
        private readonly IDictionary<string, Pool> pools;
        private string file;
        private string filePath;
        private bool newlyCreated;
        private JsonFile json;
        private string backup;
        private IRepository repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandRequire"/> class.
        /// </summary>
        public CommandRequire()
            : base()
        {
            pools = new Dictionary<string, Pool>();
        }

        /// <summary>
        /// Rollback the bucket file.
        /// If the newly created one will auto deleted.
        /// </summary>
        public void RevertBucketFile(bool hardExit = false)
        {
            var io = GetIO();

            if (newlyCreated)
            {
                io.WriteError($"{Environment.NewLine}<error>Installation failed, deleting {file}.</error>");
                json.Delete();
            }
            else
            {
                io.WriteError($"{Environment.NewLine}<error>Installation failed, reverting {file} to its original content.</error>");
                File.WriteAllText(filePath, backup);
            }

            if (hardExit)
            {
                Environment.Exit(ExitCodes.GeneralException);
            }
        }

        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("require")
                .SetDescription("Adds required packages to your bucket.json and installs them.")
                .SetDefinition(new IInputDefinition[]
                {
                    new InputArgument("packages", InputArgumentModes.IsArray | InputArgumentModes.Optional, "Optional package name can also include a version constraint, e.g. foo/bar or foo/bar@1.0.0"),
                    new InputOption("prefer-source", null, InputOptionModes.ValueNone, "Forces installation from package sources when possible, including VCS information."),
                    new InputOption("prefer-dist", null, InputOptionModes.ValueNone, "Forces installation from package dist even for dev versions."),
                    new InputOption("dry-run", null, InputOptionModes.ValueNone, "Outputs the operations but will not execute anything (implicitly enables --verbose)."),
                    new InputOption("no-scripts", null, InputOptionModes.ValueNone, "Skips the execution of all scripts defined in bucket.json file."),
                    new InputOption("no-progress", null, InputOptionModes.ValueNone, "Do not output download progress."),
                    new InputOption("no-suggest", null, InputOptionModes.ValueNone, "Do not show package suggestions."),
                    new InputOption("update-with-dependencies", null, InputOptionModes.ValueNone, "Allows inherited dependencies to be updated, except those that are root requirements."),
                    new InputOption("update-with-all-dependencies", null, InputOptionModes.ValueNone, "Allows all inherited dependencies to be updated, including those that are root requirements."),
                    new InputOption("prefer-stable", null, InputOptionModes.ValueNone, "Prefer stable versions of dependencies."),
                    new InputOption("prefer-lowest", null, InputOptionModes.ValueNone, "Prefer lowest versions of dependencies."),
                    new InputOption("ignore-platform-reqs", null, InputOptionModes.ValueNone, "Ignore platform requirements."),

                    new InputOption("dev", null, InputOptionModes.ValueNone, "Add requirement to require-dev."),
                    new InputOption("no-update", null, InputOptionModes.ValueNone, "Disables the automatic update of the dependencies."),
                    new InputOption("update-no-dev", null, InputOptionModes.ValueNone, "Run the dependency update with the --no-dev option."),
                    new InputOption("sort-packages", null, InputOptionModes.ValueNone, "Sorts packages when adding/updating a new dependency"),

                    new InputOption("allow-http", null, InputOptionModes.ValueNone, "Allow installation using unsafe(http, git, svn, ftp) protocols"),

                    // todo: add stability option.
                })
                .SetHelp(
@"The require command adds required packages to your bucket.json and installs them.

If you do not specify a package, bucket will prompt you to search for a package, and given results, provide a list of
matches to require.

If you do not specify a version constraint, bucket will choose a suitable one based on the available package versions.

If you do not want to install the new dependencies immediately you can call it with --no-update");
        }

        /// <inheritdoc />
        protected override void Initialize(IInput input, IOutput output)
        {
            base.Initialize(input, output);

            if (input.GetOption("allow-http"))
            {
                GuardConfig.AllowHttp = true;
            }
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            file = Factory.GetBucketFile();
            filePath = Path.Combine(Environment.CurrentDirectory, file);
            var io = GetIO();

            newlyCreated = !File.Exists(filePath);

            if (newlyCreated)
            {
                try
                {
                    File.WriteAllText(filePath, NewBucketContent);
                }
#pragma warning disable CA1031
                catch (SException ex)
#pragma warning restore CA1031
                {
                    io.WriteError($"<error>{file} could not be created: {ex.Message}.</error>");
                    return ExitCodes.GeneralException;
                }
            }

            backup = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(backup))
            {
                File.WriteAllText(filePath, NewBucketContent);
                backup = NewBucketContent;
            }

            json = new JsonFile(filePath);

            var bucket = GetBucket(true, input.GetOption("no-plugins"));

            // todo: import mock platform
            repository = new RepositoryComposite(
                                Arr.Merge(
                                    new[] { new RepositoryPlatform(bucket.GetPackage().GetPlatforms()) },
                                    bucket.GetRepositoryManager().GetRepositories()));

            var preferredStability = Stabilities.Stable;
            var preferredStable = bucket.GetPackage().IsPreferStable ?? false;
            if (!preferredStable)
            {
                preferredStability = bucket.GetPackage().GetMinimumStability() ?? preferredStability;
            }

            var requireKey = input.GetOption("dev") ? LinkType.RequireDev : LinkType.Require;
            var removeKey = input.GetOption("dev") ? LinkType.Require : LinkType.RequireDev;

            var requirements = FormatRequirements(
                DetermineRequirements(input, output, input.GetArgument("packages"), preferredStability, !input.GetOption("no-update")));

            // validate requirements format.
            var versionParser = new BVersionParser();
            foreach (var item in requirements)
            {
                var package = item.Key;
                var constraint = item.Value;
                if (package.ToLower() == bucket.GetPackage().GetName())
                {
                    io.WriteError($"<error>Root package \"{package}\" cannot require itself in its bucket.json</error>");
                    return ExitCodes.Normal;
                }

                versionParser.ParseConstraints(constraint);
            }

            var sortPackages = input.GetOption("sort-packages") || bucket.GetConfig().Get(Settings.SortPackages);

            if (!UpdateFileCleanly(json, requirements, requireKey, removeKey, sortPackages))
            {
                File.WriteAllText(filePath, backup);
                throw new RuntimeException("Failed to write to file successfully, operation has been rolled back.");
            }

            io.WriteError($"<info>{file} has been {(newlyCreated ? "created" : "update")}</info>");

            if (input.GetOption("no-update"))
            {
                return ExitCodes.Normal;
            }

            try
            {
                return DoUpdate(input, output, io, requirements);
            }
            catch (SException)
            {
                RevertBucketFile(false);
                throw;
            }
        }

        /// <summary>
        /// Determine the requirements packages.
        /// </summary>
        protected virtual string[] DetermineRequirements(IInput input, IOutput output, string[] requires, Stabilities preferredStability, bool checkProvidedVersions)
        {
            var result = new List<string>();
            var io = GetIO();
            if (!requires.Empty())
            {
                var normalizedRequires = NormalizeRequirements(requires);
                foreach (var (name, version) in normalizedRequires)
                {
                    if (string.IsNullOrEmpty(version))
                    {
                        // determine the best version automatically
                        var (bestName, bestVersion) = FindBestVersionAndNameForPackage(input, name, preferredStability);
                        io.WriteError($"Using version <info>{bestVersion}</info> for <info>{bestName}</info>");
                        result.Add($"{bestName} {bestVersion}");
                    }
                    else
                    {
                        // check that the specified version/constraint exists before we proceed
                        var (bestName, bestVersion) = FindBestVersionAndNameForPackage(input, name, preferredStability, checkProvidedVersions ? version : null, Stabilities.Dev);
                        result.Add($"{bestName} {version}");
                    }
                }

                return result.ToArray();
            }

            bool AskPackage(out string name)
            {
                name = io.Ask("Search for a package: ");
                return !string.IsNullOrEmpty(name);
            }

            var versionParser = new BVersionParser();
            while (AskPackage(out string name))
            {
                var matches = FindPackages(name);
                if (matches.Empty())
                {
                    continue;
                }

                var exactMatch = false;
                var choices = new List<string>();
                for (var i = 0; i < matches.Length; i++)
                {
                    var deprecated = string.Empty;
                    if (matches[i].IsDeprecated)
                    {
                        var replacement = matches[i].GetReplacementPackage();
                        if (string.IsNullOrEmpty(replacement))
                        {
                            deprecated = "No replacement was suggested.";
                        }
                        else
                        {
                            deprecated = $"Use {replacement} instead.";
                        }

                        deprecated = $"<warning>Deprecated. {deprecated}</warning>";
                    }

                    choices.Add($" <info>[{i}]</info> {matches[i].GetName()} {deprecated}");

                    if (matches[i].GetName() == name)
                    {
                        exactMatch = true;
                        break;
                    }
                }

                // no match, prompt which to pick
                if (!exactMatch)
                {
                    io.WriteError(new[]
                    {
                        string.Empty,
                        $"Found <info>{matches.Length}</info> packages matching <info>{name}</info>",
                        string.Empty,
                    });

                    io.WriteError(choices);

                    Mixture Validator(Mixture selection)
                    {
                        if (string.IsNullOrEmpty(selection))
                        {
                            return null;
                        }

                        if (int.TryParse(selection, out int index)
                            && index < matches.Length)
                        {
                            return matches[index].GetName();
                        }

                        var packageMatches = Regex.Match(selection, @"^\s*(?<name>[\S/]+)(?:\s+(?<version>\S+))?\s*$");
                        if (!packageMatches.Success)
                        {
                            throw new RuntimeException("Not a valid selection");
                        }

                        var version = packageMatches.Groups["version"].Value;
                        if (string.IsNullOrEmpty(version))
                        {
                            return packageMatches.Groups["name"].Value;
                        }

                        // validate version constraint.
                        versionParser.ParseConstraints(version);

                        return packageMatches.Groups["name"].Value + Str.Space + version;
                    }

                    name = io.AskAndValidate("Enter package # to add, or the complete package name if it is not listed: ", Validator, 3);
                }

                // no constraint yet, determine the best version automatically.
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (name.Contains(Str.Space))
                {
                    result.Add(name);
                    continue;
                }

                Mixture ValidatorConstraint(Mixture value)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return null;
                    }

                    var version = value.ToString().Trim();

                    // validate version constraint.
                    versionParser.ParseConstraints(version);
                    return version;
                }

                string constraint = io.AskAndValidate(
                        "Enter the version constraint to require (or leave blank to use the latest version): ",
                        ValidatorConstraint,
                        3);

                if (string.IsNullOrEmpty(constraint))
                {
                    (_, constraint) = FindBestVersionAndNameForPackage(input, name, preferredStability);
                    io.WriteError($"Using version <info>{constraint}</info> for <info>{name}</info>");
                }

                result.Add($"{name} {constraint}");
            }

            return result.ToArray();
        }

        /// <summary>
        /// Normalized the require version pairs.
        /// </summary>
        protected virtual (string Name, string Version)[] NormalizeRequirements(string[] requires)
        {
            var parser = new BVersionParser();
            return parser.ParseNameVersionPairs(requires);
        }

        /// <summary>
        /// Format the require to dictinoary.
        /// </summary>
        protected virtual IDictionary<string, string> FormatRequirements(string[] requirements)
        {
            var requires = new Dictionary<string, string>();
            foreach (var (name, version) in NormalizeRequirements(requirements))
            {
                requires[name] = version;
            }

            return requires;
        }

        /// <summary>
        /// Find the package with name.
        /// </summary>
        protected virtual SearchResult[] FindPackages(string name)
        {
            return GetRepository().Search(name);
        }

        /// <summary>
        /// Get an composite repository include all repositories.
        /// </summary>
        protected virtual IRepository GetRepository()
        {
            if (repository != null)
            {
                return repository;
            }

            repository = new RepositoryComposite(
                Arr.Merge(
                    new[] { new RepositoryPlatform() },
                    RepositoryFactory.CreateDefaultRepository(GetIO())));

            return repository;
        }

        /// <summary>
        /// Given a package name, this determines the best version to use in the require key.
        /// This returns a version with the ~ operator prefixed when possible.
        /// </summary>
        private (string Name, string Version) FindBestVersionAndNameForPackage(
            IInput input, string name, Stabilities preferredStability, string requiredVersion = null, Stabilities? minimumStability = null)
        {
            // find the latest version allowed in this pool.
            var versionSelector = new VersionSelector(GetPool(input, minimumStability));
            var ignorePlatformReqs = input.GetOption("ignore-platform-reqs");

            var package = versionSelector.FindBestPackage(name, requiredVersion, preferredStability);

            if (package == null)
            {
                // platform packages can not be found in the pool in versions
                // other than the local platform's has so if platform reqs are
                // ignored we just take the user's word for it
                if (ignorePlatformReqs && Regex.IsMatch(name, RepositoryPlatform.RegexPlatform))
                {
                    return (name, requiredVersion ?? "*");
                }

                // Check whether the required version was the problem.
                if (!string.IsNullOrEmpty(requiredVersion) &&
                    versionSelector.FindBestPackage(name, null, preferredStability) != null)
                {
                    throw new RuntimeException($"Could not find package {name} in a version matching {requiredVersion}.");
                }

                // Check for similar names/typos
                var similar = FindSimilarPackage(name);
                if (!similar.Empty())
                {
                    // Check whether the minimum stability was the problem
                    // but the package exists.
                    if (requiredVersion == null && Array.Exists(similar, (similarName) => similarName == name))
                    {
                        throw new RuntimeException($"Could not find a version of package {name} matching your minimum-stability ({GetMinimumStability(input)}). Require it with an explicit version constraint allowing its desired stability.");
                    }

                    var similarString = string.Join($"{Environment.NewLine}    ", similar);
                    throw new RuntimeException($"Could not find package {name}.\n\nDid you mean {(similar.Length > 1 ? "one of these" : "this")}?{Environment.NewLine}    {similarString}");
                }

                throw new RuntimeException(
                    $"Could not find a matching version of package {name}. Check the package spelling, your version constraint and that the package is available in a stability which matches your minimum-stability ({GetMinimumStability(input)}).");
            }

            return (package.GetNamePretty(), versionSelector.FindRecommendedRequireVersion(package));
        }

        private Pool GetPool(IInput input, Stabilities? minimumStability = null)
        {
            var key = minimumStability?.ToString() ?? "default";
            if (pools.TryGetValue(key, out Pool pool))
            {
                return pool;
            }

            pools[key] = pool = new Pool(minimumStability ?? GetMinimumStability(input));
            pool.AddRepository(GetRepository());
            return pool;
        }

        private Stabilities GetMinimumStability(IInput input)
        {
            Stabilities GetStabilityFromMember(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return Stabilities.Stable;
                }

                foreach (Stabilities stability in Enum.GetValues(typeof(Stabilities)))
                {
                    var member = stability.GetAttribute<EnumMemberAttribute>();
                    if (member == null)
                    {
                        continue;
                    }

                    if (member.Value.ToLower() == value)
                    {
                        return stability;
                    }
                }

                return Stabilities.Stable;
            }

            if (input.HasOption("stability"))
            {
                return GetStabilityFromMember(input.GetOption("stability"));
            }

            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                var bucket = JsonFile.Parse<ConfigBucket>(content);
                return bucket.MinimumStability ?? Stabilities.Stable;
            }

            return Stabilities.Stable;
        }

        private string[] FindSimilarPackage(string package)
        {
            try
            {
                var results = GetRepository().Search(package);
                var similarPackages = new SortSet<string, int>();
                foreach (var result in results)
                {
                    similarPackages.Add(result.GetName(), Str.Levenshtein(package, result.GetName()));
                }

                return Arr.Slice(similarPackages.ToArray(), 0, 5);
            }
#pragma warning disable CA1031
            catch (SException)
#pragma warning restore CA1031
            {
                return Array.Empty<string>();
            }
        }

        private bool UpdateFileCleanly(JsonFile json, IDictionary<string, string> newRequirements, LinkType requireType, LinkType removeType, bool sortPackages)
        {
            var source = new JsonConfigSource(json);
            foreach (var item in newRequirements)
            {
                var package = item.Key;
                var constraint = item.Value;

                if (!source.AddLink(requireType, package, constraint, sortPackages))
                {
                    return false;
                }

                if (!source.RemoveLink(removeType, package))
                {
                    return false;
                }
            }

            return true;
        }

        private int DoUpdate(IInput input, IOutput output, IIO io, IDictionary<string, string> requirements)
        {
            ResetBucket();
            var bucket = GetBucket(true, input.GetOption("no-plugins"));

            // todo: set no-progress.
            var commandEvent = new CommandEventArgs(PluginEvents.Command, "require", input, output);
            bucket.GetEventDispatcher().Dispatch(this, commandEvent);

            ISet<string> GetRequireWhitlist()
            {
                Guard.Requires<UnexpectedException>(requirements.Count > 0);
                return new HashSet<string>(Arr.Map(requirements, (requirement) => requirement.Key));
            }

            var installer = new BucketInstaller(io, bucket);
            installer
                .SetVerbose(input.GetOption("verbose"))
                .SetPreferSource(input.GetOption("prefer-source"))
                .SetPreferDist(input.GetOption("prefer-dist"))
                .SetDevMode(!input.GetOption("update-no-dev"))
                .SetRunScripts(!input.GetOption("no-scripts"))
                .SetSkipSuggest(input.GetOption("no-suggest"))
                .SetUpdate(true)
                .SetUpdateWhitelist(GetRequireWhitlist())
                .SetWhitelistTransitiveDependencies(input.GetOption("update-with-dependencies"))
                .SetWhitelistAllDependencies(input.GetOption("update-with-all-dependencies"))
                .SetIgnorePlatformRequirements(input.GetOption("ignore-platform-reqs"))
                .SetPreferStable(input.GetOption("prefer-stable"))
                .SetPreferLowest(input.GetOption("prefer-lowest"));

            var status = installer.Run();
            if (status != 0)
            {
                RevertBucketFile(false);
            }

            return status;
        }
    }
}

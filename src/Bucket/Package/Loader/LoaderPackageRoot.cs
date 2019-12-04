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
using Bucket.Exception;
using Bucket.IO;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using BVersionParser = Bucket.Package.Version.VersionParser;

namespace Bucket.Package.Loader
{
    /// <summary>
    /// A loader representing a root package.
    /// </summary>
    public sealed class LoaderPackageRoot : LoaderPackage
    {
        private readonly RepositoryManager manager;
        private readonly Config config;
        private readonly IIO io;
        private readonly IVersionParser versionParser;
        private readonly IDictionary<string, Stabilities> stabilities;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoaderPackageRoot"/> class.
        /// </summary>
        public LoaderPackageRoot(RepositoryManager manager, Config config, IIO io = null, IVersionParser versionParser = null)
        {
            this.manager = manager;
            this.config = config;
            this.io = io ?? IONull.That;
            this.versionParser = versionParser ?? new BVersionParser();

            stabilities = new Dictionary<string, Stabilities>();
            foreach (Stabilities stability in Enum.GetValues(typeof(Stabilities)))
            {
                var member = stability.GetAttribute<EnumMemberAttribute>();
                stabilities[member != null ? member.Value.ToLower() : stability.ToString().ToLower()] = stability;
            }
        }

        /// <inheritdoc />
        public override IPackage Load(ConfigBucketBase config, Type expectedClass)
        {
            if (!typeof(IPackageRoot).IsAssignableFrom(expectedClass))
            {
                throw new ArgumentException($"The type must implement \"{nameof(IPackageRoot)}\".");
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new RuntimeException("The \"name\" property not allowed to be empty.");
            }

            if (string.IsNullOrEmpty(config.Version))
            {
                throw new RuntimeException("The \"version\" property not allowed to be empty.");
            }

            IPackage realPackage, package;
            realPackage = package = base.Load(config, expectedClass);

            if (realPackage is PackageAlias packageAlias)
            {
                realPackage = packageAlias.GetAliasOf();
            }

            if (!(realPackage is PackageRoot packageRoot))
            {
                throw new UnexpectedException($"The package type does not meet expectations and should be: {nameof(PackageRoot)}");
            }

            if (config.MinimumStability != null)
            {
                packageRoot.SetMinimunStability(config.MinimumStability);
            }

            packageRoot.SetPlatforms(config.Platforms);

            var aliases = new List<ConfigAlias>();
            var stabilityFlags = new Dictionary<string, Stabilities>();
            var references = new Dictionary<string, string>();
            var required = new HashSet<string>();
            void ExtractRequire(Link[] links)
            {
                var requires = new Dictionary<string, string>();
                foreach (var link in links)
                {
                    required.Add(link.GetTarget().ToLower());
                    requires[link.GetTarget()] = link.GetConstraint().GetPrettyString();
                }

                ExtractAliases(requires, aliases);
                ExtractStabilityFlags(requires, packageRoot.GetMinimumStability(), stabilityFlags);
                ExtractReferences(requires, references);
            }

            ExtractRequire(realPackage.GetRequires());
            ExtractRequire(realPackage.GetRequiresDev());

            if (required.Contains(config.Name.ToLower()))
            {
                throw new RuntimeException($"Root package \"{config.Name}\" cannot require itself in its bucket.json{Environment.NewLine}Did you accidentally name your root package after an external package?");
            }

            packageRoot.SetAliases(aliases.ToArray());
            packageRoot.SetStabilityFlags(stabilityFlags);
            packageRoot.SetReferences(references);
            packageRoot.SetPreferStable(config.PreferStable);

            var repositories = RepositoryFactory.CreateDefaultRepository(io, this.config, manager);
            foreach (var repository in repositories)
            {
                manager.AddRepository(repository);
            }

            packageRoot.SetRepositories(this.config.GetRepositories());

            return package;
        }

        private void ExtractAliases(IDictionary<string, string> requires, IList<ConfigAlias> collection)
        {
            foreach (var item in requires)
            {
                var require = item.Key;
                var requireVersion = item.Value;

                var match = Regex.Match(requireVersion, @"^(?<version>[^,\s#]+)(?:#[^ ]+)? +as +(?<alias>[^,\s]+)$");
                if (!match.Success)
                {
                    continue;
                }

                collection.Add(new ConfigAlias()
                {
                    Package = require.ToLower(),
                    Version = versionParser.Normalize(match.Groups["version"].Value, requireVersion),
                    Alias = match.Groups["alias"].Value,
                    AliasNormalized = versionParser.Normalize(match.Groups["alias"].Value, requireVersion),
                });
            }
        }

        private void ExtractStabilityFlags(
            IDictionary<string, string> requires,
            Stabilities? minimumStability,
            IDictionary<string, Stabilities> collection)
        {
            foreach (var item in requires)
            {
                var require = item.Key;
                var requireVersion = item.Value;
                var orSplit = Regex.Split(requireVersion.Trim(), @"\s*\|\|?\s*");
                var constraints = new LinkedList<string>();

                // extract all sub-constraints in case it is an OR/AND multi-constraint
                foreach (var orConstraint in orSplit)
                {
                    var andSplit = Regex.Split(orConstraint, @"(?<!^|as|[=>< ,]) *(?<!-)[, ](?!-) *(?!,|as|$)");
                    foreach (var andConstraint in andSplit)
                    {
                        constraints.AddLast(andConstraint);
                    }
                }

                var matchStabilityFlags = false;
                foreach (var constraint in constraints)
                {
                    // todo: stable|RC|beta|alpha|dev need replace to variable.
                    var match = Regex.Match(constraint, "^[^@]*?@(?<stability>stable|RC|beta|alpha|dev)", RegexOptions.IgnoreCase);
                    if (!match.Success || !stabilities.TryGetValue(match.Groups["stability"].Value.ToLower(), out Stabilities stability))
                    {
                        continue;
                    }

                    // Choose the most unstable version.
                    var requireLower = require.ToLower();
                    if (collection.TryGetValue(requireLower, out Stabilities prevStability) && stability < prevStability)
                    {
                        continue;
                    }

                    collection[requireLower] = stability;
                    matchStabilityFlags = true;
                }

                if (matchStabilityFlags)
                {
                    continue;
                }

                foreach (var constraint in constraints)
                {
                    // Used to handle non-standard stability markers
                    // for example: ~1.2.0-stable2
                    requireVersion = Regex.Replace(constraint, @"^([^,\s@]+) as .+$", "${1}");
                    if (!Regex.IsMatch(requireVersion, @"^[^,\s@]+$"))
                    {
                        continue;
                    }

                    // Version speculation only for non-stable versions
                    // Because stable is the default.
                    var stability = VersionParser.ParseStability(requireVersion);
                    if (stability == Stabilities.Stable)
                    {
                        continue;
                    }

                    // Record only stability markers that are less than
                    // the minimum stability and choose the most unstable
                    // version.
                    var requireLower = require.ToLower();
                    if ((collection.TryGetValue(requireLower, out Stabilities prevStability) && stability < prevStability) ||
                        stability < minimumStability)
                    {
                        continue;
                    }

                    collection[requireLower] = stability;
                }
            }
        }

        private void ExtractReferences(IDictionary<string, string> requires, IDictionary<string, string> collection)
        {
            foreach (var item in requires)
            {
                var require = item.Key;
                var requireVersion = item.Value;
                requireVersion = Regex.Replace(requireVersion, @"^([^,\s@]+) as .+$", "${1}");

                // Only use the reference in developer mode.
                var match = Regex.Match(requireVersion, @"^[^,\s@]+?#(?<reference>[a-f0-9]+)$");
                if (!match.Success || VersionParser.ParseStability(requireVersion) != Stabilities.Dev)
                {
                    continue;
                }

                collection[require.ToLower()] = match.Groups["reference"].Value;
            }
        }
    }
}

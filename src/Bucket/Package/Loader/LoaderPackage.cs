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
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Bucket.Util;
using System;
using System.Collections.Generic;
using BVersionParser = Bucket.Package.Version.VersionParser;

namespace Bucket.Package.Loader
{
    /// <summary>
    /// Load package with <see cref="ConfigBucket"/> object.
    /// </summary>
    public class LoaderPackage : ILoaderPackage
    {
        private readonly Dictionary<Type, PackageCreater> packageCreaters;
        private readonly IVersionParser versionParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoaderPackage"/> class.
        /// </summary>
        public LoaderPackage(IVersionParser versionParser = null)
        {
            packageCreaters = new Dictionary<Type, PackageCreater>
            {
                [typeof(IPackage)] = (name, version, versionPretty)
                    => new Package(name, version, versionPretty),
                [typeof(IPackageComplete)] = (name, version, versionPretty)
                    => new PackageComplete(name, version, versionPretty),
                [typeof(IPackageRoot)] = (name, version, versionPretty)
                    => new PackageRoot(name, version, versionPretty),
            };

            this.versionParser = versionParser ?? new BVersionParser();
        }

        private delegate Package PackageCreater(string name, string version, string versionPretty);

        /// <inheritdoc />
        public virtual IPackage Load(ConfigBucketBase config, Type expectedClass)
        {
            var package = CreatePackage(config, expectedClass);
            package = ConfigurePackageLinks(config, package);
            package = ConfigurePackage(config, package);
            package = ConfigurePackageComplete(config, package);

            // todo: Determine the alias package.
            return package;
        }

        internal Link[] ParseLinks(string source, string sourceVersion, string description, IDictionary<string, string> links)
        {
            if (links == null || links.Count <= 0)
            {
                return Array.Empty<Link>();
            }

            var result = new Link[links.Count];
            var i = 0;
            foreach (var item in links)
            {
                result[i++] = CreateLink(source, sourceVersion, description, item.Key, item.Value);
            }

            return result;
        }

        private Package CreatePackage(ConfigBucketBase config, Type type)
        {
            if (string.IsNullOrEmpty(config.VersionNormalized))
            {
                config.VersionNormalized = versionParser.Normalize(config.Version);
            }

            if (!packageCreaters.TryGetValue(type, out PackageCreater creater))
            {
                throw new RuntimeException($"Can not found creater with \"{type}\" type.");
            }

            return creater(config.Name, config.VersionNormalized, config.Version);
        }

        private Package ConfigurePackage(ConfigBucketBase config, Package package)
        {
            package.SetPackageType(config.PackageType);
            package.SetReleaseDate(config.ReleaseDate);

            if (config.Source != null)
            {
                package.SetSourceType(config.Source.Type);
                package.SetSourceUri(config.Source.Uri);
                package.SetSourceReference(config.Source.Reference);
                package.SetSourceMirrors(config.Source.Mirrors);
            }

            if (config.Dist != null)
            {
                package.SetDistType(config.Dist.Type);
                package.SetDistUri(config.Dist.Uri);
                package.SetDistReference(config.Dist.Reference);
                package.SetDistShasum(config.Dist.Shasum);
                package.SetDistMirrors(config.Dist.Mirrors);
            }

            package.SetSuggests(config.Suggests);
            package.SetNotificationUri(config.NotificationUri);
            package.SetArchives(config.Archive);
            package.SetExtra(config.Extra);
            package.SetInstallationSource(config.InstallationSource);
            package.SetBinaries(config.Binaries);

            return package;
        }

        private Package ConfigurePackageComplete(ConfigBucketBase config, Package package)
        {
            if (!(package is PackageComplete packageComplete))
            {
                return package;
            }

            packageComplete.SetDescription(config.Description);
            packageComplete.SetHomepage(config.Homepage);
            packageComplete.SetDeprecated(config.Deprecated);
            packageComplete.SetKeyworkds(config.Keywords);
            packageComplete.SetAuthors(config.Authors);
            packageComplete.SetLicenses(config.Licenses);
            packageComplete.SetSupport(config.Support);
            packageComplete.SetScripts(config.Scripts);

            return packageComplete;
        }

        private Package ConfigurePackageLinks(ConfigBucketBase config, Package package)
        {
            package.SetRequires(
                ParseLinks(config.Name, config.Version, nameof(config.Requires).ToLower(), config.Requires));
            package.SetRequiresDev(
                ParseLinks(config.Name, config.Version, $"{Str.LowerDashes(nameof(config.RequiresDev))} (for development)", config.RequiresDev));
            package.SetProvides(
                ParseLinks(config.Name, config.Version, nameof(config.Provides).ToLower(), config.Provides));
            package.SetReplaces(
                ParseLinks(config.Name, config.Version, nameof(config.Replaces).ToLower(), config.Replaces));
            package.SetConflicts(
                ParseLinks(config.Name, config.Version, nameof(config.Conflicts).ToLower(), config.Conflicts));

            return package;
        }

        private Link CreateLink(string source, string sourceVersion, string description, string target, string prettyConstraint)
        {
            if (string.IsNullOrEmpty(prettyConstraint))
            {
                throw new ArgumentException($"Link constraint in {source} {description} > {target} should not be a null or empty.");
            }

            IConstraint parsedConstraint;
            if (prettyConstraint == BasePackage.SelfVersion)
            {
                parsedConstraint = versionParser.ParseConstraints(sourceVersion);
            }
            else
            {
                parsedConstraint = versionParser.ParseConstraints(prettyConstraint);
            }

            return new Link(source, target, parsedConstraint, description, prettyConstraint);
        }
    }
}

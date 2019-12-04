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
using Bucket.Util;
using System;
using System.Collections.Generic;

namespace Bucket.Package.Dumper
{
    /// <summary>
    /// Represents a package dumper.
    /// </summary>
    public class DumperPackage
    {
        /// <summary>
        /// Dump the package instance to <see cref="ConfigBucket"/>.
        /// </summary>
        /// <typeparam name="T">The type of the config struct.</typeparam>
        /// <param name="package">The package instance.</param>
        /// <returns>The <see cref="ConfigBucket"/> instance.</returns>
        public T Dump<T>(IPackage package)
            where T : ConfigBucketBase, new()
        {
            var data = new T
            {
                Name = package.GetNamePretty(),
                Version = package.GetVersionPretty(),
                VersionNormalized = package.GetVersion(),
                ReleaseDate = package.GetReleaseDate(),
                PackageType = package.GetPackageType(),
                NotificationUri = package.GetNotificationUri(),
                Binaries = package.GetBinaries().ZeroAsNull(),
                Archive = package.GetArchives().ZeroAsNull(),
                Extra = package.GetExtra(),
                InstallationSource = package.GetInstallationSource(),
            };

            if (!string.IsNullOrEmpty(package.GetSourceType()))
            {
                data.Source = new ConfigResource
                {
                    Type = package.GetSourceType(),
                    Uri = package.GetSourceUri(),
                    Reference = package.GetSourceReference(),
                    Mirrors = package.GetSourceMirrors().ZeroAsNull(),
                };
            }

            if (!string.IsNullOrEmpty(package.GetDistType()))
            {
                data.Dist = new ConfigResource
                {
                    Type = package.GetDistType(),
                    Uri = package.GetDistUri(),
                    Reference = package.GetDistReference(),
                    Mirrors = package.GetDistMirrors().ZeroAsNull(),
                    Shasum = package.GetDistShasum(),
                };
            }

            data.Requires = LinksToDictonary(package.GetRequires());
            data.RequiresDev = LinksToDictonary(package.GetRequiresDev());
            data.Conflicts = LinksToDictonary(package.GetConflicts());
            data.Provides = LinksToDictonary(package.GetProvides());
            data.Replaces = LinksToDictonary(package.GetReplaces());

            var suggests = package.GetSuggests();
            if (suggests != null && suggests.Count > 0)
            {
                data.Suggests = new SortedDictionary<string, string>(suggests);
            }

            if (package is IPackageComplete packageComplete)
            {
                data.Licenses = packageComplete.GetLicenses().ZeroAsNull();
                data.Authors = packageComplete.GetAuthors().ZeroAsNull();
                data.Description = packageComplete.GetDescription();
                data.Homepage = packageComplete.GetHomepage();
                data.Keywords = packageComplete.GetKeywords().ZeroAsNull();
                data.Repositories = packageComplete.GetRepositories().ZeroAsNull();

                if (packageComplete.IsDeprecated)
                {
                    var replacement = packageComplete.GetReplacementPackage();
                    if (string.IsNullOrEmpty(replacement))
                    {
                        data.Deprecated = true;
                    }
                    else
                    {
                        data.Deprecated = replacement;
                    }
                }

                data.Scripts = packageComplete.GetScripts();

                var supports = packageComplete.GetSupport();
                if (supports != null && supports.Count > 0)
                {
                    data.Support = new SortedDictionary<string, string>(supports);
                }
            }

            if (package is IPackageRoot packageRoot)
            {
                data.MinimumStability = packageRoot.GetMinimumStability();
                data.Platforms = packageRoot.GetPlatforms();
            }

            if (data.Keywords != null)
            {
                Array.Sort(data.Keywords);
            }

            return data;
        }

        /// <summary>
        /// Convert the link to a dictionary.
        /// </summary>
        /// <remarks>Return null if the links elements is zero.</remarks>
        /// <param name="links">An array of links.</param>
        protected virtual IDictionary<string, string> LinksToDictonary(Link[] links)
        {
            if (links == null || links.Length == 0)
            {
#pragma warning disable S1168
                return null;
#pragma warning restore S1168
            }

            var collection = new SortedDictionary<string, string>();
            foreach (var link in links)
            {
                collection[link.GetTarget()] = link.GetPrettyConstraint();
            }

            return collection;
        }
    }
}

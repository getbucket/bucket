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

using Bucket.DependencyResolver;
using Bucket.Semver;
using Bucket.Util;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Bucket.Package.Version
{
    /// <summary>
    /// Selects the best possible version for a package.
    /// </summary>
    public class VersionSelector
    {
        private readonly Pool pool;
        private readonly VersionParser parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionSelector"/> class.
        /// </summary>
        public VersionSelector(Pool pool)
        {
            this.pool = pool;
            parser = new VersionParser();
        }

        /// <summary>
        /// Given a package name and optional version, returns the
        /// latest PackageInterface that matches.
        /// </summary>
        public virtual IPackage FindBestPackage(string packageName, string targetPackageVersion = null, Stabilities? preferredStability = null)
        {
            preferredStability = preferredStability ?? Stabilities.Stable;
            var constraint = !string.IsNullOrEmpty(targetPackageVersion) ? parser.ParseConstraints(targetPackageVersion) : null;
            var candidates = pool.WhatProvides(packageName.ToLower(), constraint, true);

            if (candidates.Empty())
            {
                return null;
            }

            // select highest version if we have many
            var package = candidates[0];
            var minStability = preferredStability.Value;
            foreach (var candidate in candidates)
            {
                var candidateStability = candidate.GetStability();
                var currentStability = package.GetStability();

                // candidate is less stable than our preferred stability,
                // and current package is more stable than candidate, skip it
                if (minStability < candidateStability && currentStability < candidateStability)
                {
                    continue;
                }

                // candidate is less stable than our preferred stability,
                // and current package is less stable than candidate, select candidate
                if (minStability < candidateStability && candidateStability < currentStability)
                {
                    package = candidate;
                    continue;
                }

                // candidate is more stable than our preferred stability,
                // and current package is less stable than preferred stability, select candidate
                if (minStability >= candidateStability && minStability < currentStability)
                {
                    package = candidate;
                    continue;
                }

                // select highest version of the two
                if (Comparator.LessThan(package.GetVersion(), candidate.GetVersion()))
                {
                    package = candidate;
                }
            }

            return package;
        }

        /// <summary>
        /// Given a concrete package, this returns a ~ constraint (when possible)
        /// that should be used, for example, in bucket.json.
        /// </summary>
        public virtual string FindRecommendedRequireVersion(IPackage package)
        {
            // For example:
            //  * 1.2.1         -> ^1.2
            //  * 1.2           -> ^1.2
            //  * v3.2.1        -> ^3.2
            //  * 2.0-beta.1    -> ^2.0@beta
            //  * dev-master    -> ^2.1@dev      (dev version with alias)
            //  * dev-master    -> dev-master    (dev versions are untouched)
            var version = package.GetVersion();
            if (!package.IsDev)
            {
                return TransformVersion(version, package.GetVersionPretty(), package.GetStability());
            }

            // todo: add a branch alias.
            return package.GetVersionPretty();
        }

        private static string TransformVersion(string version, string versionPretty, Stabilities stability)
        {
            // attempt to transform 2.1.1 to 2.1
            // this allows you to upgrade through minor versions
            var semanticVersionParts = version.Split('.');

            // check to see if we have a semver-looking version
            if (semanticVersionParts.Length != 4 || !Regex.IsMatch(semanticVersionParts[3], @"^0\D?"))
            {
                return versionPretty;
            }
            else
            {
                // remove the last parts (i.e. the patch version number and any extra)
                if (semanticVersionParts[0] == "0")
                {
                    Arr.RemoveAt(ref semanticVersionParts, 3);
                }
                else
                {
                    Arr.RemoveAt(ref semanticVersionParts, 3);
                    Arr.RemoveAt(ref semanticVersionParts, 2);
                }

                version = string.Join(".", semanticVersionParts);
            }

            if (stability != Stabilities.Stable)
            {
                var member = stability.GetAttribute<EnumMemberAttribute>();
                version += $"@{member.Value}";
            }

            return $"^{version}";
        }
    }
}

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

using Bucket.Repository;
using Bucket.Util;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SemverVersionParser = Bucket.Semver.VersionParser;

namespace Bucket.Package.Version
{
    /// <summary>
    /// Semver version parser implementation with bucket project.
    /// </summary>
    public class VersionParser : SemverVersionParser
    {
        /// <summary>
        /// Whether the package action is upgrade.
        /// </summary>
        /// <param name="normalizedFrom">The from normalized version.</param>
        /// <param name="normalizedTo">The to normalized version.</param>
        /// <returns>True if the package action is upgrade.</returns>
        public static bool IsUpgrade(string normalizedFrom, string normalizedTo)
        {
            if (string.IsNullOrEmpty(normalizedFrom) || string.IsNullOrEmpty(normalizedTo))
            {
                return false;
            }

            if ((normalizedFrom.Length >= 4 && normalizedFrom.Substring(0, 4) == "dev-") ||
                (normalizedTo.Length >= 4 && normalizedTo.Substring(0, 4) == "dev-"))
            {
                return true;
            }

            var sorted = Semver.Semver.Sort(new[] { normalizedTo, normalizedFrom });

            return sorted[0] == normalizedFrom;
        }

        /// <summary>
        /// Parses an array of strings representing package/version pairs.
        /// </summary>
        /// <remarks>
        /// The parsing results in an array of arrays, each of which contain
        /// a 'name' key with value and optionally a 'version' key with value.
        /// </remarks>
        /// <param name="pairs">An array of package/version pairs separated by @ :.</param>
        public virtual (string Name, string Version)[] ParseNameVersionPairs(string[] pairs)
        {
            var result = new List<(string Name, string Version)>();
            for (var i = 0; i < pairs.Length; i++)
            {
                var pair = Regex.Replace(pairs[i], "^([^@:]+)[@:](.*)$", "${1} ${2}");

                // Compatible with version splits represented by spaces.
                if (!pair.Contains(Str.Space) &&
                        (i + 1) < pairs.Length &&
                        !pairs[i + 1].Contains("/") &&
                        !Regex.IsMatch(pairs[i + 1], RepositoryPlatform.RegexPlatform))
                {
                    pair += Str.Space + pairs[i + 1];
                    i++;
                }

                if (pair.Contains(Str.Space))
                {
                    var segment = pair.Split(' ');
                    result.Add((segment[0], segment[1]));
                }
                else
                {
                    result.Add((pair, null));
                }
            }

            return result.ToArray();
        }
    }
}

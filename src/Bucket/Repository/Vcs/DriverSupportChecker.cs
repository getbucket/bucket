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
using Bucket.IO;
using System.Collections.Generic;

namespace Bucket.Repository.Vcs
{
    /// <summary>
    /// Represents a driver support checker.
    /// </summary>
    public static class DriverSupportChecker
    {
        private static IDictionary<string, ShouldSupport> checker
            = new Dictionary<string, ShouldSupport>();

        /// <summary>
        /// Whether the driver support the specified uri.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The config instance.</param>
        /// <param name="uri">Thr specified uri.</param>
        /// <param name="deep">Whether is deep checker.</param>
        /// <returns>True if the driver is supported.</returns>
        public delegate bool ShouldSupport(IIO io, Config config, string uri, bool deep = false);

        /// <summary>
        /// Register an inspector to check if the driver with the specified name is supported.
        /// </summary>
        /// <param name="name">The driver name.</param>
        /// <param name="checker">The checker.</param>
        public static void RegisterChecker(string name, ShouldSupport checker)
        {
            DriverSupportChecker.checker.Add(name, checker);
        }

        /// <summary>
        /// Check if the specified driver is supported.
        /// </summary>
        /// <param name="name">The checked driver name.</param>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The global config instance.</param>
        /// <param name="uri">The vcs uri.</param>
        /// <param name="deep">Whether to conduct an in-depth inspection.</param>
        /// <returns>True if supported.</returns>
        public static bool IsSupport(string name, IIO io, Config config, string uri, bool deep = false)
        {
            if (DriverSupportChecker.checker.TryGetValue(name, out ShouldSupport checker))
            {
                return checker(io, config, uri, deep);
            }

            return false;
        }
    }
}

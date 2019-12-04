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

namespace Bucket.DependencyResolver.Rules
{
    /// <summary>
    /// Represents the rule type.
    /// </summary>
    public enum RuleType
    {
        /// <summary>
        /// The package rule.
        /// </summary>
        Package = 0,

        /// <summary>
        /// The job rule.
        /// </summary>
        Job = 1,

        /// <summary>
        /// Represents a learning rule that will help analyze the most appropriate package when a conflict occurs.
        /// </summary>
        Learned = 4,
    }
}

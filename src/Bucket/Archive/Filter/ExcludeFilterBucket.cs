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

using System.Collections.Generic;

namespace Bucket.Archive.Filter
{
    /// <summary>
    /// A default filter representing a bucket rule.
    /// </summary>
    public class ExcludeFilterBucket : BaseExcludeFilter
    {
        private readonly string[] excludeRules;
        private IEnumerable<FilterPattern> excludePatterns;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeFilterBucket"/> class.
        /// </summary>
        /// <param name="excludeRules">An array of exclude rules from bucket.json.</param>
        public ExcludeFilterBucket(string[] excludeRules)
        {
            this.excludeRules = excludeRules;
        }

        /// <inheritdoc />
        protected override IEnumerable<FilterPattern> GetExcludePatterns()
        {
            if (excludePatterns == null)
            {
                excludePatterns = GeneratePatterns(excludeRules);
            }

            return excludePatterns;
        }
    }
}

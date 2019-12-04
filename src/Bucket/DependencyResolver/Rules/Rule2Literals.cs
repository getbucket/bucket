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

using Bucket.Package;

namespace Bucket.DependencyResolver.Rules
{
    /// <summary>
    /// Represents 2 literals.
    /// </summary>
    internal sealed class Rule2Literals : RuleGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rule2Literals"/> class.
        /// </summary>
        /// <param name="literal1">The literal 1.</param>
        /// <param name="literal2">The literal 2.</param>
        /// <param name="reason">The <see cref="Reason"/> describing the reason for generating this rule.</param>
        /// <param name="reasonData">The data of the reason, maybe <see cref="IPackage"/> or <see cref="Link"/>.</param>
        /// <param name="job">The job this rule was created from.</param>
        public Rule2Literals(int literal1, int literal2, Reason reason, object reasonData, Job job = null)
            : base(new[] { literal1, literal2 }, reason, reasonData, job)
        {
        }
    }
}

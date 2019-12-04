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

using System;
using System.Linq;
using System.Text;

namespace Bucket.DependencyResolver.Rules
{
    /// <summary>
    /// Represents a general rule.
    /// </summary>
    internal class RuleGeneric : Rule
    {
        private readonly int[] literals;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleGeneric"/> class.
        /// </summary>
        /// <param name="literals">All packages literals.</param>
        /// <param name="reason">The <see cref="Reason"/> describing the reason for generating this rule.</param>
        /// <param name="reasonData">Any data of the reason.</param>
        /// <param name="job">The job this rule was created from.</param>
        public RuleGeneric(int[] literals, Reason reason, object reasonData, Job job = null)
            : base(reason, reasonData, job)
        {
            Array.Sort(literals);
            this.literals = literals;
        }

        /// <inheritdoc />
        public override bool IsAssertion => literals.Length == 1;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is Rule rule))
            {
                return false;
            }

            return Enumerable.SequenceEqual(literals, rule.GetLiterals());
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return string.Join(", ", literals).GetHashCode();
        }

        /// <inheritdoc />
        public override int[] GetLiterals()
        {
            return literals;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append(!Enable ? "disabled(" : "(");
            var first = true;
            foreach (var literal in literals)
            {
                if (!first)
                {
                    result.Append("|");
                }

                result.Append(literal);
                first = false;
            }

            result.Append(")");
            return result.ToString();
        }
    }
}

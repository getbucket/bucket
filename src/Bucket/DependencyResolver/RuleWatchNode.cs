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

using Bucket.DependencyResolver.Rules;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// Wrapper around a <see cref="Rule"/> which keeps track of the two literals it watches.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="RuleWatchGraph"/> to store rules in two RuleWatchChains.
    /// </remarks>
    internal sealed class RuleWatchNode
    {
        private readonly Rule rule;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleWatchNode"/> class.
        /// </summary>
        /// <param name="rule">The rule to wrap.</param>
        public RuleWatchNode(Rule rule)
        {
            this.rule = rule;

            var literals = rule.GetLiterals();
            Watch1 = literals.Length > 0 ? literals[0] : 0;
            Watch2 = literals.Length > 1 ? literals[1] : 0;
        }

        public int Watch1 { get; private set; }

        public int Watch2 { get; private set; }

        public static implicit operator RuleWatchNode(Rule rule)
        {
            return new RuleWatchNode(rule);
        }

        /// <summary>
        /// Places the second watch on the rule's literal, decided at the highest level.
        /// </summary>
        /// <remarks>
        /// Useful for learned rules where the literal for the highest rule is most
        /// likely to quickly lead to further decisions.
        /// </remarks>
        /// <param name="decisions">The decisions made so far by the solver.</param>
        public void Watch2OnHighest(Decisions decisions)
        {
            var literals = rule.GetLiterals();

            // if there are only 2 elements, both are being watched anyway
            if (literals.Length < 3)
            {
                return;
            }

            var watchLevel = 0;
            foreach (var literal in literals)
            {
                var level = decisions.GetDecisionLevel(literal);
                if (level > watchLevel)
                {
                    Watch2 = literal;
                    watchLevel = level;
                }
            }
        }

        /// <summary>
        /// Given one watched literal, this method returns the other watched literal.
        /// </summary>
        /// <param name="literal">The watched literal that should not be returned.</param>
        public int GetOtherWatch(int literal)
        {
            if (Watch1 == literal)
            {
                return Watch2;
            }

            return Watch1;
        }

        /// <summary>
        /// Moves a watch from one literal to another.
        /// </summary>
        /// <param name="from">The previously watched literal.</param>
        /// <param name="to">The literal to be watched now.</param>
        public void MoveWatch(int from, int to)
        {
            if (Watch1 == from)
            {
                Watch1 = to;
            }
            else
            {
                Watch2 = to;
            }
        }

        /// <summary>
        /// Gets the rule this node wraps.
        /// </summary>
        /// <returns>Returns the rule this node wraps.</returns>
        public Rule GetRule()
        {
            return rule;
        }
    }
}

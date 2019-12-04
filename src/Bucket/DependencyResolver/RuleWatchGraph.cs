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
using Bucket.Util;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// The <see cref="RuleWatchGraph"/> efficiently propagates decisions to other rules.
    /// </summary>
    /// <remarks>
    /// All rules generated for solving a SAT problem should be inserted into the
    /// graph. When a decision on a literal is made, the graph can be used to
    /// propagate the decision to all other rules involving the literal, leading to
    /// other trivial decisions resulting from unit clauses.
    /// </remarks>
    // https://doc.opensuse.org/projects/satsolver/11.2/
    // https://research.swtch.com/version-sat
    internal sealed class RuleWatchGraph
    {
        private readonly IDictionary<int, LinkedList<RuleWatchNode>> watchChains;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleWatchGraph"/> class.
        /// </summary>
        public RuleWatchGraph()
        {
            watchChains = new Dictionary<int, LinkedList<RuleWatchNode>>();
        }

        /// <summary>
        /// Add a rule node into the appropriate chains within the graph.
        /// </summary>
        /// <remarks>
        /// The node is prepended to the watch chains for each of the two literals it
        /// watches.
        /// Assertions are skipped because they only require on a single package and
        /// have no alternative literal that could be true, so there is no need to
        /// watch changes in any literals.
        /// </remarks>
        /// <param name="node">The rule node to be inserted into the graph.</param>
        public void Add(RuleWatchNode node)
        {
            if (node.GetRule().IsAssertion)
            {
                return;
            }

            foreach (var literal in new[] { node.Watch1, node.Watch2 })
            {
                if (!watchChains.TryGetValue(literal, out LinkedList<RuleWatchNode> watchChainNode))
                {
                    watchChains[literal] = watchChainNode = new LinkedList<RuleWatchNode>();
                }

                watchChainNode.AddFirst(node);
            }
        }

        /// <summary>
        /// Propagates a decision on a literal to all rules watching the literal.
        /// </summary>
        /// <remarks>
        /// If a decision, e.g. +A has been made, then all rules containing -A, e.g.
        /// (-A|+B|+C) now need to satisfy at least one of the other literals, so
        /// that the rule as a whole becomes true, since with +A applied the rule
        /// is now (false|+B|+C) so essentially (+B|+C).
        /// This means that all rules watching the literal -A need to be updated to
        /// watch 2 other literals which can still be satisfied instead. So literals
        /// that conflict with previously made decisions are not an option.
        /// Alternatively it can occur that a unit clause results: e.g. if in the
        /// above example the rule was (-A|+B), then A turning true means that
        /// B must now be decided true as well.
        /// </remarks>
        /// <param name="decidedLiteral">The literal which was decided.</param>
        /// <param name="level">The level at which the decision took place and at which all resulting decisions should be made.</param>
        /// <param name="decisions">Used to check previous decisions and to register decisions resulting from propagation.</param>
        /// <returns>If a conflict is found the conflicting rule is returned.</returns>
        public Rule PropagateLiteral(int decidedLiteral, int level, Decisions decisions)
        {
            // we invert the decided literal here, example:
            // A was decided => (-A|B) now requires B to be true, so we look for
            // rules which are fulfilled by -A, rather than A.
            // This means finding out the conflicts or requires.
            var literal = -decidedLiteral;

            if (!watchChains.TryGetValue(literal, out LinkedList<RuleWatchNode> chain))
            {
                return null;
            }

            foreach (var node in chain.ToArray())
            {
                var otherWatch = node.GetOtherWatch(literal);
                if (!node.GetRule().Enable || decisions.IsSatisfy(otherWatch))
                {
                    continue;
                }

                var ruleLiterals = node.GetRule().GetLiterals();
                var alternativeLiterals = Arr.Filter(ruleLiterals, (ruleLiteral) =>
                {
                    // Guaranteed selection decision is not at the same time
                    // as Watch1 and Watch2, guaranteeing no conflict.
                    return literal != ruleLiteral && otherWatch != ruleLiteral && !decisions.IsConflict(ruleLiteral);
                });

                if (alternativeLiterals.Length > 0)
                {
                    var toLiteral = alternativeLiterals[0];

                    if (!watchChains.TryGetValue(toLiteral, out LinkedList<RuleWatchNode> toChain))
                    {
                        watchChains[toLiteral] = toChain = new LinkedList<RuleWatchNode>();
                    }

                    node.MoveWatch(literal, toLiteral);
                    chain.Remove(node);
                    toChain.AddFirst(node);
                    continue;
                }

                if (decisions.IsConflict(otherWatch))
                {
                    return node.GetRule();
                }

                decisions.Decide(otherWatch, level, node.GetRule());
            }

            return null;
        }
    }
}

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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// Stores decisions on installing, uninstall or keeping packages.
    /// </summary>
    internal sealed class Decisions : IEnumerable<(int Literal, Rule Reason)>
    {
        private readonly Pool pool;
        private readonly IDictionary<int, int> decisions;
        private readonly List<Decision> decisionsList;

        /// <summary>
        /// Initializes a new instance of the <see cref="Decisions"/> class.
        /// </summary>
        /// <param name="pool">The pool contains repositories that provide packages.</param>
        public Decisions(Pool pool)
        {
            this.pool = pool;
            decisions = new Dictionary<int, int>();
            decisionsList = new List<Decision>();
        }

        public int Count => decisionsList.Count;

        public (int Literal, Rule Reason) this[int position]
        {
            get => At(position);
        }

        public void Decide(int literal, int level, Rule reason)
        {
            AddDecision(literal, level);
            decisionsList.Add(new Decision()
            {
                Literal = literal,
                Reason = reason,
            });
        }

        /// <summary>
        /// Whether the literal is satisfy decision.
        /// </summary>
        public bool IsSatisfy(int literal)
        {
            var packageId = Math.Abs(literal);

            if (!decisions.TryGetValue(packageId, out int decision))
            {
                return false;
            }

            return (literal > 0 && decision > 0) || (literal < 0 && decision < 0);
        }

        /// <summary>
        /// Whether the literal is conflict.
        /// </summary>
        public bool IsConflict(int literal)
        {
            var packageId = Math.Abs(literal);

            if (!decisions.TryGetValue(packageId, out int decision))
            {
                return false;
            }

            return (literal < 0 && decision > 0) || (literal > 0 && decision < 0);
        }

        /// <summary>
        /// Whether the literal or package is decided.
        /// </summary>
        public bool IsDecided(int literalOrPackageId)
        {
            var packageId = Math.Abs(literalOrPackageId);

            if (!decisions.TryGetValue(packageId, out int decided))
            {
                return false;
            }

            return decided != 0;
        }

        public bool IsUndecided(int literalOrPackageId)
        {
            return !IsDecided(literalOrPackageId);
        }

        public bool IsDecidedInstall(int literalOrPackageId)
        {
            var packageId = Math.Abs(literalOrPackageId);

            if (!decisions.TryGetValue(packageId, out int decision))
            {
                return false;
            }

            return decision > 0;
        }

        public int GetDecisionLevel(int literalOrPackageId)
        {
            var packageId = Math.Abs(literalOrPackageId);

            if (decisions.TryGetValue(packageId, out int decision))
            {
                return Math.Abs(decision);
            }

            return 0;
        }

        public Rule GetDecisionReason(int literalOrPackageId)
        {
            var packageId = Math.Abs(literalOrPackageId);

            foreach (var decision in decisionsList)
            {
                if (packageId == Math.Abs(decision.Literal))
                {
                    return decision.Reason;
                }
            }

            return null;
        }

        public (int Literal, Rule Reason) At(int position)
        {
            var decision = decisionsList[position];
            return (decision.Literal, decision.Reason);
        }

        public bool ContainsAt(int position)
        {
            return position >= 0 && position < decisionsList.Count;
        }

        public Rule GetLastReason()
        {
            return At(decisionsList.Count - 1).Reason;
        }

        public int GetLastLiteral()
        {
            return At(decisionsList.Count - 1).Literal;
        }

        public void Revert()
        {
            foreach (var decision in decisionsList)
            {
                decisions[Math.Abs(decision.Literal)] = 0;
            }

            decisionsList.Clear();
        }

        public void RevertToPosition(int position)
        {
            position += 1;
            position = Math.Max(position, 0);
            if (position >= decisions.Count)
            {
                return;
            }

            var range = decisionsList.GetRange(position, decisions.Count - position);
            foreach (var decision in range)
            {
                decisions[Math.Abs(decision.Literal)] = 0;
            }

            decisionsList.RemoveRange(position, decisions.Count - position);
        }

        public void RevertLast()
        {
            decisions[Math.Abs(GetLastLiteral())] = 0;
            decisionsList.RemoveAt(decisionsList.Count - 1);
        }

        public override string ToString()
        {
            var decisionsSorted = decisions.OrderBy(item => item.Key);
            var result = new StringBuilder();
            result.Append('[');

            foreach (var decision in decisionsSorted)
            {
                result.Append(decision.Key).Append(':').Append(decision.Value).Append(',');
            }

            return (result.Length > 1 ? result.ToString(0, result.Length - 1) : result.ToString()) + "]";
        }

        public IEnumerator<(int Literal, Rule Reason)> GetEnumerator()
        {
            for (var i = decisionsList.Count - 1; i >= 0; i--)
            {
                var decision = decisionsList[i];
                yield return (decision.Literal, decision.Reason);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void AddDecision(int literal, int level)
        {
            var packageId = Math.Abs(literal);

            if (decisions.TryGetValue(packageId, out int previousDecision) && previousDecision != 0)
            {
                var literalString = pool.LiteralToPrettyString(literal, null);
                var package = pool.GetPackageByLiteral(literal);

                throw new SolverBugException(
                    $"Trying to decide {literalString} on level {level}, even though {package} was previously decided as {previousDecision}.");
            }

            if (literal > 0)
            {
                decisions[packageId] = level;
            }
            else
            {
                decisions[packageId] = -level;
            }
        }

        private struct Decision
        {
            public int Literal { get; set; }

            public Rule Reason { get; set; }
        }
    }
}

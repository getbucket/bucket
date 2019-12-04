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

using Bucket.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bucket.DependencyResolver.Rules
{
    internal sealed class RuleSet : IEnumerable<Rule>
    {
        private readonly Dictionary<RuleType, List<Rule>> rules;
        private readonly List<Rule> rulesById;
        private readonly HashSet<Rule> rulesByHash;
        private readonly RuleType[] ruleTypes;
        private readonly int minWidth;

        public RuleSet()
        {
            rules = new Dictionary<RuleType, List<Rule>>();
            rulesById = new List<Rule>();
            rulesByHash = new HashSet<Rule>();
            ruleTypes = Enum.GetValues(typeof(RuleType)).Cast<RuleType>().ToArray();

            foreach (var ruleType in ruleTypes)
            {
                rules[ruleType] = new List<Rule>();
                minWidth = Math.Max(minWidth, ruleType.ToString().Length);
            }
        }

        public int Count { get; private set; } = 0;

        public Rule this[int id]
        {
            get => GetRuleById(id);
        }

        /// <summary>
        /// Add rules to the list of rules of the specified rule type.
        /// Returns false if the rule exists in any type of rule list.
        /// </summary>
        /// <param name="rule">The rule instance.</param>
        /// <param name="ruleType">The specified rule type.</param>
        /// <returns>Returns false if the rule exists in any type of rule list.</returns>
        public bool Add(Rule rule, RuleType ruleType)
        {
            if (rule == null || rulesByHash.Contains(rule))
            {
                return false;
            }

            rules[ruleType].Add(rule);
            rulesById.Add(rule);
            rulesByHash.Add(rule);
            rule.SetRuleType(ruleType);

            Count++;

            return true;
        }

        public Rule GetRuleById(int id)
        {
            return rulesById[id];
        }

        public IEnumerator<Rule> GetEnumerator()
        {
            return new RuleSetIterator(this, ruleTypes);
        }

        public IEnumerable<Rule> GetEnumeratorFor(params RuleType[] iteratorTypes)
        {
            iteratorTypes = iteratorTypes ?? Array.Empty<RuleType>();
            iteratorTypes = iteratorTypes.Distinct().ToArray();

            return new RuleSetIterator(this, iteratorTypes);
        }

        public IEnumerable<Rule> GetEnumeratorWithout(params RuleType[] iteratorTypes)
        {
            iteratorTypes = iteratorTypes ?? Array.Empty<RuleType>();
            iteratorTypes = ruleTypes.Except(iteratorTypes.Distinct()).ToArray();

            return new RuleSetIterator(this, iteratorTypes);
        }

        public string GetPrettyString(Pool pool = null)
        {
            var result = new StringBuilder();
            result.Append(Environment.NewLine);
            foreach (var item in rules)
            {
                result.Append(Str.Pad(minWidth + 1, item.Key.ToString()));
                result.Append(": ");
                foreach (var rule in item.Value)
                {
                    result.Append(pool != null ? rule.GetPrettyString(pool) : rule.ToString());
                    result.Append(Environment.NewLine);
                }

                result.Append(Environment.NewLine);
                result.Append(Environment.NewLine);
            }

            return result.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class RuleSetIterator : IEnumerator<Rule>, IEnumerable<Rule>
        {
            private readonly RuleSet ruleSet;
            private readonly RuleType[] iteratorTypes;
            private int ruleIndex;
            private int typeIndex;

            public RuleSetIterator(RuleSet ruleSet, RuleType[] iteratorTypes)
            {
                this.ruleSet = ruleSet;
                this.iteratorTypes = iteratorTypes;
                Reset();
            }

            public Rule Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // ignore.
            }

            public IEnumerator<Rule> GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
            start:
                if (typeIndex >= iteratorTypes.Length)
                {
                    return false;
                }

                var rules = ruleSet.rules[iteratorTypes[typeIndex]];

                if (ruleIndex >= rules.Count)
                {
                    ruleIndex = 0;
                    typeIndex++;
                    goto start;
                }

                Current = rules[ruleIndex];
                ruleIndex++;
                return true;
            }

            public void Reset()
            {
                ruleIndex = 0;
                typeIndex = 0;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

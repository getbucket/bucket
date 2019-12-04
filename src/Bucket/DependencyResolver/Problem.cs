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
using Bucket.Package;
using Bucket.Semver.Constraint;
using Bucket.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// Represents a problem detected while solving requires.
    /// </summary>
    internal sealed class Problem : IEnumerable<Rule>
    {
        private readonly Pool pool;
        private readonly LinkedList<List<Rule>> reasons;
        private readonly HashSet<Rule> reasonsSeen;

        /// <summary>
        /// Initializes a new instance of the <see cref="Problem"/> class.
        /// </summary>
        /// <param name="pool">The pool instance.</param>
        public Problem(Pool pool)
        {
            this.pool = pool;
            reasons = new LinkedList<List<Rule>>();
            reasonsSeen = new HashSet<Rule>(new EqualityComparer());
            NextSection();
        }

        /// <summary>
        /// Add a rule as a reason.
        /// </summary>
        /// <param name="rule"> A rule which is a reason for this problem.</param>
        public void AddRule(Rule rule)
        {
            if (reasonsSeen.Add(rule))
            {
                reasons.Last.Value.Add(rule);
            }
        }

        /// <summary>
        /// Gets all reasons for this problem.
        /// </summary>
        /// <returns>Returns all reasons for this problem.</returns>
        public Rule[] GetReasons()
        {
            return new ProblemIterator(this).ToArray();
        }

        /// <summary>
        /// Move the cause of the problem to the next section.
        /// reasons for the problem, each is a rule or a job and a rule.
        /// </summary>
        public void NextSection()
        {
            reasons.AddLast(new List<Rule>());
        }

        /// <summary>
        /// A human readable textual representation of the problem's reasons.
        /// </summary>
        /// <param name="installedMap">A map of all installed packages.</param>
        public string GetPrettyString(IDictionary<int, IPackage> installedMap = null)
        {
            var prefix = $"{Environment.NewLine}    - ";

            var rules = new ProblemIterator(this).ToArray();
            if (rules.Length == 1)
            {
                var rule = rules[0];
                var job = rule.GetJob();
                var packageName = job?.PackageName;
                var constraint = job?.Constraint;
                var packages = Array.Empty<IPackage>();

                if (constraint != null)
                {
                    packages = pool.WhatProvides(packageName, constraint);
                }

                if (job == null || job.Command != JobCommand.Install || packages.Length > 0)
                {
                    goto general_analysis;
                }

                if (!Regex.IsMatch(packageName, $"^{Factory.RegexPackageNameIllegal}$"))
                {
                    var illegalChars = Regex.Replace(packageName, Factory.RegexPackageNameIllegal, string.Empty);
                    return $"{prefix}The requested package {packageName} could not be found, it looks like its name is invalid, \"{illegalChars}\" is not allowed in package names.";
                }

                var providers = pool.WhatProvides(packageName, constraint, true, true);
                if (providers.Length > 0)
                {
                    return $"{prefix}The requested package {packageName}{ConstraintToText(constraint)} is satisfiable by {Rule.FormatPackagesUnique(providers)} but these conflict with your requirements or minimum-stability.";
                }

                providers = pool.WhatProvides(packageName, null, true, true);
                if (providers.Length > 0)
                {
                    return $"{prefix}The requested package {packageName}{ConstraintToText(constraint)} exists as {Rule.FormatPackagesUnique(providers)} but these are rejected by your constraint.";
                }

                return $"{prefix}The requested package {packageName} could not be found in any version, there may be a typo in the package name.";
            }

        general_analysis:
            var result = new LinkedList<string>();
            foreach (var rule in rules)
            {
                if (rule.GetJob() == null)
                {
                    result.AddLast(rule.GetPrettyString(pool, installedMap));
                }
                else
                {
                    result.AddLast(JobToText(rule.GetJob()));
                }
            }

            return prefix + string.Join(prefix, result);
        }

        public IEnumerator<Rule> GetEnumerator()
        {
            return new ProblemIterator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static string ConstraintToText(IConstraint constraint)
        {
            return constraint != null ? Str.Space + constraint.GetPrettyString() : string.Empty;
        }

        private string JobToText(Job job)
        {
            var packageName = job.PackageName;
            var constraint = job.Constraint;

            if (job.Command == JobCommand.Install)
            {
                var providers = pool.WhatProvides(packageName, constraint);
                if (providers.Length <= 0)
                {
                    return $"No package found to satisfy install request for {packageName}{ConstraintToText(constraint)}.";
                }

                return $"Installation request for {packageName}{ConstraintToText(constraint)} -> satisfiable by {Rule.FormatPackagesUnique(providers)}.";
            }

            if (job.Command == JobCommand.Update)
            {
                return $"Update request for {packageName}{ConstraintToText(constraint)}.";
            }

            if (job.Command == JobCommand.Uninstall)
            {
                return $"Uninstall request for {packageName}{ConstraintToText(constraint)}.";
            }

            var packages = Array.Empty<IPackage>();
            if (constraint != null)
            {
                packages = pool.WhatProvides(packageName, constraint);
            }

            return $"Job(cmd={job.Command}, target={packageName}, packages=[{Rule.FormatPackagesUnique(packages)}])";
        }

        private sealed class ProblemIterator : IEnumerator<Rule>, IEnumerable<Rule>
        {
            private readonly Problem problem;
            private LinkedListNode<List<Rule>> currentNode;
            private int index;

            public ProblemIterator(Problem problem)
            {
                this.problem = problem;
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
                if (index > currentNode.Value.Count)
                {
                    do
                    {
                        currentNode = currentNode.Previous;
                        if (currentNode == null)
                        {
                            return false;
                        }

                        index = 1;
                    }
                    while (index > currentNode.Value.Count);
                }

                var collection = currentNode.Value;
                Current = collection[index - 1];
                index++;

                return true;
            }

            public void Reset()
            {
                currentNode = problem.reasons.Last;
                index = 1;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class EqualityComparer : IEqualityComparer<Rule>
        {
            public bool Equals(Rule x, Rule y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(Rule obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}

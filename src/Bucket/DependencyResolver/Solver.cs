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

using Bucket.DependencyResolver.Operation;
using Bucket.DependencyResolver.Policy;
using Bucket.DependencyResolver.Rules;
using Bucket.IO;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// The dependency solver.
    /// </summary>
    public sealed class Solver
    {
        private readonly IIO io;
        private readonly IPolicy policy;
        private readonly Pool pool;
        private readonly IRepository installed;
        private readonly IDictionary<int, IPackage> installedMap;
        private readonly ICollection<int> updateMap;
        private readonly IList<Problem> problems;
        private readonly IList<Branch> branches;
        private readonly RuleSetGenerator ruleSetGenerator;
        private readonly Stopwatch stopwatch;
        private readonly IDictionary<int, int> learnedWhy;
        private readonly IList<LinkedList<Rule>> learnedPool;
        private RuleSet rules;
        private Decisions decisions;
        private RuleWatchGraph watchGraph;
        private Job[] jobs;
        private int propagateIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="Solver"/> class.
        /// </summary>
        /// <param name="policy">The solver policy.</param>
        /// <param name="pool">The pool contains repositories that provide packages.</param>
        /// <param name="installed">The installed packages repository.</param>
        /// <param name="io">The input/output instance.</param>
        public Solver(IPolicy policy, Pool pool, IRepository installed, IIO io)
        {
            this.installed = installed;
            this.io = io;
            this.policy = policy;
            this.pool = pool;
            ruleSetGenerator = new RuleSetGenerator(pool);
            installedMap = new Dictionary<int, IPackage>();
            updateMap = new HashSet<int>();
            problems = new List<Problem>();
            stopwatch = new Stopwatch();
            learnedWhy = new Dictionary<int, int>();
            learnedPool = new List<LinkedList<Rule>>();
            branches = new List<Branch>();
        }

        /// <summary>
        /// Gets a value indicating the rule set size.
        /// </summary>
        public int RuleSetSize => rules.Count;

        /// <summary>
        /// Gets a value indicating whether positive values have been tested for literals(a unit test mark).
        /// </summary>
        internal bool TestFlagLearnedPositiveLiteral { get; private set; }

        /// <summary>
        /// Resolve the requires of the requested.
        /// </summary>
        /// <param name="request">The request instance.</param>
        /// <param name="ignorePlantform">Whether is ignore plantform check.</param>
        /// <returns>An array of operation.</returns>
        public IOperation[] Solve(Request request, bool ignorePlantform = false)
        {
            jobs = request.GetJobs();

            SetupInstalledMap();
            rules = ruleSetGenerator.GetRulesFor(jobs, installedMap, ignorePlantform);
            CheckForRootRequiresProblems(ignorePlantform);

            decisions = new Decisions(pool);
            watchGraph = new RuleWatchGraph();
            learnedWhy.Clear();
            learnedPool.Clear();
            branches.Clear();

            foreach (var rule in rules)
            {
                watchGraph.Add(rule);
            }

            SetupAssertionRuleDecisions();

            io.WriteError("Resolving requires through SAT", true, Verbosities.Debug);
            stopwatch.Restart();
            stopwatch.Start();

            RunSAT();

            io.WriteError(string.Empty, true, Verbosities.Debug);
            io.WriteError($"Dependency resolution completed in {stopwatch.Elapsed.TotalSeconds.ToString("0.00")} seconds", true, Verbosities.Debug);

            // If we don't make a decision about the packages we have
            // installed, then we think these packages will be uninstall.
            foreach (var item in installedMap)
            {
                var packageId = item.Key;
                if (decisions.IsUndecided(packageId))
                {
                    decisions.Decide(-packageId, 1, null);
                }
            }

            if (problems.Count > 0)
            {
                throw new SolverProblemsException(problems, installedMap);
            }

            var transaction = new Transaction(policy, pool, installedMap, decisions);
            return transaction.GetOperations();
        }

        /// <summary>
        /// Makes a decision and propagates it to all rules.
        /// </summary>
        /// <remarks>
        /// Evaluates each term affected by the decision (linked through watches)
        /// If we find unit rules we make new decisions based on them.
        /// </remarks>
        /// <returns>A rule on conflict, otherwise null.</returns>
        private Rule Propagate(int level)
        {
            while (decisions.ContainsAt(propagateIndex))
            {
                var (literal, _) = decisions.At(propagateIndex);
                var conflict = watchGraph.PropagateLiteral(literal, level, decisions);
                propagateIndex++;

                if (conflict != null)
                {
                    return conflict;
                }
            }

            return null;
        }

        private int AnalyzeUnsolvable(Rule conflictRule)
        {
            var problem = new Problem(pool);
            problem.AddRule(conflictRule);

            AnalyzeUnsolvableRule(problem, conflictRule);

            problems.Add(problem);

            var seen = new HashSet<int>();

            void AddSeen(int[] literals)
            {
                foreach (var literal in literals)
                {
                    // skip the one true literal
                    if (decisions.IsSatisfy(literal))
                    {
                        continue;
                    }

                    seen.Add(Math.Abs(literal));
                }
            }

            AddSeen(conflictRule.GetLiterals());

            foreach (var (literal, reason) in decisions)
            {
                // skip literals that are not in this rule.
                if (!seen.Contains(Math.Abs(literal)))
                {
                    continue;
                }

                problem.AddRule(reason);
                AnalyzeUnsolvableRule(problem, reason);
                AddSeen(reason.GetLiterals());
            }

            return 0;
        }

        private void AnalyzeUnsolvableRule(Problem problem, Rule conflictRule)
        {
            if (conflictRule.GetRuleType() == RuleType.Learned)
            {
                var problemRules = learnedPool[learnedWhy[conflictRule.ObjectId]];
                foreach (var problemRule in problemRules)
                {
                    AnalyzeUnsolvableRule(problem, problemRule);
                }

                return;
            }

            if (conflictRule.GetRuleType() == RuleType.Package)
            {
                // package rules cannot be part of a problem.
                return;
            }

            problem.NextSection();
            problem.AddRule(conflictRule);
        }

        private int SelectAndInstall(int level, Queue<int> decisionQueue, Rule rule)
        {
            // choose best package to install from decisionQueue.
            var literals = policy.SelectPreferredPackages(pool, installedMap, decisionQueue.ToArray(), rule.GetRequirePackageName());

            var selectedLiteral = Arr.Shift(ref literals);

            if (literals.Length > 0)
            {
                branches.Add(new Branch
                {
                    Literals = literals,
                    Level = level,
                });
            }

            return SetPropagateLearn(level, selectedLiteral, rule);
        }

        /// <summary>
        /// Set propagate learn.
        /// </summary>
        /// <remarks>
        /// Add free decision (a positive literal) to decision queue
        /// increase level and propagate decision return if no conflict.
        /// in conflict case, analyze conflict rule, add resulting
        /// rule to learnt rule set, make decision from learnt
        /// rule (always unit) and re-propagate.
        /// </remarks>
        /// <returns>returns the new solver level or 0 if unsolvable.</returns>
        private int SetPropagateLearn(int level, int literal, Rule rule)
        {
            level++;

            decisions.Decide(literal, level, rule);

            while (true)
            {
                rule = Propagate(level);
                if (rule == null)
                {
                    // return if no conflict.
                    break;
                }

                if (level == 1)
                {
                    return AnalyzeUnsolvable(rule);
                }

                // conflict
                var (learnLiteral, newLevel, newRule, why) = Analyze(level, rule);

                if (newLevel <= 0 || newLevel >= level)
                {
                    throw new SolverBugException(
                        $"Trying to revert to invalid level {newLevel} from level {level}.");
                }
                else if (newRule == null)
                {
                    throw new SolverBugException(
                        $"No rule was learned from analyzing {rule} at level {level}.");
                }

                level = newLevel;
                Revert(level);
                rules.Add(newRule, RuleType.Learned);
                learnedWhy[newRule.ObjectId] = why;
                var ruleNode = new RuleWatchNode(newRule);
                ruleNode.Watch2OnHighest(decisions);
                watchGraph.Add(ruleNode);
                decisions.Decide(learnLiteral, level, newRule);
            }

            return level;
        }

        /// <summary>
        /// Reverts a decision at the given level.
        /// </summary>
        /// <param name="level">The given level.</param>
        private void Revert(int level)
        {
            while (decisions.Count > 0)
            {
                var literal = decisions.GetLastLiteral();
                if (decisions.IsUndecided(literal))
                {
                    break;
                }

                var decisionLevel = decisions.GetDecisionLevel(literal);
                if (decisionLevel <= level)
                {
                    break;
                }

                decisions.RevertLast();
                propagateIndex = decisions.Count;
            }

            while (branches.Count > 0 && branches[branches.Count - 1].Level >= level)
            {
                branches.RemoveAt(branches.Count - 1);
            }
        }

        private (int LearnLiteral, int NewLevel, Rule NewRule, int Why) Analyze(int level, Rule rule)
        {
            var analyzedRule = rule;
            var ruleLevel = 1;
            var num = 0;
            var level1Num = 0;
            var seen = new HashSet<int>();
            var learnedLiterals = new List<int>() { 0 };

            var decisionId = decisions.Count;
            var learnedPoolRules = new LinkedList<Rule>();
            learnedPool.Add(learnedPoolRules);

            while (true)
            {
                learnedPoolRules.AddLast(rule);

                foreach (var literal in rule.GetLiterals())
                {
                    // skip the one true literal
                    if (decisions.IsSatisfy(literal) || seen.Contains(Math.Abs(literal)))
                    {
                        continue;
                    }

                    seen.Add(Math.Abs(literal));

                    var decisionLevel = decisions.GetDecisionLevel(literal);
                    if (decisionLevel == 1)
                    {
                        level1Num++;
                    }
                    else if (level == decisionLevel)
                    {
                        num++;
                    }
                    else
                    {
                        // not level1 or conflict level, add to new rule
                        learnedLiterals.Add(literal);
                        if (decisionLevel > ruleLevel)
                        {
                            ruleLevel = decisionLevel;
                        }
                    }
                }

                var level1Retry = true;
                while (level1Retry)
                {
                    level1Retry = false;
                    if (num == 0 && (--level1Num) == 0)
                    {
                        // all level 1 literals done.
                        goto end_cycle;
                    }

                    int literal;
                    while (true)
                    {
                        if (decisionId <= 0)
                        {
                            throw new SolverBugException(
                                $"Reached invalid decision id {decisionId} while looking through {rule} for a literal present in the analyzed rule {analyzedRule}.");
                        }

                        decisionId--;
                        var decision = decisions.At(decisionId);
                        literal = decision.Literal;
                        if (seen.Contains(Math.Abs(literal)))
                        {
                            break;
                        }
                    }

                    seen.Remove(Math.Abs(literal));
                    if (num > 0 && ((--num) == 0))
                    {
                        if (literal < 0)
                        {
                            TestFlagLearnedPositiveLiteral = true;
                        }

                        learnedLiterals[0] = -literal;
                        if (level1Num == 0)
                        {
                            goto end_cycle;
                        }

                        var first = true;
                        foreach (var learnedLiteral in learnedLiterals)
                        {
                            if (!first)
                            {
                                seen.Remove(Math.Abs(learnedLiteral));
                            }

                            first = false;
                        }

                        // only level 1 marks left
                        level1Num++;
                        level1Retry = true;
                    }
                }

                rule = decisions.At(decisionId).Reason;
            }

        end_cycle:
            var why = learnedPool.Count - 1;
            if (learnedLiterals[0] == 0)
            {
                throw new SolverBugException(
                    $"Did not find a learnable literal in analyzed rule {analyzedRule}.");
            }

            var newRule = new RuleGeneric(learnedLiterals.ToArray(), Reason.Learned, why);
            return (learnedLiterals[0], ruleLevel, newRule, why);
        }

        private void RunSAT()
        {
            propagateIndex = 0;

            // here's the main loop:
            // 1. propagate new decisions (only needed once)
            // 2. fulfill jobs
            // 3. fulfill all unresolved rules
            // 4. minimalize solution if we had choices
            //
            // if we encounter a problem, we rewind to a safe level
            // and restart with step 1.
            //
            // Level means processing level. Indicates whether the
            // decision is on the same level(layer)
            var level = 1;
            var systemLevel = level + 1;

            while (true)
            {
                if (level == 1)
                {
                    var conflictRule = Propagate(level);
                    if (conflictRule != null)
                    {
                        AnalyzeUnsolvable(conflictRule);
                        return;
                    }
                }

                // handle job rules
                if (level < systemLevel)
                {
                    var iterator = rules.GetEnumeratorFor(RuleType.Job).GetEnumerator();
                    while (iterator.MoveNext())
                    {
                        var rule = iterator.Current;

                        if (!rule.Enable)
                        {
                            continue;
                        }

                        var decisionQueue = new Queue<int>();
                        var noneSatisfied = true;
                        foreach (var literal in rule.GetLiterals())
                        {
                            if (decisions.IsSatisfy(literal))
                            {
                                noneSatisfied = false;
                                break;
                            }

                            if (literal > 0 && decisions.IsUndecided(literal))
                            {
                                decisionQueue.Enqueue(literal);
                            }
                        }

                        if (noneSatisfied && decisionQueue.Count > 0 && installed.Count != updateMap.Count)
                        {
                            // prune all update packages until installed version
                            // except for requested updates
                            var prunedQueue = new Queue<int>();
                            foreach (var literal in decisionQueue)
                            {
                                if (!installedMap.ContainsKey(Math.Abs(literal)))
                                {
                                    continue;
                                }

                                prunedQueue.Enqueue(literal);
                                if (updateMap.Contains(Math.Abs(literal)))
                                {
                                    prunedQueue = decisionQueue;
                                    break;
                                }
                            }

                            decisionQueue = prunedQueue;
                        }

                        if (noneSatisfied && decisionQueue.Count > 0)
                        {
                            var oldLevel = level;

                            level = SelectAndInstall(level, decisionQueue, rule);

                            if (level == 0)
                            {
                                return;
                            }

                            if (level <= oldLevel)
                            {
                                break;
                            }
                        }
                    }

                    systemLevel = level + 1;

                    // jobs left.
                    if (iterator.MoveNext())
                    {
                        continue;
                    }
                }

                if (level < systemLevel)
                {
                    systemLevel = level;
                }

                var rulesCount = rules.Count;
                var pass = 1;

                for (int i = 0, n = 0; n < rulesCount; i++, n++)
                {
                    if (i == rulesCount)
                    {
                        if (pass == 1)
                        {
                            io.WriteError($"Something's changed, looking at all rules again (pass #{pass})", false, Verbosities.Debug);
                        }
                        else
                        {
                            io.OverwriteError($"Something's changed, looking at all rules again (pass #{pass})", false, -1, Verbosities.Debug);
                        }

                        i = 0;
                        pass++;
                    }

                    var rule = rules.GetRuleById(i);
                    var literals = rule.GetLiterals();
                    if (!rule.Enable)
                    {
                        continue;
                    }

                    var decisionQueue = new Queue<int>();

                    // make sure that:
                    // - all negative literals are installed
                    // - no positive literal is installed
                    //
                    // i.e. the rule is not fulfilled and we
                    // just need to decide on the positive literals
                    foreach (var literal in literals)
                    {
                        if (literal <= 0)
                        {
                            if (!decisions.IsDecidedInstall(literal))
                            {
                                goto next_rule;
                            }
                        }
                        else
                        {
                            if (decisions.IsDecidedInstall(literal))
                            {
                                goto next_rule;
                            }

                            if (decisions.IsUndecided(literal))
                            {
                                decisionQueue.Enqueue(literal);
                            }
                        }
                    }

                    // need to have at least 2 item to pick from
                    if (decisionQueue.Count < 2)
                    {
                        continue;
                    }

                    level = SelectAndInstall(level, decisionQueue, rule);
                    if (level == 0)
                    {
                        return;
                    }

                    // something changed, so look at all rules again
                    rulesCount = rules.Count;
                    n = -1;

#pragma warning disable S3626
#pragma warning disable S1751
                next_rule:
                    continue;
#pragma warning restore S1751
#pragma warning disable S3626
                }

                io.WriteError("Looking at all rules.", true, Verbosities.Debug);

                if (level < systemLevel)
                {
                    continue;
                }

                // minimization step
                if (branches.Count <= 0)
                {
                    break;
                }

                var lastLiteral = 0;
                var lastLevel = 0;
                var lastBranchIndex = 0;
                var lastBranchOffset = 0;
                for (var i = branches.Count - 1; i >= 0; i--)
                {
                    var (literals, branchLevel) = branches[i];

                    for (var offset = 0; offset < literals.Length; offset++)
                    {
                        var literal = literals[offset];

                        if (literal <= 0 || decisions.GetDecisionLevel(literal) <= branchLevel + 1)
                        {
                            continue;
                        }

                        lastLiteral = literal;
                        lastBranchIndex = i;
                        lastBranchOffset = offset;
                        lastLevel = branchLevel;
                    }
                }

                if (lastLiteral == 0)
                {
                    break;
                }

                var (branchLiterals, l) = branches[lastBranchIndex];
                Arr.RemoveAt(ref branchLiterals, lastBranchOffset);
                branches[lastBranchIndex] = new Branch(branchLiterals, l);

                level = lastLevel;
                Revert(level);
                var reason = decisions.GetLastReason();
                level = SetPropagateLearn(level, lastLiteral, reason);
                if (level == 0)
                {
                    return;
                }
            }
        }

        private void CheckForRootRequiresProblems(bool ignorePlantForm)
        {
            foreach (var job in jobs)
            {
                if (job.Command == JobCommand.Update)
                {
                    var packages = pool.WhatProvides(job.PackageName, job.Constraint);
                    foreach (var package in packages)
                    {
                        if (installedMap.ContainsKey(package.Id))
                        {
                            updateMap.Add(package.Id);
                        }
                    }
                }
                else if (job.Command == JobCommand.UpdateAll)
                {
                    foreach (var item in installedMap)
                    {
                        updateMap.Add(item.Key);
                    }
                }
                else if (job.Command == JobCommand.Install)
                {
                    if (ignorePlantForm && Regex.IsMatch(job.PackageName, RepositoryPlatform.RegexPlatform))
                    {
                        continue;
                    }

                    if (pool.WhatProvides(job.PackageName, job.Constraint).Length > 0)
                    {
                        continue;
                    }

                    var problem = new Problem(pool);
                    problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null, job));
                    problems.Add(problem);
                }
            }
        }

        private void SetupInstalledMap()
        {
            installedMap.Clear();
            foreach (var package in installed.GetPackages())
            {
                installedMap[package.Id] = package;
            }
        }

        private void SetupAssertionRuleDecisions()
        {
            var decisionStart = decisions.Count - 1;
            var rulesCount = rules.Count;

            for (var ruleIndex = 0; ruleIndex < rulesCount; ruleIndex++)
            {
                var rule = rules.GetRuleById(ruleIndex);
                if (!rule.IsAssertion || !rule.Enable)
                {
                    continue;
                }

                var literals = rule.GetLiterals();
                var literal = literals[0];

                if (!decisions.IsDecided(literal))
                {
                    decisions.Decide(literal, 1, rule);
                    continue;
                }

                if (decisions.IsSatisfy(literal))
                {
                    continue;
                }

                if (rule.GetRuleType() == RuleType.Learned)
                {
                    rule.Enable = false;
                    continue;
                }

                var conflict = decisions.GetDecisionReason(literal);
                var problem = new Problem(pool);

                problem.AddRule(rule);
                problem.AddRule(conflict);

                if (conflict != null && conflict.GetRuleType() == RuleType.Package)
                {
                    DisableProblem(rule);
                    problems.Add(problem);
                    continue;
                }

                // conflict with another job
                foreach (var assertRule in rules.GetEnumeratorFor(RuleType.Job))
                {
                    if (!assertRule.Enable || !assertRule.IsAssertion)
                    {
                        continue;
                    }

                    var assertRuleLiterals = assertRule.GetLiterals();
                    var assertRuleLiteral = assertRuleLiterals[0];

                    if (Math.Abs(literal) != Math.Abs(assertRuleLiteral))
                    {
                        continue;
                    }

                    problem.AddRule(assertRule);
                    DisableProblem(assertRule);
                }

                problems.Add(problem);
                decisions.RevertToPosition(decisionStart);
                ruleIndex = -1;
            }
        }

        private void DisableProblem(Rule why)
        {
            var job = why.GetJob();
            if (job == null)
            {
                why.Enable = false;
                return;
            }

            // disable all rules of this job.
            foreach (var rule in rules)
            {
                if (job == rule.GetJob())
                {
                    rule.Enable = false;
                }
            }
        }

        private struct Branch
        {
            public Branch(int[] literals, int level)
            {
                Literals = literals;
                Level = level;
            }

            public int[] Literals { get; set; }

            public int Level { get; set; }

#pragma warning disable S1144
            public void Deconstruct(out int[] literals, out int level)
#pragma warning restore S1144
            {
                literals = Literals;
                level = Level;
            }
        }
    }
}

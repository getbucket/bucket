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

using Bucket.DependencyResolver;
using Bucket.DependencyResolver.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Bucket.Tests.DependencyResolver.Rules
{
    [TestClass]
    public class TestsRuleSet
    {
        [TestMethod]
        public void TestAddAndGetEnumeratorFor()
        {
            var first = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);
            var second = new RuleGeneric(new[] { 2 }, Reason.JobInstall, null);
            var third = new RuleGeneric(Array.Empty<int>(), Reason.InternalAllowUpdate, null);

            var ruleSet = new RuleSet();
            ruleSet.Add(first, RuleType.Job);
            ruleSet.Add(third, RuleType.Learned);
            ruleSet.Add(second, RuleType.Job);

            Assert.AreEqual(3, ruleSet.Count);
            CollectionAssert.AreEqual(new[] { first, second }, ruleSet.GetEnumeratorFor(RuleType.Job).ToArray());
            CollectionAssert.AreEqual(new[] { third }, ruleSet.GetEnumeratorFor(RuleType.Learned).ToArray());
        }

        [TestMethod]
        public void TestAddIgnoresDuplicates()
        {
            var first = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);
            var second = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);
            var third = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);

            var ruleSet = new RuleSet();
            ruleSet.Add(first, RuleType.Job);
            Assert.IsFalse(ruleSet.Add(second, RuleType.Job));
            Assert.IsFalse(ruleSet.Add(third, RuleType.Job));

            Assert.AreEqual(1, ruleSet.Count);
            CollectionAssert.AreEqual(new[] { first }, ruleSet.GetEnumeratorFor(RuleType.Job).ToArray());
            CollectionAssert.AreEqual(Array.Empty<Rule>(), ruleSet.GetEnumeratorFor(RuleType.Learned).ToArray());
        }

        [TestMethod]
        public void TestCount()
        {
            var first = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);

            var ruleSet = new RuleSet();
            ruleSet.Add(first, RuleType.Job);

            Assert.AreEqual(1, ruleSet.Count);
        }

        [TestMethod]
        public void TestRuleById()
        {
            var first = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);
            var second = new RuleGeneric(new[] { 2 }, Reason.JobInstall, null);
            var third = new RuleGeneric(Array.Empty<int>(), Reason.InternalAllowUpdate, null);

            var ruleSet = new RuleSet();
            ruleSet.Add(first, RuleType.Job);
            ruleSet.Add(third, RuleType.Learned);
            ruleSet.Add(second, RuleType.Job);

            Assert.AreEqual(first, ruleSet.GetRuleById(0));
            Assert.AreEqual(third, ruleSet.GetRuleById(1));
            Assert.AreEqual(second, ruleSet.GetRuleById(2));
        }

        [TestMethod]
        public void TestGetEnumeratorWithout()
        {
            var first = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);
            var second = new RuleGeneric(new[] { 2 }, Reason.JobInstall, null);
            var third = new RuleGeneric(Array.Empty<int>(), Reason.InternalAllowUpdate, null);

            var ruleSet = new RuleSet();
            ruleSet.Add(first, RuleType.Job);
            ruleSet.Add(third, RuleType.Learned);
            ruleSet.Add(second, RuleType.Job);

            Assert.AreEqual(3, ruleSet.Count);

            CollectionAssert.AreEqual(new[] { third }, ruleSet.GetEnumeratorWithout(RuleType.Job).ToArray());
            CollectionAssert.AreEqual(new[] { first, second }, ruleSet.GetEnumeratorWithout(RuleType.Learned).ToArray());
        }

        [TestMethod]
        public void TestGetPrettyString()
        {
            var pool = new Pool();
            var package = Helper.MockPackage("foo", "2.1");
            var repository = Helper.MockRepository(package);
            pool.AddRepository(repository);
            var literal = package.Id;

            var ruleSet = new RuleSet();
            var rule = new RuleGeneric(new[] { literal }, Reason.JobInstall, null);
            ruleSet.Add(rule, RuleType.Job);
            StringAssert.Contains(ruleSet.GetPrettyString(pool), "Job     : Install command rule (install foo 2.1)");
        }
    }
}

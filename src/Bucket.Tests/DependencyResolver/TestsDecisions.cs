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
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Bucket.Tests.DependencyResolver
{
    [TestClass]
    public class TestsDecisions
    {
        private Pool pool;
        private Rule defaultRule;
        private Decisions decisions;

        [TestInitialize]
        public void Initialize()
        {
            pool = new Pool();
            defaultRule = new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null);
            decisions = new Decisions(pool);
        }

        [TestMethod]
        public void TestDecide()
        {
            Assert.AreEqual(0, decisions.Count);
            decisions.Decide(1, 1, defaultRule);
            Assert.AreEqual(1, decisions.Count);
            Assert.IsTrue(decisions.IsDecided(1));
        }

        [TestMethod]
        public void TestIsSatisfy()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 1, defaultRule);

            Assert.IsTrue(decisions.IsSatisfy(1));
            Assert.IsFalse(decisions.IsSatisfy(-1));
            Assert.IsTrue(decisions.IsSatisfy(-2));
            Assert.IsFalse(decisions.IsSatisfy(2));
            Assert.IsFalse(decisions.IsSatisfy(3));
        }

        [TestMethod]
        public void TestIsConflict()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 1, defaultRule);

            Assert.IsTrue(decisions.IsConflict(-1));
            Assert.IsFalse(decisions.IsConflict(1));
            Assert.IsTrue(decisions.IsConflict(2));
            Assert.IsFalse(decisions.IsConflict(-2));
            Assert.IsFalse(decisions.IsConflict(3));
        }

        [TestMethod]
        public void TestIsDecidedAndIsUndecided()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 1, defaultRule);

            Assert.IsTrue(decisions.IsDecided(1));
            Assert.IsTrue(decisions.IsDecided(2));
            Assert.IsTrue(decisions.IsUndecided(3));
        }

        [TestMethod]
        public void TestRevertLastAndIsDecided()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 1, defaultRule);
            decisions.RevertLast();

            Assert.IsTrue(decisions.IsDecided(1));
            Assert.IsFalse(decisions.IsDecided(2));
        }

        [TestMethod]
        public void TestIsDecidedInstall()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 1, defaultRule);

            Assert.IsTrue(decisions.IsDecidedInstall(1));
            Assert.IsTrue(decisions.IsDecidedInstall(-1));
            Assert.IsFalse(decisions.IsDecidedInstall(2));
            Assert.IsFalse(decisions.IsDecidedInstall(-2));
        }

        [TestMethod]
        public void TestGetDecisionLevel()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 2, defaultRule);

            Assert.AreEqual(1, decisions.GetDecisionLevel(1));
            Assert.AreEqual(1, decisions.GetDecisionLevel(-1));
            Assert.AreEqual(2, decisions.GetDecisionLevel(2));
            Assert.AreEqual(2, decisions.GetDecisionLevel(-2));
        }

        [TestMethod]
        public void TestGetDecisionReason()
        {
            var reason1 = new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null);
            var reason2 = new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null);
            decisions.Decide(1, 1, reason1);
            decisions.Decide(-2, 2, reason2);

            Assert.AreSame(reason1, decisions.GetDecisionReason(1));
            Assert.AreSame(reason1, decisions.GetDecisionReason(-1));
            Assert.AreSame(reason2, decisions.GetDecisionReason(2));
            Assert.AreSame(reason2, decisions.GetDecisionReason(-2));
        }

        [TestMethod]
        public void TestContainsAt()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 1, defaultRule);

            Assert.IsFalse(decisions.ContainsAt(-1));
            Assert.IsTrue(decisions.ContainsAt(0));
            Assert.IsTrue(decisions.ContainsAt(1));
            Assert.IsFalse(decisions.ContainsAt(2));
        }

        [TestMethod]
        public void TestGetLastReason()
        {
            var reason1 = new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null);
            var reason2 = new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null);
            decisions.Decide(1, 1, reason1);
            decisions.Decide(-2, 2, reason2);

            Assert.AreSame(reason2, decisions.GetLastReason());
        }

        [TestMethod]
        public void TestGetLastLiteral()
        {
            var reason1 = new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null);
            var reason2 = new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null);
            decisions.Decide(1, 1, reason1);
            decisions.Decide(-2, 2, reason2);

            Assert.AreEqual(-2, decisions.GetLastLiteral());
        }

        [TestMethod]
        public void TestRevert()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 2, defaultRule);
            decisions.Decide(3, 2, defaultRule);

            decisions.Revert();

            Assert.AreEqual(0, decisions.Count);
            Assert.IsFalse(decisions.IsDecided(1));
            Assert.IsFalse(decisions.IsDecided(2));
            Assert.IsFalse(decisions.IsDecided(3));
        }

        [TestMethod]
        public void TestRevertToPosition()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 2, defaultRule);
            decisions.Decide(3, 3, defaultRule);
            decisions.Decide(-4, 4, defaultRule);

            decisions.RevertToPosition(1);

            Assert.AreEqual(2, decisions.Count);
            Assert.IsTrue(decisions.IsDecided(1));
            Assert.IsTrue(decisions.IsDecided(2));
            Assert.IsFalse(decisions.IsDecided(3));
            Assert.IsFalse(decisions.IsDecided(4));
        }

        [TestMethod]
        public void TestRevertToPositionBound()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 2, defaultRule);
            decisions.Decide(3, 3, defaultRule);
            decisions.Decide(-4, 4, defaultRule);
            decisions.RevertToPosition(3);

            Assert.AreEqual(4, decisions.Count);
        }

        [TestMethod]
        public void TestRevertLast()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 2, defaultRule);
            decisions.Decide(3, 3, defaultRule);

            decisions.RevertLast();

            Assert.IsTrue(decisions.IsDecided(1));
            Assert.IsTrue(decisions.IsDecided(2));
            Assert.IsFalse(decisions.IsDecided(3));
        }

        [TestMethod]
        public void TestAt()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 2, defaultRule);
            decisions.Decide(3, 3, defaultRule);

            Assert.AreEqual(1, decisions.At(0).Literal);
            Assert.AreEqual(-2, decisions.At(1).Literal);
            Assert.AreEqual(3, decisions[2].Literal);
        }

        [TestMethod]
        public void TestToString()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 2, defaultRule);
            decisions.Decide(3, 3, defaultRule);
            decisions.Decide(-4, 4, defaultRule);

            Assert.AreEqual("[1:1,2:-2,3:3,4:-4]", decisions.ToString());
        }

        [TestMethod]
        public void TestIEnumerator()
        {
            decisions.Decide(1, 1, defaultRule);
            decisions.Decide(-2, 2, defaultRule);
            decisions.Decide(3, 3, defaultRule);
            decisions.Decide(-4, 4, defaultRule);

            var expected = new[] { -4, 3, -2, 1 };
            foreach ((int literal, Rule reason) in decisions)
            {
                Assert.AreEqual(Arr.Shift(ref expected), literal);
                Assert.AreSame(defaultRule, reason);
            }
        }
    }
}

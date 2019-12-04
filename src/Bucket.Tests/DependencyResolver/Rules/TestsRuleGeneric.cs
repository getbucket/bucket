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
using Bucket.Package;
using Bucket.Semver.Constraint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Bucket.Tests.DependencyResolver.Rules
{
    [TestClass]
    public class TestsRuleGeneric
    {
        [TestMethod]
        public void TestGetHash()
        {
            var rule = new RuleGeneric(new[] { 123 }, Reason.JobInstall, null);
            Assert.AreEqual("123".GetHashCode(StringComparison.Ordinal), rule.GetHashCode());
        }

        [TestMethod]
        public void TestEqualsForRulesWithDifferentLiterals()
        {
            var first = new RuleGeneric(new[] { 1, 2 }, Reason.JobInstall, null);
            var second = new RuleGeneric(new[] { 1, 3 }, Reason.JobInstall, null);

            Assert.IsFalse(first.Equals(second));
        }

        [TestMethod]
        public void TestEqualsForRulesWithDifferLiteralsQuantity()
        {
            var first = new RuleGeneric(new[] { 1, 2 }, Reason.JobInstall, null);
            var second = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);

            Assert.IsFalse(first.Equals(second));
        }

        [TestMethod]
        public void TestEqualsForRulesWithSameLiterals()
        {
            var first = new RuleGeneric(new[] { 1, 2 }, Reason.JobInstall, null);
            var second = new RuleGeneric(new[] { 1, 2 }, Reason.JobInstall, null);

            Assert.IsTrue(first.Equals(second));
        }

        [TestMethod]
        public void TestSetAndGetType()
        {
            var rule = new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null);
            rule.SetRuleType(RuleType.Job);

            Assert.AreEqual(RuleType.Job, rule.GetRuleType());
        }

        [TestMethod]
        public void TestEnable()
        {
            var rule = new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null)
            {
                Enable = false,
            };

            Assert.AreEqual(false, rule.Enable);

            rule.Enable = true;
            Assert.AreEqual(true, rule.Enable);
        }

        [TestMethod]
        public void TestIsAssertions()
        {
            var first = new RuleGeneric(new[] { 1, 2 }, Reason.JobInstall, null);
            var second = new RuleGeneric(new[] { 1 }, Reason.JobInstall, null);

            Assert.IsFalse(first.IsAssertion);
            Assert.IsTrue(second.IsAssertion);
        }

        [TestMethod]
        [DataRow("Install command rule (don\'t install baz 1.1 | install foo 2.1)", Reason.JobInstall)]
        public void TestPrettyString(string expected, Reason reason)
        {
            var pool = new Pool();
            var package1 = Helper.MockPackage("foo", "2.1");
            var package2 = Helper.MockPackage("baz", "1.1");
            var repository = Helper.MockRepository(package1, package2);
            pool.AddRepository(repository);

            var rule = new RuleGeneric(new[] { package1.Id, -package2.Id }, reason, null);

            Assert.AreEqual(expected, rule.GetPrettyString(pool));
        }

        [TestMethod]
        public void TestPrettyStringWithRequire()
        {
            var pool = new Pool();
            var package1 = Helper.MockPackage("foo", "2.1");
            var package2 = Helper.MockPackage("baz", "1.1");
            var package3 = Helper.MockPackage("boo", "1.2");
            var package4 = Helper.MockPackage("boo", "6.8");
            var repository = Helper.MockRepository(package1, package2, package3, package4);
            pool.AddRepository(repository);
            var link = new Link("foo", "boo", new Constraint("=", "1.2"));

            var rule = new RuleGeneric(new[] { -package1.Id, package2.Id, package3.Id, package4.Id }, Reason.PackageRequire, link);

            Assert.AreEqual("foo 2.1 relates to boo == 1.2 -> satisfiable by baz[1.1], boo[1.2, 6.8].", rule.GetPrettyString(pool));
        }

        [TestMethod]
        public void TestGetLiterals()
        {
            var rule = new RuleGeneric(new[] { 1, -2 }, Reason.JobInstall, null);
            CollectionAssert.AreEqual(new[] { -2, 1 }, rule.GetLiterals());
        }

        [TestMethod]
        public void TestToString()
        {
            var rule = new RuleGeneric(new[] { 1, -2 }, Reason.JobInstall, null);

            Assert.AreEqual("(-2|1)", rule.ToString());
            rule.Enable = false;
            Assert.AreEqual("disabled(-2|1)", rule.ToString());
        }
    }
}

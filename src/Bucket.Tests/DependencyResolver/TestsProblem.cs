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
using Bucket.Semver.Constraint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Bucket.Tests.DependencyResolver
{
    [TestClass]
    public class TestsProblem
    {
        private Pool pool;
        private Problem problem;
        private Request request;

        [TestInitialize]
        public void Initialize()
        {
            pool = new Pool();
            problem = new Problem(pool);
            request = new Request();
        }

        [TestMethod]
        public void TestAddRule()
        {
            request.Install("foo", new Constraint(">=", "1.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[0]));

            Assert.AreEqual(1, problem.GetReasons().Length);
            Assert.AreEqual(Reason.JobInstall, problem.GetReasons()[0].GetReason());
        }

        [TestMethod]
        public void TestAddRuleMult()
        {
            request.Install("foo", new Constraint(">=", "1.0"));
            request.Install("bar", new Constraint(">=", "1.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[0]));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[1]));

            problem.NextSection();

            request.Install("baz", new Constraint(">=", "1.0"));
            request.Install("plp", new Constraint(">=", "1.0"));

            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[2]));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[3]));

            problem.NextSection();
            problem.NextSection();
            request.Install("app", new Constraint(">=", "1.0"));

            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[4]));
            problem.NextSection();

            var expected = new[] { "app", "baz", "plp", "foo", "bar" };
            var actual = problem.GetReasons();
            Assert.AreEqual(expected.Length, actual.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i].GetJob().PackageName);
            }

            var n = 0;
            foreach (var rule in problem)
            {
                Assert.AreEqual(expected[n++], rule.GetJob().PackageName);
            }
        }

        [TestMethod]
        public void TestGetPrettyStringWithIllegalChars()
        {
            request.Install("illegal#@#chars", new Constraint(">=", "1.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[0]));

            StringAssert.Contains(
                problem.GetPrettyString(),
                "The requested package illegal#@#chars could not be found, it looks like its name is invalid, \"#@#\" is not allowed in package names.");
        }

        [TestMethod]
        public void TestGetPrettyStringWithByPassfilter()
        {
            request.Install("foo", new Constraint(">=", "1.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[0]));

            var package1 = Helper.MockPackage("foo", "1.5");
            var package2 = Helper.MockPackage("foo", "2.0");
            var package3 = Helper.MockPackage("foo", "3.0");

            var repository = Helper.MockRepository(package1, package2, package3);
            pool.AddRepository(repository);
            pool.SetWhiteList(new[] { int.MaxValue });

            StringAssert.Contains(
                problem.GetPrettyString(),
                "The requested package foo >= 1.0 is satisfiable by foo[1.5, 2.0, 3.0] but these conflict with your requirements or minimum-stability.");
        }

        [TestMethod]
        public void TestGetPrettyStringWithRejectedConstraint()
        {
            request.Install("foo", new Constraint(">=", "9.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[0]));

            var package1 = Helper.MockPackage("foo", "1.5");
            var package2 = Helper.MockPackage("foo", "2.0");
            var package3 = Helper.MockPackage("foo", "3.0");

            var repository = Helper.MockRepository(package1, package2, package3);
            pool.AddRepository(repository);

            StringAssert.Contains(
                problem.GetPrettyString(),
                "The requested package foo >= 9.0 exists as foo[1.5, 2.0, 3.0] but these are rejected by your constraint.");
        }

        [TestMethod]
        public void TestGetPrettyStringWithNotFoundAnyVersion()
        {
            request.Install("foo", new Constraint(">=", "2.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[0]));

            StringAssert.Contains(
                problem.GetPrettyString(),
                "The requested package foo could not be found in any version, there may be a typo in the package name.");
        }

        [TestMethod]
        public void TestGetPrettyStringMultJobInstall()
        {
            request.Install("foo", new Constraint(">=", "2.0"));
            request.Install("bar", new Constraint(">=", "2.0"));
            request.Install("no-package", new Constraint(">=", "3.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[0]));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[1]));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobInstall, null, request.GetJobs()[2]));

            var package1 = Helper.MockPackage("foo", "1.5");
            var package2 = Helper.MockPackage("foo", "2.0");
            var package3 = Helper.MockPackage("bar", "3.0");

            var repository = Helper.MockRepository(package1, package2, package3);
            pool.AddRepository(repository);

            var actual = problem.GetPrettyString();
            StringAssert.Contains(
                actual,
                "Installation request for bar >= 2.0 -> satisfiable by bar[3.0].");
            StringAssert.Contains(
                actual,
                "Installation request for foo >= 2.0 -> satisfiable by foo[2.0].");
            StringAssert.Contains(
                actual,
                "No package found to satisfy install request for no-package >= 3.0.");
        }

        [TestMethod]
        public void TestGetPrettyStringMultJobUpdate()
        {
            request.Update("foo", new Constraint(">=", "2.0"));
            request.Update("bar", new Constraint(">=", "2.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.InternalAllowUpdate, null, request.GetJobs()[0]));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.InternalAllowUpdate, null, request.GetJobs()[1]));

            var actual = problem.GetPrettyString();
            StringAssert.Contains(actual, "Update request for foo >= 2.0.");
            StringAssert.Contains(actual, "Update request for bar >= 2.0.");
        }

        [TestMethod]
        public void TestGetPrettyStringMultJobUninstall()
        {
            request.Uninstall("foo", new Constraint(">=", "2.0"));
            request.Uninstall("bar", new Constraint(">=", "2.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobUninstall, null, request.GetJobs()[0]));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobUninstall, null, request.GetJobs()[1]));

            var actual = problem.GetPrettyString();
            StringAssert.Contains(actual, "Uninstall request for foo >= 2.0.");
            StringAssert.Contains(actual, "Uninstall request for bar >= 2.0.");
        }

        [TestMethod]
        public void TestEmptyRuleGenericProblem()
        {
            request.Install("foo", new Constraint(">=", "2.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.Undefined, null, request.GetJobs()[0]));

            var actual = problem.GetPrettyString();
            StringAssert.Contains(actual, "The requested package foo could not be found in any version, there may be a typo in the package name.");
        }

        [TestMethod]
        public void TestGetPrettyStringMultJobOther()
        {
            // This will not happen under normal circumstances.
            var job1 = new Job()
            {
                Command = JobCommand.UpdateAll,
                PackageName = "foo",
                Constraint = new Constraint(">=", "2.0"),
            };

            var job2 = new Job()
            {
                Command = JobCommand.UpdateAll,
                PackageName = "bar",
                Constraint = new Constraint(">=", "2.0"),
            };

            var package1 = Helper.MockPackage("foo", "1.5");
            var package2 = Helper.MockPackage("foo", "2.0");
            var package3 = Helper.MockPackage("foo", "3.0");
            var package4 = Helper.MockPackage("bar", "3.0");

            var repository = Helper.MockRepository(package1, package2, package3, package4);
            pool.AddRepository(repository);

            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.InternalAllowUpdate, null, job1));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.InternalAllowUpdate, null, job2));

            var actual = problem.GetPrettyString();
            StringAssert.Contains(actual, "Job(cmd=UpdateAll, target=bar, packages=[bar[3.0]])");
            StringAssert.Contains(actual, "Job(cmd=UpdateAll, target=foo, packages=[foo[2.0, 3.0]])");
        }

        [TestMethod]
        public void TestPrettyDisplayOrder()
        {
            request.Uninstall("foo", new Constraint(">=", "2.0"));
            request.Uninstall("bar", new Constraint(">=", "2.0"));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobUninstall, null, request.GetJobs()[0]));
            problem.AddRule(new RuleGeneric(Array.Empty<int>(), Reason.JobUninstall, null, request.GetJobs()[1]));

            var actual = problem.GetPrettyString();
            Assert.IsTrue(actual.IndexOf("bar >= 2.0", StringComparison.OrdinalIgnoreCase)
                > actual.IndexOf("foo >= 2.0", StringComparison.OrdinalIgnoreCase));
        }
    }
}

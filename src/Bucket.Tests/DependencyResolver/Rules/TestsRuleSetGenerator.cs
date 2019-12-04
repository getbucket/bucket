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
using System.Collections.Generic;

namespace Bucket.Tests.DependencyResolver.Rules
{
    [TestClass]
    public class TestsRuleSetGenerator
    {
        private Pool pool;

        [TestInitialize]
        public void Initialize()
        {
            pool = new Pool();

            var packageBucketCore = Helper.MockPackage("bucket.core", "2.2",
                new[] { new Link("bucket.core", "logger", new Constraint("==", "6.8"), "provide") });
            var packageBucketCore3 = Helper.MockPackage("bucket.core", "3.6",
                new[] { new Link("bucket.core", "logger", new Constraint("==", "6.8"), "provide") });
            var packageBucketCore5 = Helper.MockPackage("bucket.core", "5.2",
                new[] { new Link("bucket.core", "logger", new Constraint("==", "6.8"), "provide") });
            var packageBucketHelper = Helper.MockPackage("bucket.helper", "1.6",
                requires: new[] { new Link("bucket.helper", "bucket.core", new Constraint(">=", "2.0"), "requires") });
            var packageUnity = Helper.MockPackage("unity", "1.6",
                requires: new[] { new Link("unity", "bucket.helper", new Constraint(">=", "1.0"), "requires") });
            var packageLogger = Helper.MockPackage("logger", "6.8");

            var repository = Helper.MockRepository(packageBucketCore, packageBucketHelper, packageUnity, packageLogger, packageBucketCore3, packageBucketCore5);

            pool.AddRepository(repository);
        }

        [TestMethod]
        public void TestGetRulesFor()
        {
            var generator = new RuleSetGenerator(pool);
            var ruleSet = generator.GetRulesFor(
                new[]
                {
                    new Job()
                    {
                        Command = JobCommand.Install,
                        PackageName = "unity",
                        Constraint = new Constraint(">", "1.0"),
                    },
                }, new Dictionary<int, IPackage>());

            var expected = new[]
            {
                "unity 1.6 requires bucket.helper >= 1.0 -> satisfiable by bucket.helper[1.6].",
                "bucket.helper 1.6 requires bucket.core >= 2.0 -> satisfiable by bucket.core[2.2, 3.6, 5.2].",
                "Can only install one of: bucket.core[3.6, 2.2].",
                "Can only install one of: bucket.core[5.2, 2.2].",
                "Can only install one of: bucket.core[5.2, 3.6].",
                "Install command rule (install unity 1.6)",
            };

            Assert.AreEqual(expected.Length, ruleSet.Count);

            for (var i = 0; i < ruleSet.Count; i++)
            {
                Assert.AreEqual(expected[i], ruleSet[i].GetPrettyString(pool));
            }
        }

        [TestMethod]
        public void TestGetRulesWithConflictRule()
        {
            var packageConflict = Helper.MockPackage("bucket.conflict", "2.2",
                conflict: new[] { new Link("bucket.conflict", "bucket.core", new Constraint("<", "5.0"), "conflict") });
            var repository = Helper.MockRepository(packageConflict);
            pool.AddRepository(repository);
            var generator = new RuleSetGenerator(pool);

            var ruleSet = generator.GetRulesFor(
                new[]
                {
                    new Job()
                    {
                        Command = JobCommand.Install,
                        PackageName = "unity",
                        Constraint = new Constraint(">", "1.0"),
                    },
                    new Job()
                    {
                        Command = JobCommand.Install,
                        PackageName = "bucket.conflict",
                        Constraint = new Constraint(">", "2.0"),
                    },
                }, new Dictionary<int, IPackage>());

            var expected = new[]
            {
                "unity 1.6 requires bucket.helper >= 1.0 -> satisfiable by bucket.helper[1.6].",
                "bucket.helper 1.6 requires bucket.core >= 2.0 -> satisfiable by bucket.core[2.2, 3.6, 5.2].",
                "Can only install one of: bucket.core[3.6, 2.2].",
                "Can only install one of: bucket.core[5.2, 2.2].",
                "Can only install one of: bucket.core[5.2, 3.6].",
                "Install command rule (install unity 1.6)",
                "Install command rule (install bucket.conflict 2.2)",
                "bucket.conflict 2.2 conflicts with bucket.core[2.2].",
                "bucket.conflict 2.2 conflicts with bucket.core[3.6].",
            };

            Assert.AreEqual(expected.Length, ruleSet.Count);

            for (var i = 0; i < ruleSet.Count; i++)
            {
                Assert.AreEqual(expected[i], ruleSet[i].GetPrettyString(pool));
            }
        }

        [TestMethod]
        public void TestGetRulesReplaceRule()
        {
            var packageReplaceBucketHelper = Helper.MockPackage("bucket.replace.helper", "2.6",
                replaces: new[] { new Link("bucket.replace.helper", "bucket.helper", new Constraint("==", "1.6"), "replaces") });

            var packageReplaceFoo = Helper.MockPackage("replace.foo", "2.8",
                requires: new[] { new Link("replace.foo", "bucket.replace.helper", new Constraint(">=", "1.0"), "requires") });

            var repository = Helper.MockRepository(packageReplaceBucketHelper, packageReplaceFoo);
            pool.AddRepository(repository);
            var generator = new RuleSetGenerator(pool);

            var ruleSet = generator.GetRulesFor(
                new[]
                {
                    new Job()
                    {
                        Command = JobCommand.Install,
                        PackageName = "unity",
                        Constraint = new Constraint(">", "1.0"),
                    },
                    new Job()
                    {
                        Command = JobCommand.Install,
                        PackageName = "replace.foo",
                        Constraint = new Constraint(">", "2.0"),
                    },
                }, new Dictionary<int, IPackage>());

            var expected = new[]
            {
                "unity 1.6 requires bucket.helper >= 1.0 -> satisfiable by bucket.helper[1.6], bucket.replace.helper[2.6].",
                "bucket.helper 1.6 requires bucket.core >= 2.0 -> satisfiable by bucket.core[2.2, 3.6, 5.2].",
                "don't install bucket.replace.helper 2.6 | don't install bucket.helper 1.6",
                "Can only install one of: bucket.core[3.6, 2.2].",
                "Can only install one of: bucket.core[5.2, 2.2].",
                "Can only install one of: bucket.core[5.2, 3.6].",
                "Install command rule (install unity 1.6)",
                "replace.foo 2.8 requires bucket.replace.helper >= 1.0 -> satisfiable by bucket.replace.helper[2.6].",
                "Install command rule (install replace.foo 2.8)",
            };

            Assert.AreEqual(expected.Length, ruleSet.Count);

            for (var i = 0; i < ruleSet.Count; i++)
            {
                Assert.AreEqual(expected[i], ruleSet[i].GetPrettyString(pool));
            }
        }

        [TestMethod]
        public void TestGetRulesUninstallRule()
        {
            var generator = new RuleSetGenerator(pool);
            var installedPackages = new Dictionary<int, IPackage>();
            for (var i = 1; i <= pool.Count; i++)
            {
                installedPackages.Add(i, pool.GetPackageById(i));
            }

            var ruleSet = generator.GetRulesFor(
                new[]
                {
                    new Job()
                    {
                        Command = JobCommand.Uninstall,
                        PackageName = "unity",
                        Constraint = new Constraint(">", "1.0"),
                    },
                }, installedPackages);

            var expected = new[]
            {
                "Can only install one of: bucket.core[3.6, 2.2].",
                "Can only install one of: bucket.core[5.2, 2.2].",
                "bucket.helper 1.6 requires bucket.core >= 2.0 -> satisfiable by bucket.core[2.2, 3.6, 5.2].",
                "Can only install one of: bucket.core[5.2, 3.6].",
                "unity 1.6 requires bucket.helper >= 1.0 -> satisfiable by bucket.helper[1.6].",
                "Uninstall command rule (don't install unity 1.6)",
            };

            for (var i = 0; i < ruleSet.Count; i++)
            {
                Assert.AreEqual(expected[i], ruleSet[i].GetPrettyString(pool));
            }
        }
    }
}

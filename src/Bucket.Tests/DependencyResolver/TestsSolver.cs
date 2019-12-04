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
using Bucket.DependencyResolver.Operation;
using Bucket.DependencyResolver.Policy;
using Bucket.IO;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Semver.Constraint;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.DependencyResolver
{
    [TestClass]
    public sealed class TestsSolver
    {
        private Pool pool;
        private RepositoryArray repository;
        private RepositoryArray repositoryInstalled;
        private Request request;
        private IPolicy policy;
        private Solver solver;

        [TestInitialize]
        public void Initialize()
        {
            pool = new Pool();
            repository = new RepositoryArray();
            repositoryInstalled = new RepositoryArray();

            request = new Request();
            policy = new PolicyDefault();
            solver = new Solver(policy, pool, repositoryInstalled, IONull.That);
        }

        [TestMethod]
        public void TestSolverInstallSingle()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            repository.AddPackage(packageFoo);
            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult((JobCommand.Install, packageFoo));
        }

        [TestMethod]
        public void TestSolverUninstallInstalledPackageIfNotInstall()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            repositoryInstalled.AddPackage(packageFoo);
            RepositoryComplete();

            AssertSolverResult((JobCommand.Uninstall, packageFoo));
        }

        [TestMethod]
        public void TestInstallNonExistingPackageFails()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            repository.AddPackage(packageFoo);
            RepositoryComplete();

            request.Install("bar", Helper.Constraint("==", "1.0"));

            try
            {
                solver.Solve(request);
                Assert.Fail("Unsolvable conflict did not result in exception.");
            }
            catch (SolverProblemsException exception)
            {
                var problems = exception.GetProblems();
                Assert.AreEqual(1, problems.Length);
                Assert.AreEqual(2, exception.ExitCode);
                StringAssert.Contains(
                    problems[0].GetPrettyString(),
                    "- The requested package bar could not be found in any version, there may be a typo in the package name.");
            }
        }

        [TestMethod]
        public void TestSolverInstallSamePackageFromDifferentRepositories()
        {
            var repository1 = new RepositoryArray();
            var repository2 = new RepositoryArray();

            var packageFoo1 = Helper.MockPackage("foo", "1.0");
            var packageFoo2 = Helper.MockPackage("foo", "1.0");

            repository1.AddPackage(packageFoo1);
            repository2.AddPackage(packageFoo2);

            pool.AddRepository(repository1);
            pool.AddRepository(repository2);

            request.Install("foo");

            AssertSolverResult((JobCommand.Install, packageFoo1));
        }

        [TestMethod]
        public void TestSolverInstallWithRequires()
        {
            var requires = new[]
            {
                new Link("foo", "bar", Helper.Constraint("<", "1.2"), "requires"),
            };

            var packageBarLower = Helper.MockPackage("bar", "1.0");
            var packageBarHight = Helper.MockPackage("bar", "1.2");
            var packageFoo = Helper.MockPackage("foo", "1.0", requires: requires);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBarLower);
            repository.AddPackage(packageBarHight);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult(
                (JobCommand.Install, packageBarLower),
                (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        public void TestSolverInstallWithNotEqualOperator()
        {
            var packageBar = Helper.MockPackage("bar", "1.0");
            var packageBar11 = Helper.MockPackage("bar", "1.1");
            var packageBar12 = Helper.MockPackage("bar", "1.2");
            var packageBar13 = Helper.MockPackage("bar", "1.3");

            var requires = new[]
            {
                new Link("foo", "bar", new ConstraintMulti(
                    Helper.Constraint("<=", "1.3"),
                    Helper.Constraint("!=", "1.3"),
                    Helper.Constraint("!=", "1.2")), "requires"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: requires);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBar11);
            repository.AddPackage(packageBar12);
            repository.AddPackage(packageBar13);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult(
                (JobCommand.Install, packageBar11),
                (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        public void TestSolverInstallWithRequiresInOrder()
        {
            var barRequires = new[]
            {
                new Link("bar", "foo", Helper.Constraint(">=", "1.0"), "requires"),
                new Link("bar", "baz", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var bazRequires = new[]
            {
                new Link("bar", "foo", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageBar = Helper.MockPackage("bar", "1.0", requires: barRequires);
            var packageBaz = Helper.MockPackage("baz", "1.0", requires: bazRequires);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBaz);

            RepositoryComplete();

            request.Install("foo");
            request.Install("bar");
            request.Install("baz");

            AssertSolverResult(
                (JobCommand.Install, packageFoo),
                (JobCommand.Install, packageBaz),
                (JobCommand.Install, packageBar));
        }

        [TestMethod]
        public void TestSolverInstallInstalled()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            repositoryInstalled.AddPackage(packageFoo);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult();
        }

        [TestMethod]
        public void TestSolverInstallInstalledWithAlternative()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            repository.AddPackage(packageFoo);
            repositoryInstalled.AddPackage(packageFoo);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult();
        }

        [TestMethod]
        public void TestSolverUninstallSingle()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            repositoryInstalled.AddPackage(packageFoo);

            RepositoryComplete();

            request.Uninstall("foo");

            AssertSolverResult((JobCommand.Uninstall, packageFoo));
        }

        [TestMethod]
        public void TestSolverUninstallUninstalledPackage()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            repository.AddPackage(packageFoo);

            RepositoryComplete();

            request.Uninstall("foo");

            AssertSolverResult();
        }

        [TestMethod]
        public void TestSolverUninstallRequires()
        {
            var barRequires = new[]
            {
                new Link("bar", "foo", Helper.Constraint(">=", "1.0"), "requires"),
                new Link("bar", "baz", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageBar = Helper.MockPackage("bar", "1.0", requires: barRequires);
            var packageBaz = Helper.MockPackage("baz", "1.0");

            repositoryInstalled.AddPackage(packageFoo);
            repositoryInstalled.AddPackage(packageBar);
            repositoryInstalled.AddPackage(packageBaz);

            RepositoryComplete();

            request.Install("foo");
            request.Uninstall("bar");

            AssertSolverResult(
                (JobCommand.Uninstall, packageBaz),
                (JobCommand.Uninstall, packageBar));
        }

        [TestMethod]
        public void TestSolverUpdateDoesOnlyUpdate()
        {
            var fooRequires = new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);
            var packageBar = Helper.MockPackage("bar", "1.0");
            var packageBar11 = Helper.MockPackage("bar", "1.1");

            repositoryInstalled.AddPackage(packageFoo);
            repositoryInstalled.AddPackage(packageBar);
            repository.AddPackage(packageBar11);

            RepositoryComplete();

            request.Install("foo", Helper.Constraint("==", "1.0"));
            request.Install("bar", Helper.Constraint("==", "1.1"));

            request.Update("foo", Helper.Constraint("==", "1.0"));
            request.Update("bar", Helper.Constraint("==", "1.0"));

            AssertSolverResult(
                (JobCommand.Update, packageBar, packageBar11));
        }

        [TestMethod]
        public void TestSolverUpdateSingle()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageFoo11 = Helper.MockPackage("foo", "1.1");

            repositoryInstalled.AddPackage(packageFoo);
            repository.AddPackage(packageFoo11);

            RepositoryComplete();

            request.Install("foo");
            request.Update("foo");

            AssertSolverResult(
                (JobCommand.Update, packageFoo, packageFoo11));
        }

        [TestMethod]
        public void TestSolverUpdateAll()
        {
            var fooRequires = new[]
            {
                new Link("foo", "bar", null, "requires"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);
            var packageFoo11 = Helper.MockPackage("foo", "1.1", requires: fooRequires);
            var packageBar = Helper.MockPackage("bar", "1.0");
            var packageBar11 = Helper.MockPackage("bar", "1.1");

            repositoryInstalled.AddPackage(packageFoo);
            repositoryInstalled.AddPackage(packageBar);
            repository.AddPackage(packageFoo11);
            repository.AddPackage(packageBar11);

            RepositoryComplete();

            request.Install("foo");
            request.UpdateAll();

            AssertSolverResult(
                (JobCommand.Update, packageBar, packageBar11),
                (JobCommand.Update, packageFoo, packageFoo11));
        }

        [TestMethod]
        public void TestSolverUpdateCurrent()
        {
            var packageFoo_1 = Helper.MockPackage("foo", "1.0");
            var packageFoo_2 = Helper.MockPackage("foo", "1.0");
            repositoryInstalled.AddPackage(packageFoo_1);
            repository.AddPackage(packageFoo_2);

            RepositoryComplete();

            request.Install("foo");
            request.Update("foo");

            AssertSolverResult();
        }

        [TestMethod]
        public void TestSolverUpdateOnlyUpdatesSelectedPackage()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageFoo11 = Helper.MockPackage("foo", "1.1");
            var packageBar = Helper.MockPackage("bar", "1.0");
            var packageBar11 = Helper.MockPackage("bar", "1.1");

            repositoryInstalled.AddPackage(packageFoo);
            repositoryInstalled.AddPackage(packageBar);
            repository.AddPackage(packageFoo11);
            repository.AddPackage(packageBar11);

            RepositoryComplete();

            request.Install("foo");
            request.Install("bar");
            request.Update("foo");

            AssertSolverResult(
                (JobCommand.Update, packageFoo, packageFoo11));
        }

        [TestMethod]
        public void TestSolverUpdateConstrained()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageFoo11 = Helper.MockPackage("foo", "1.1");
            var packageFoo20 = Helper.MockPackage("foo", "2.0");

            repositoryInstalled.AddPackage(packageFoo);
            repository.AddPackage(packageFoo11);
            repository.AddPackage(packageFoo20);

            RepositoryComplete();

            request.Install("foo", Helper.Constraint("<", "2.0"));
            request.Update("foo");

            AssertSolverResult(
               (JobCommand.Update, packageFoo, packageFoo11));
        }

        [TestMethod]
        public void TestSolverUpdateFullyConstrained()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageFoo11 = Helper.MockPackage("foo", "1.1");
            var packageFoo20 = Helper.MockPackage("foo", "2.0");

            repositoryInstalled.AddPackage(packageFoo);
            repository.AddPackage(packageFoo11);
            repository.AddPackage(packageFoo20);

            RepositoryComplete();

            request.Install("foo", Helper.Constraint("<", "2.0"));

            // Upgrade is limited by the constraints of the installation
            request.Update("foo", Helper.Constraint("=", "1.0"));

            AssertSolverResult(
               (JobCommand.Update, packageFoo, packageFoo11));
        }

        [TestMethod]
        public void TestSolverUpdateFullyConstrainedPrunesInstalledPackages()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageFoo11 = Helper.MockPackage("foo", "1.1");
            var packageFoo20 = Helper.MockPackage("foo", "2.0");
            var packageBar = Helper.MockPackage("bar", "1.0");

            repositoryInstalled.AddPackage(packageFoo);
            repositoryInstalled.AddPackage(packageBar);
            repository.AddPackage(packageFoo11);
            repository.AddPackage(packageFoo20);

            RepositoryComplete();

            request.Install("foo", Helper.Constraint("<", "2.0"));
            request.Update("foo", Helper.Constraint("=", "1.0"));

            AssertSolverResult(
               (JobCommand.Update, packageFoo, packageFoo11),
               (JobCommand.Uninstall, packageBar));
        }

        [TestMethod]
        public void TestSolverAllJobs()
        {
            var packageC10 = Helper.MockPackage("c", "1.0");
            var packageD10_1 = Helper.MockPackage("d", "1.0");
            repositoryInstalled.AddPackage(packageD10_1);
            repositoryInstalled.AddPackage(packageC10);

            var aRequires = new[]
            {
                new Link("a", "b", Helper.Constraint("<", "1.1"), "requires"),
            };
            var packageA20 = Helper.MockPackage("a", "2.0", requires: aRequires);
            var packageB10 = Helper.MockPackage("b", "1.0");
            var packageB11 = Helper.MockPackage("b", "1.1");
            var packageC11 = Helper.MockPackage("c", "1.1");
            var packageD10_2 = Helper.MockPackage("d", "1.0");
            repository.AddPackage(packageA20);
            repository.AddPackage(packageB10);
            repository.AddPackage(packageB11);
            repository.AddPackage(packageC11);
            repository.AddPackage(packageD10_2);

            RepositoryComplete();

            request.Install("a");
            request.Install("c");
            request.Update("c");
            request.Uninstall("d");

            AssertSolverResult(
               (JobCommand.Update, packageC10, packageC11),
               (JobCommand.Install, packageB10),
               (JobCommand.Install, packageA20),
               (JobCommand.Uninstall, packageD10_1));
        }

        [TestMethod]
        public void TestSolverThreeAlternativeRequiresAndConflict()
        {
            var fooRequires = new[]
            {
                new Link("foo", "bar", Helper.Constraint("<", "1.1"), "requires"),
            };

            var fooConflict = new[]
            {
                new Link("foo", "bar", Helper.Constraint("<", "1.0"), "conflicts"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires, conflict: fooConflict);
            var packageBarOlder = Helper.MockPackage("bar", "0.9");
            var packageBarMiddle = Helper.MockPackage("bar", "1.0");
            var packageBarNewer = Helper.MockPackage("bar", "1.1");

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBarMiddle);
            repository.AddPackage(packageBarNewer);
            repository.AddPackage(packageBarOlder);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult(
               (JobCommand.Install, packageBarMiddle),
               (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        public void TestSolverReplacement()
        {
            var barReplace = new[]
            {
                new Link("bar", "foo", null, "replacement"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0");
            var packageBar = Helper.MockPackage("bar", "1.0", replaces: barReplace);

            repositoryInstalled.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            RepositoryComplete();

            request.Install("bar");

            AssertSolverResult(
               (JobCommand.Update, packageFoo, packageBar));
        }

        [TestMethod]
        public void TestInstallOneOfTwoAlternatives()
        {
            var packageFoo_1 = Helper.MockPackage("foo", "1.0");
            var packageFoo_2 = Helper.MockPackage("foo", "1.0");

            repository.AddPackage(packageFoo_1);
            repository.AddPackage(packageFoo_2);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult(
              (JobCommand.Install, packageFoo_1));
        }

        [TestMethod]
        [ExpectedException(typeof(SolverProblemsException))]
        public void TestInstallProviderNotExplicitly()
        {
            var fooRequires = new[]
            {
                new Link("foo", "baz", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var barProvider = new[]
            {
                new Link("bar", "baz", Helper.Constraint("=", "1.0"), "provider"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);
            var packageBar = Helper.MockPackage("bar", "1.0", provides: barProvider);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            RepositoryComplete();

            request.Install("foo");

            // must explicitly pick the provider, so error in this case
            solver.Solve(request);
        }

        [TestMethod]
        public void TestInstallProviderExplicitly()
        {
            var fooRequires = new[]
            {
                new Link("foo", "baz", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var barProvider = new[]
            {
                new Link("bar", "baz", Helper.Constraint("=", "1.0"), "provider"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);
            var packageBar = Helper.MockPackage("bar", "1.0", provides: barProvider);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            RepositoryComplete();

            request.Install("foo");

            // We must explicitly specify that the provided
            // provide will be implicitly effective.
            request.Install("bar");

            AssertSolverResult(
               (JobCommand.Install, packageBar),
               (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        public void TestSkipReplacerOfExistingPackage()
        {
            var fooRequires = new[]
            {
                new Link("foo", "baz", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var barReplacement = new[]
            {
                new Link("bar", "baz", Helper.Constraint(">=", "1.0"), "replaces"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);
            var packageBar = Helper.MockPackage("bar", "1.0", replaces: barReplacement);
            var packageBaz = Helper.MockPackage("baz", "1.0");

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBaz);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult(
               (JobCommand.Install, packageBaz),
               (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        [ExpectedException(typeof(SolverProblemsException))]
        public void TestNoInstallReplacerOfMissingPackage()
        {
            var fooRequires = new[]
            {
                new Link("foo", "baz", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var barReplacement = new[]
            {
                new Link("bar", "baz", Helper.Constraint(">=", "1.0"), "replaces"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);
            var packageBar = Helper.MockPackage("bar", "1.0", replaces: barReplacement);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            RepositoryComplete();

            request.Install("foo");

            solver.Solve(request);
        }

        [TestMethod]
        public void TestSkipReplacedPackageIfReplacerIsSelected()
        {
            var fooRequires = new[]
            {
                new Link("foo", "baz", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var barReplacement = new[]
            {
                new Link("bar", "baz", Helper.Constraint(">=", "1.0"), "replaces"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);
            var packageBar = Helper.MockPackage("bar", "1.0", replaces: barReplacement);
            var packageBaz = Helper.MockPackage("baz", "1.0");

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBaz);

            RepositoryComplete();

            request.Install("foo");
            request.Install("bar");

            AssertSolverResult(
               (JobCommand.Install, packageBar),
               (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        public void TestPickOlderIfNewerConflicts()
        {
            var fooRequires = new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "2.0"), "requires"),
                new Link("foo", "baz", Helper.Constraint(">=", "2.0"), "requires"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);

            // bar:
            var bar20Requires = new[]
            {
                new Link("bar", "baz", Helper.Constraint(">=", "2.0"), "requires"),
            };

            // new package bar21 require on version of package baz that does not exist
            var bar21Requires = new[]
            {
                new Link("bar", "baz", Helper.Constraint(">=", "2.2"), "requires"),
            };

            var packageBar20 = Helper.MockPackage("bar", "2.0", requires: bar20Requires);
            var packageBar21 = Helper.MockPackage("bar", "2.1", requires: bar21Requires);

            // baz:
            var packageBaz21 = Helper.MockPackage("baz", "2.1");

            // add a package boo replacing both bar and baz, so that boo and bar or
            // boo and baz cannot be simultaneously installed
            var booReplacement = new[]
            {
                new Link("boo", "bar", Helper.Constraint(">=", "2.0"), "replaces"),
                new Link("boo", "baz", Helper.Constraint(">=", "2.0"), "replaces"),
            };
            var packageBoo = Helper.MockPackage("boo", "2.0", replaces: booReplacement);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar20);
            repository.AddPackage(packageBar21);
            repository.AddPackage(packageBaz21);
            repository.AddPackage(packageBoo);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult(
              (JobCommand.Install, packageBaz21),
              (JobCommand.Install, packageBar20),
              (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        public void TestInstallCircularRequires()
        {
            var fooRequires = new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var bar11Requires = new[]
            {
                new Link("bar", "foo", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var packageFoo = Helper.MockPackage("foo", "1.0", requires: fooRequires);
            var packageBar09 = Helper.MockPackage("bar", "0.9");
            var packageBar11 = Helper.MockPackage("bar", "1.1", requires: bar11Requires);

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar09);
            repository.AddPackage(packageBar11);

            RepositoryComplete();

            request.Install("foo");

            AssertSolverResult(
               (JobCommand.Install, packageBar11),
               (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        public void TestInstallAlternativeWithCircularRequires()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0", requires: new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "1.0"), "requires"),
            });
            var packageBar = Helper.MockPackage("bar", "1.0", requires: new[]
            {
                new Link("bar", "virtual", Helper.Constraint(">=", "1.0"), "requires"),
            });
            var packageBaz = Helper.MockPackage("baz", "1.0", provides: new[]
            {
                new Link("baz", "virtual", Helper.Constraint("==", "1.0"), "provide"),
            }, requires: new[]
            {
                new Link("baz", "foo", Helper.Constraint("==", "1.0"), "requires"),
            });
            var packageBoo = Helper.MockPackage("boo", "1.0", provides: new[]
            {
                new Link("baz", "virtual", Helper.Constraint("==", "1.0"), "provide"),
            }, requires: new[]
            {
                new Link("baz", "foo", Helper.Constraint("==", "1.0"), "requires"),
            });

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBaz);
            repository.AddPackage(packageBoo);

            RepositoryComplete();

            request.Install("foo");
            request.Install("baz");

            AssertSolverResult(
              (JobCommand.Install, packageFoo),
              (JobCommand.Install, packageBaz),
              (JobCommand.Install, packageBar));
        }

        [TestMethod]
        public void TestUseReplacerIfNecessary()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0", requires: new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "1.0"), "requires"),
                new Link("foo", "vritual", Helper.Constraint(">=", "1.0"), "requires"),
            });
            var packageBar = Helper.MockPackage("bar", "1.0");
            var packageBaz_1 = Helper.MockPackage("baz", "1.0", replaces: new[]
            {
                new Link("baz", "bar", Helper.Constraint(">=", "1.0"), "provide"),
                new Link("baz", "vritual", Helper.Constraint(">=", "1.0"), "provide"),
            });
            var packageBaz_2 = Helper.MockPackage("baz", "1.1", replaces: new[]
            {
                new Link("baz", "bar", Helper.Constraint(">=", "1.0"), "provide"),
                new Link("baz", "vritual", Helper.Constraint(">=", "1.0"), "provide"),
            });

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBaz_1);
            repository.AddPackage(packageBaz_2);

            RepositoryComplete();

            request.Install("foo");
            request.Install("baz");

            AssertSolverResult(
             (JobCommand.Install, packageBaz_2),
             (JobCommand.Install, packageFoo));
        }

        [TestMethod]
        [ExpectedException(typeof(SolverProblemsException), "The requested package unknow could not be found in any version, there may be a typo in the package name.")]
        public void TestInstallUnknowDecidedWithAlternative()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0", requires: new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "1.0"), "requires"),
                new Link("foo", "baz", Helper.Constraint(">=", "1.0"), "requires"),
            });
            var packageBar = Helper.MockPackage("bar", "1.0");
            var packageBaz_1 = Helper.MockPackage("baz", "1.0", replaces: new[]
            {
                new Link("baz", "bar", Helper.Constraint(">=", "1.0"), "provide"),
            });
            var packageBaz_2 = Helper.MockPackage("baz", "1.1", replaces: new[]
            {
                new Link("baz", "bar", Helper.Constraint(">=", "1.0"), "provide"),
            });

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBaz_1);
            repository.AddPackage(packageBaz_2);

            RepositoryComplete();

            request.Install("foo");
            request.Install("unknow");

            solver.Solve(request);
        }

        [TestMethod]
        [Timeout(5000)]
        [ExpectedException(typeof(SolverProblemsException))]
        public void TestNoEndlesLoop()
        {
            var packageFoo1 = Helper.MockPackage("foo", "2.0.x-dev");
            var packageFoo2 = Helper.MockPackage("foo", "2.1.x-dev");
            var packageFoo3 = Helper.MockPackage("foo", "2.2.x-dev");

            var packageBar1 = Helper.MockPackage("bar", "2.0.10", requires: new[]
            {
                new Link("bar", "foo", Helper.Constraint(">=", "2.1.0.0-dev"), "requires"),
            });
            var packageBar2 = Helper.MockPackage("bar", "2.0.9", requires: new[]
            {
                new Link("bar", "foo", Helper.Constraint(">=", "2.1.0.0-dev"), "requires"),
            }, replaces: new[]
            {
                new Link("bar", "boz", Helper.Constraint(">=", "2.0.9.0"), "replaces"),
            });

            var packageBoo = Helper.MockPackage("boo", "2.0-dev", requires: new[]
            {
                new Link("boo", "foo", Helper.Constraint(">=", "2.0"), "requires"),
                new Link("boo", "boz", Helper.Constraint(">=", "2.0"), "requires"),
            });
            var packageBoz = Helper.MockPackage("boz", "2.0.9", requires: new[]
            {
                new Link("boo", "foo", Helper.Constraint(">=", "2.1"), "requires"),
                new Link("boo", "bar", Helper.Constraint(">=", "2.0-dev"), "requires"),
            });

            repository.AddPackage(packageFoo1);
            repository.AddPackage(packageFoo2);
            repository.AddPackage(packageFoo3);
            repository.AddPackage(packageBar1);
            repository.AddPackage(packageBar2);
            repository.AddPackage(packageBoo);
            repository.AddPackage(packageBoz);

            RepositoryComplete();

            request.Install("boo", Helper.Constraint("==", "2.0-dev"));

            solver.Solve(request);
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(SolverProblemsException), @"
  Problem 1
    - Installation request for foo -> satisfiable by foo[1.0].
    - bar 1.0 conflicts with foo[1.0].
    - Installation request for bar -> satisfiable by bar[1.0].")]
        public void TestConflictResultEmpty()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0", conflict: new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "1.0"), "conflict"),
            });
            var packageBar = Helper.MockPackage("bar", "1.0");

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            RepositoryComplete();

            request.Install("foo");
            request.Install("bar");

            solver.Solve(request);
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(SolverProblemsException), @"
  Problem 1
    - Installation request for foo -> satisfiable by foo[1.0].
    - foo 1.0 requires bar >= 2.0 -> no matching package found.")]
        public void TestUnsatisfiableRequires()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0", requires: new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "2.0"), "requires"),
            });
            var packageBar = Helper.MockPackage("bar", "1.0");

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);

            RepositoryComplete();

            request.Install("foo");

            solver.Solve(request);
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(SolverProblemsException), @"
  Problem 1
    - boo 1.0 requires boz >= 1.0 -> satisfiable by boz[1.0].
    - boz 1.0 requires bar < 1.0 -> satisfiable by bar[0.9].
    - bar 1.0 requires boo >= 1.0 -> satisfiable by boo[1.0].
    - Can only install one of: bar[0.9, 1.0].
    - foo 1.0 requires bar >= 1.0 -> satisfiable by bar[1.0].
    - Installation request for foo -> satisfiable by foo[1.0].")]
        public void TestRequiresMismatchException()
        {
            var packageFoo = Helper.MockPackage("foo", "1.0", requires: new[]
            {
                new Link("foo", "bar", Helper.Constraint(">=", "1.0"), "requires"),
            });
            var packageBar1 = Helper.MockPackage("bar", "1.0", requires: new[]
            {
                new Link("bar", "boo", Helper.Constraint(">=", "1.0"), "requires"),
            });
            var packageBar2 = Helper.MockPackage("bar", "0.9");
            var packageBoo = Helper.MockPackage("boo", "1.0", requires: new[]
            {
                new Link("boo", "boz", Helper.Constraint(">=", "1.0"), "requires"),
            });
            var packageBoz = Helper.MockPackage("boz", "1.0", requires: new[]
            {
                new Link("boz", "bar", Helper.Constraint("<", "1.0"), "requires"),
            });

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar1);
            repository.AddPackage(packageBar2);
            repository.AddPackage(packageBoo);
            repository.AddPackage(packageBoz);

            RepositoryComplete();

            request.Install("foo");

            solver.Solve(request);
        }

        [TestMethod]
        public void TestLearnLiteralsWithSortedRuleLiterals()
        {
            var packageFoo20 = Helper.MockPackage("provider1/foo", "2.0");
            var packageFoo16 = Helper.MockPackage("provider1/foo", "1.6");
            var packageFoo15 = Helper.MockPackage("provider1/foo", "1.5");

            var packageBar = Helper.MockPackage("provider2/bar", "2.0", replaces: new[]
            {
                new Link("provider2/bar", "provider2/baz", Helper.Constraint("=", "2.0"), "replaces"),
            });

            var packageBaz = Helper.MockPackage("provider2/baz", "2.0", requires: new[]
            {
                new Link("provider2/baz", "provider1/foo", Helper.Constraint("<", "2.0"), "requires"),
            });

            repository.AddPackage(packageFoo20);
            repository.AddPackage(packageFoo16);
            repository.AddPackage(packageFoo15);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageBaz);

            RepositoryComplete();

            request.Install("provider2/baz");
            request.Install("provider1/foo");

            AssertSolverResult(
            (JobCommand.Install, packageFoo16),
            (JobCommand.Install, packageBaz));
        }

        [TestMethod]
        public void TestInstallRecursiveAliasRequires()
        {
            var packageFoo10 = Helper.MockPackage("foo", "1.0");
            var packageFoo20 = Helper.MockPackage("foo", "2.0", requires: new[]
            {
                new Link("foo", "bar", Helper.Constraint("=", "2.0"), "requires", "== 2.0"),
            });
            var packageBar = Helper.MockPackage("bar", "2.0", requires: new[]
            {
                new Link("bar", "foo", Helper.Constraint(">=", "2.0"), "requires"),
            });
            var packageFoo20Alias = Helper.MockPackageAlias(packageFoo20, "1.1");

            repository.AddPackage(packageFoo10);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageFoo20);
            repository.AddPackage(packageFoo20Alias);

            RepositoryComplete();

            request.Install("foo", Helper.Constraint("==", "1.1"));

            AssertSolverResult(
            (JobCommand.Install, packageFoo20),
            (JobCommand.Install, packageBar),
            (JobCommand.MarkPackageAliasInstalled, packageFoo20Alias));
        }

        [TestMethod]
        public void TestInstallAlias()
        {
            var packageFoo = Helper.MockPackage("foo", "2.0");
            var packageBar = Helper.MockPackage("bar", "1.0", requires: new[]
            {
                new Link("bar", "foo", Helper.Constraint("<", "2.0"), "requires"),
            });

            var packageFooAlias = Helper.MockPackageAlias(packageFoo, "1.1");

            repository.AddPackage(packageFoo);
            repository.AddPackage(packageBar);
            repository.AddPackage(packageFooAlias);

            RepositoryComplete();

            request.Install("foo", Helper.Constraint("==", "2.0"));
            request.Install("bar");

            AssertSolverResult(
            (JobCommand.Install, packageFoo),
            (JobCommand.MarkPackageAliasInstalled, packageFooAlias),
            (JobCommand.Install, packageBar));
        }

        [TestMethod]
        public void TestUninstallAlias()
        {
            var packageFoo = Helper.MockPackage("foo", "2.0");
            var packageBar = Helper.MockPackage("bar", "1.0", requires: new[]
            {
                new Link("bar", "foo", Helper.Constraint("<", "2.0"), "requires"),
            });

            var packageFooAlias = Helper.MockPackageAlias(packageFoo, "1.1");

            repositoryInstalled.AddPackage(packageFoo);
            repositoryInstalled.AddPackage(packageBar);
            repositoryInstalled.AddPackage(packageFooAlias);

            RepositoryComplete();

            request.Uninstall("foo");
            request.Uninstall("bar");

            AssertSolverResult(
            (JobCommand.Uninstall, packageBar),
            (JobCommand.MarkPackageAliasUninstall, packageFooAlias),
            (JobCommand.Uninstall, packageFoo));
        }

        [TestMethod]
        public void TestLearnPositiveLiteral()
        {
            var packageARequires = new[]
            {
                new Link("a", "b", Helper.Constraint("==", "1.0"), "requires"),
                new Link("a", "c", Helper.Constraint(">=", "1.0"), "requires"),
                new Link("a", "d", Helper.Constraint("==", "1.0"), "requires"),
            };

            var packageBRequires = new[]
            {
                new Link("b", "e", Helper.Constraint("==", "1.0"), "requires"),
            };

            var packageC1Requires = new[]
            {
                new Link("c", "f", Helper.Constraint("==", "1.0"), "requires"),
            };

            var packageC2Requires = new[]
            {
                new Link("c", "f", Helper.Constraint("==", "1.0"), "requires"),
                new Link("c", "g", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var packageDRequires = new[]
            {
                new Link("d", "f", Helper.Constraint(">=", "1.0"), "requires"),
            };

            var packageERequires = new[]
            {
                new Link("e", "g", Helper.Constraint("<=", "2.0"), "requires"),
            };

            var packageA = Helper.MockPackage("a", "1.0", requires: packageARequires);
            var packageB = Helper.MockPackage("b", "1.0", requires: packageBRequires);
            var packageC1 = Helper.MockPackage("c", "1.0", requires: packageC1Requires);
            var packageC2 = Helper.MockPackage("c", "2.0", requires: packageC2Requires);
            var packageD = Helper.MockPackage("d", "1.0", requires: packageDRequires);
            var packageE = Helper.MockPackage("e", "1.0", requires: packageERequires);
            var packageF1 = Helper.MockPackage("f", "1.0");
            var packageF2 = Helper.MockPackage("f", "2.0");
            var packageG1 = Helper.MockPackage("g", "1.0");
            var packageG2 = Helper.MockPackage("g", "2.0");
            var packageG3 = Helper.MockPackage("g", "3.0");

            repository.AddPackage(packageA);
            repository.AddPackage(packageB);
            repository.AddPackage(packageC1);
            repository.AddPackage(packageC2);
            repository.AddPackage(packageD);
            repository.AddPackage(packageE);
            repository.AddPackage(packageF1);
            repository.AddPackage(packageF2);
            repository.AddPackage(packageG1);
            repository.AddPackage(packageG2);
            repository.AddPackage(packageG3);

            RepositoryComplete();

            request.Install("a");

            Assert.AreEqual(false, solver.TestFlagLearnedPositiveLiteral);

            AssertSolverResult(
            (JobCommand.Install, packageF1),
            (JobCommand.Install, packageD),
            (JobCommand.Install, packageG2),
            (JobCommand.Install, packageC2),
            (JobCommand.Install, packageE),
            (JobCommand.Install, packageB),
            (JobCommand.Install, packageA));

            Assert.AreEqual(true, solver.TestFlagLearnedPositiveLiteral);
        }

        private void RepositoryComplete()
        {
            pool.AddRepository(repositoryInstalled);
            pool.AddRepository(repository);
        }

        private void AssertSolverResult(params SolverResult[] expected)
        {
            CollectionAssert.AreEqual(expected, solver.Solve(request));
        }

        private sealed class SolverResult
        {
            public SolverResult(JobCommand command, IPackage package)
            {
                Command = command;
                To = package;
            }

            public SolverResult(JobCommand command, IPackage from, IPackage to)
            {
                Command = command;
                From = from;
                To = to;
            }

            public JobCommand Command { get; private set; }

            public IPackage From { get; private set; }

            public IPackage To { get; private set; }

#pragma warning disable CA2225
            public static implicit operator SolverResult((JobCommand command, IPackage package) tuple)
#pragma warning restore CA2225
            {
                return new SolverResult(tuple.command, tuple.package);
            }

#pragma warning disable CA2225
            public static implicit operator SolverResult((JobCommand command, IPackage from, IPackage to) tuple)
#pragma warning restore CA2225
            {
                return new SolverResult(tuple.command, tuple.from, tuple.to);
            }

            public override bool Equals(object obj)
            {
                if (obj is OperationUpdate operationUpdate)
                {
                    Assert.AreEqual(Command, operationUpdate.JobCommand);
                    Assert.AreEqual(From, operationUpdate.GetInitialPackage());
                    Assert.AreEqual(To, operationUpdate.GetTargetPackage());
                    return true;
                }
                else if (obj is BaseOperation baseOperation)
                {
                    Assert.AreEqual(Command, baseOperation.JobCommand);
                    Assert.AreEqual(To, baseOperation.GetPackage());
                    return true;
                }

                return ReferenceEquals(this, obj);
            }

            public override int GetHashCode()
            {
                return Command.GetHashCode();
            }
        }
    }
}

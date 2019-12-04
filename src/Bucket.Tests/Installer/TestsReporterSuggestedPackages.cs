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

using Bucket.Installer;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Tester;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace Bucket.Tests.Installer
{
    [TestClass]
    public class TestsReporterSuggestedPackages
    {
        private ReporterSuggestedPackages reporter;
        private TesterIOConsole tester;
        private Mock<IRepositoryInstalled> repositoryInstalled;
        private Mock<IPackage> package;

        [TestInitialize]
        public void Initialize()
        {
            tester = new TesterIOConsole();
            reporter = new ReporterSuggestedPackages(tester.Mock(AbstractTester.OptionStdErrorSeparately(true)));
            repositoryInstalled = new Mock<IRepositoryInstalled>();
            package = new Mock<IPackage>();
            package.Setup((o) => o.GetNamePretty()).Returns("foo");
        }

        [TestMethod]
        public void TestAddSuggestion()
        {
            reporter.AddSuggestion("foo", "bar", "baz");
            reporter.Display();
            StringAssert.Contains(tester.GetDisplayError(), "foo suggests installing bar (baz)");
        }

        [TestMethod]
        public void TestAddSuggestionMult()
        {
            reporter.AddSuggestion("foo", "bar", "reason 1");
            reporter.AddSuggestion("foo", "baz", "reason 2");
            reporter.Display();

            var display = tester.GetDisplayError();
            StringAssert.Contains(display, "foo suggests installing bar (reason 1)");
            StringAssert.Contains(display, "foo suggests installing baz (reason 2)");
        }

        [TestMethod]
        public void TestAddSuggestions()
        {
            package.Setup((o) => o.GetSuggests()).Returns(() => new SortedDictionary<string, string>()
            {
                { "bar", "reason 1" },
                { "baz", "reason 2" },
            });

            reporter.AddSuggestions(package.Object);

            reporter.Display();

            var display = tester.GetDisplayError();
            StringAssert.Contains(display, "foo suggests installing bar (reason 1)");
            StringAssert.Contains(display, "foo suggests installing baz (reason 2)");
        }

        [TestMethod]
        public void TestGetSuggestionsEmptyByDefault()
        {
            CollectionAssert.AreEqual(Array.Empty<Suggestion>(), reporter.GetSuggestions());
        }

        [TestMethod]
        public void TestGetSuggestedPackages()
        {
            reporter.AddSuggestion("foo", "bar", "baz");
            CollectionAssert.AreEqual(new[] { new Suggestion("foo", "bar", "baz") }, reporter.GetSuggestions());
        }

        [TestMethod]
        public void TestDisplayWithNoReason()
        {
            reporter.AddSuggestion("foo", "bar");
            reporter.Display();

            var display = tester.GetDisplayError();
            StringAssert.Contains(display.Trim(), "foo suggests installing bar");
        }

        [TestMethod]
        public void TestDisplayIgnoresFormatting()
        {
            reporter.AddSuggestion("foo", "bar", "\x1b[1;37;42m foo\r\nbar \x1b[0m");
            reporter.AddSuggestion("foo", "baz", "<bg=red>foobaz</>");
            reporter.Display();

            var display = tester.GetDisplayError();
            StringAssert.Contains(display, "foo suggests installing bar ([1;37;42m foo bar [0m)");
            StringAssert.Contains(display, "foo suggests installing baz (<bg=red>foobaz</>)");
        }

        [TestMethod]
        public void TestDisplaySkipInstalledPackage()
        {
            package.Setup((o) => o.GetSuggests()).Returns(() => new SortedDictionary<string, string>()
            {
                { "bar", "reason 1" },
                { "baz", "reason 2" },
            });

            var packageBazMock = new Mock<IPackage>();
            packageBazMock.Setup((o) => o.GetNames()).Returns(new[] { "baz" });

            repositoryInstalled.Setup((o) => o.GetPackages()).Returns(new[] { packageBazMock.Object });

            reporter.AddSuggestions(package.Object);

            reporter.Display(repositoryInstalled.Object);

            var display = tester.GetDisplayError();
            StringAssert.Contains(display.Trim(), "foo suggests installing bar (reason 1)");
        }

        [TestMethod]
        public void TestDisplayProcessReason()
        {
            reporter.AddSuggestion("foo", "bar", "install %target% get %source% gui suuport");
            reporter.Display();

            var display = tester.GetDisplayError();
            StringAssert.Contains(display.Trim(), "foo suggests installing bar (install bar get foo gui suuport)");
        }
    }
}

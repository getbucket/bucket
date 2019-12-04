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

using Bucket.Command;
using Bucket.Console;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Command
{
    [TestClass]
    public class TestsCommandValidate
    {
        [TestMethod]
        public void TestExecute()
        {
            var tester = new TesterCommand(new CommandValidate());
            Assert.AreEqual(ExitCodes.FileNotFoundException, tester.Execute());
            StringAssert.Contains(tester.GetDisplay(), "not found.");
        }

        [TestMethod]
        public void TestExecuteWithFileArgument()
        {
            var file = Helper.Fixtrue("Command/validate-normal.json");
            var tester = new TesterCommand(new CommandValidate());

            Assert.AreEqual(GameBox.Console.ExitCodes.Normal, tester.Execute(file, AbstractTester.OptionDecorated(false)));
            StringAssert.Contains(tester.GetDisplay(), "is valid.");
        }

        [TestMethod]
        public void TestExecuteWithFileArgumentRelative()
        {
            var tester = new TesterCommand(new CommandValidate());

            Assert.AreEqual(GameBox.Console.ExitCodes.Normal, tester.Execute("Fixtures/Command/validate-normal.json", AbstractTester.OptionDecorated(false)));
            StringAssert.Contains(tester.GetDisplay(), "Fixtures/Command/validate-normal.json is valid.");
        }

        [TestMethod]
        public void TestExecuteFailds()
        {
            var file = Helper.Fixtrue("Command/validate-failds.json");
            var tester = new TesterCommand(new CommandValidate());

            Assert.AreEqual(ExitCodes.ValidationErrors, tester.Execute(file));

            var display = tester.GetDisplay();
            StringAssert.Contains(display, "strict errors that make it unable to be published as a package:");
            StringAssert.Contains(display, "bar, baz are required both in require and require-dev, this can lead to unexpected behavior.");
            StringAssert.Contains(display, "Name \"fooBarBaz\" does not match the best practice (e.g. lower-cased/with-dashes). We suggest using \"foo-bar-baz\" instead.");
            StringAssert.Contains(display, "It is recommended to add a provider name to the package (e.g. provide-name/package-name).");
        }

        [TestMethod]
        public void TestExecuteFaildsNotCheckPublish()
        {
            var file = Helper.Fixtrue("Command/validate-failds.json");
            var tester = new TesterCommand(new CommandValidate());

            Assert.AreEqual(GameBox.Console.ExitCodes.Normal, tester.Execute($"--no-check-publish {file}"));
        }

        [TestMethod]
        public void TestExecuteFaildsNotCheckPublishStrict()
        {
            var file = Helper.Fixtrue("Command/validate-failds.json");
            var tester = new TesterCommand(new CommandValidate());

            Assert.AreEqual(ExitCodes.ValidationWarning, tester.Execute($"--no-check-publish --strict {file}"));
        }
    }
}

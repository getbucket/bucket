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

using Bucket.IO;
using Bucket.Tester;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Tester
{
    [TestClass]
    public class TestsTesterIOConsole
    {
        [TestMethod]
        public void TestTesterIOConsoleMock()
        {
            var tester = new TesterIOConsole();
            tester.SetInputs(new[] { "menghanyu" });

            var io = tester.Mock(AbstractTester.OptionStdErrorSeparately(true));

            Assert.AreEqual("menghanyu", (string)io.Ask("what's your name?", "miaomiao"));
            io.Write("hello world");
            Assert.AreEqual(
@"hello world
", tester.GetDisplay());

            io.WriteError("hello world error");
            Assert.AreEqual(
@"what's your name?hello world error
", tester.GetDisplayError());
        }

        [TestMethod]
        public void TestTesterIOConsoleTrack()
        {
            var tester = new TesterIOConsole();
            tester.SetInputs(new[] { "menghanyu" });

            var io = tester.Track(
                new IOConsole(null, null),
                AbstractTester.OptionStdErrorSeparately(true),
                AbstractTester.Interactive(false));

            Assert.AreEqual("miaomiao", (string)io.Ask("what's your name?", "miaomiao"));

            io.Write("hello world");
            Assert.AreEqual(
@"hello world
", tester.GetDisplay());

            io.WriteError("hello world error");
            Assert.AreEqual(
@"hello world error
", tester.GetDisplayError());
        }
    }
}

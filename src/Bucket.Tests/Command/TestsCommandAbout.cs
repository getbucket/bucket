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
using GameBox.Console;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Command
{
    [TestClass]
    public class TestsCommandAbout
    {
        [TestMethod]
        public void TestExecute()
        {
            var tester = new TesterCommand(new CommandAbout());
            Assert.AreEqual(ExitCodes.Normal, tester.Execute());
            StringAssert.Contains(
                tester.GetDisplay(), @"
Bucket - Package Dependency Manager
Bucket is a dependency manager tracking local dependencies of your projects and libraries.
See https://github.com/getbucket/bucket/wiki for more information.

");
        }
    }
}

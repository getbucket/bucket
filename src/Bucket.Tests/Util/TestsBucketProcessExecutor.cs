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

using Bucket.Tester;
using Bucket.Util;
using GameBox.Console.Output;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsBucketProcessExecutor
    {
        [TestMethod]
        public void TestExecuteCapturesOutput()
        {
            var process = new BucketProcessExecutor();
            process.Execute("echo foo", out string stdout);

            Assert.AreEqual(
@"foo
", stdout);
        }

        [TestMethod]
        public void TestExecuteCapturesErrorOutput()
        {
            var process = new BucketProcessExecutor();
            process.Execute("unknow foo", out _, out string stderr);

            StringAssert.Contains(stderr, "unknow");
        }

        [TestMethod]
        public void TestHidenSensitive()
        {
            var tester = new TesterIOConsole();
            var process = new BucketProcessExecutor(tester.Mock(AbstractTester.OptionVerbosity(OutputOptions.VerbosityDebug)));

            process.Execute("echo https://foo:bar@example.org/ && echo http://foo@example.org && echo http://abcdef1234567890234578:x-oauth-token@github.com/", out string stdout);

            StringAssert.Contains(tester.GetDisplay(), "Executing command (CWD): echo https://foo:***@example.org/ && echo http://foo@example.org && echo http://***:***@github.com/");
        }

        [TestMethod]
        public void TestDoesntHidePorts()
        {
            var tester = new TesterIOConsole();
            var process = new BucketProcessExecutor(tester.Mock(AbstractTester.OptionVerbosity(OutputOptions.VerbosityDebug)));

            process.Execute("echo https://localhost:1234/");

            StringAssert.Contains(tester.GetDisplay(), "Executing command (CWD): echo https://localhost:1234/");
        }

        [TestMethod]
        public void TestDefaultTimeout()
        {
            var process = new BucketProcessExecutor();
            var processDefault = new BucketProcessExecutor
            {
                Timeout = 1,
            };

            Assert.AreEqual(Timeout.Infinite, process.Timeout);
            Assert.AreEqual(1, processDefault.Timeout);

            BucketProcessExecutor.SetDefaultTimeout(999);

            Assert.AreEqual(999, process.Timeout);
            Assert.AreEqual(1, processDefault.Timeout);

            Assert.AreEqual(999, BucketProcessExecutor.GetDefaultTimeout());
        }
    }
}

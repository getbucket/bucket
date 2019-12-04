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

using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsExtensionIProcessExecutor
    {
        [TestMethod]
        public void TestExecuteWithStdoutStderr()
        {
            var process = new BucketProcessExecutor();
            process.Execute("echo foo", out string stdout, out string stderr);

            Assert.AreEqual("foo", stdout.Trim());
            Assert.AreEqual(string.Empty, stderr);
        }

        [TestMethod]
        public void TestExecuteWithStdoutArray()
        {
            var process = new BucketProcessExecutor();
            process.Execute("echo foo && echo bar", out string[] stdout);

            Assert.AreEqual("foo", stdout[0].Trim());
            Assert.AreEqual("bar", stdout[1].Trim());
        }

        [TestMethod]
        public void TestExecuteWithStdout()
        {
            var process = new BucketProcessExecutor();
            process.Execute("echo foo && echo bar", out string stdout);

            StringAssert.Contains(stdout, "foo");
            StringAssert.Contains(stdout, "bar");
        }
    }
}

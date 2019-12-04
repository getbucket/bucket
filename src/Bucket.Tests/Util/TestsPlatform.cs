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
using System;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsPlatform
    {
        [TestMethod]
        public void TestExpandPath()
        {
            Environment.SetEnvironmentVariable("TESTENV", "/home/test");

            Assert.AreEqual("/home/test/foo", Platform.ExpandPath("%TESTENV%/foo"));
            Assert.AreEqual("/home/test/foo", Platform.ExpandPath("$TESTENV/foo"));
            Assert.AreEqual(Helper.GetHome() + "/foo", Platform.ExpandPath("~/foo"));
        }
    }
}

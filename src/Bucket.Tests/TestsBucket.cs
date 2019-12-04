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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests
{
    [TestClass]
    public class TestsBucket
    {
        [TestMethod]
        public void TestGetVersion()
        {
            Assert.IsNotNull(Bucket.GetVersion());
            Assert.AreNotEqual(string.Empty, Bucket.GetVersion());
        }

        [TestMethod]
        public void TestsGetReleaseData()
        {
            Assert.IsTrue(Bucket.GetReleaseData().Ticks > 0);
        }

        [TestMethod]
        public void TestsGetEventDispatcher()
        {
            var bucket = new Bucket();
            Assert.IsNull(bucket.GetEventDispatcher());
        }

        [TestMethod]
        public void TestsSetEventDispatcher()
        {
            var bucket = new Bucket();
            bucket.SetEventDispatcher(null);
        }
    }
}

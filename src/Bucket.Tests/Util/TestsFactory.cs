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

using Bucket.Configuration;
using Bucket.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsFactory
    {
        [TestMethod]
        public void TestCreate()
        {
            var factory = new Factory();
            var cwd = Helper.Fixtrue("Util");
            var bucket = factory.CreateBucket(IONull.That, "bucket-factory-1.json", false, cwd);
            Assert.AreEqual(false, (bool)bucket.GetConfig().Get(Settings.SecureHttp));
        }

        [TestMethod]
        public void TestCreateConfig()
        {
            var factory = new Factory();
            Assert.AreNotEqual(null, factory.CreateConfig(IONull.That));
        }
    }
}

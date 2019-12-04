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
using Bucket.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryFactory
    {
        [TestMethod]
        public void TestCreateManager()
        {
            var manager = RepositoryFactory.CreateManager(IONull.That, new Config());
            Assert.AreNotEqual(null, manager);
        }

        [TestMethod]
        public void TestCreateDefaultRepository()
        {
            var repositories = RepositoryFactory.CreateDefaultRepository(IONull.That);
            Assert.AreEqual(1, repositories.Length);
            Assert.AreEqual(typeof(RepositoryBucket), repositories[0].GetType());
        }
    }
}

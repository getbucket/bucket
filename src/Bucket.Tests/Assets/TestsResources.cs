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

using Bucket.Assets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Assets
{
    [TestClass]
    public class TestsResources
    {
        [TestMethod]
        public void TestGetString()
        {
            var content = Resources.GetString("Schema/bucket-schema.json");
            Assert.IsTrue(!string.IsNullOrEmpty(content));
        }

        [TestMethod]
        public void TestGetStream()
        {
            var content = Resources.GetStream("Schema/bucket-schema.json").ToText();
            Assert.IsTrue(!string.IsNullOrEmpty(content));
        }
    }
}

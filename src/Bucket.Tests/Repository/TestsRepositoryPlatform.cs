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

using Bucket.Plugin;
using Bucket.Repository;
using Bucket.Semver.Constraint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Bucket.Tests.Repository
{
    [TestClass]
    public class TestsRepositoryPlatform
    {
        [TestMethod]
        public void TestConstructorGivenPlatforms()
        {
            var platform = new RepositoryPlatform(new Dictionary<string, string>()
            {
                { "foo", "1.0.0" },
            });

            var package = platform.FindPackage("foo", "1.0.0");
            Assert.AreEqual("foo-1.0.0.0", package.ToString());
        }

        [TestMethod]
        public void TestDefaultApiPackage()
        {
            var platform = new RepositoryPlatform();
            var package = platform.FindPackage(PluginManager.PluginRequire, new ConstraintNone());
            StringAssert.Contains(package.ToString(), PluginManager.PluginRequire);
        }
    }
}

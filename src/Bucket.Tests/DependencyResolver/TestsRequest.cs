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

using Bucket.DependencyResolver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Bucket.Tests.DependencyResolver
{
    [TestClass]
    public class TestsRequest
    {
        [TestMethod]
        public void TestRequestInstallAndUninstall()
        {
            var request = new Request();
            request.Install("foo");
            request.Fix("bar");
            request.Uninstall("foobar");

            CollectionAssert.AreEqual(
                new[]
                {
                    new Job { Command = JobCommand.Install, PackageName = "foo", Constraint = null, Fixed = false },
                    new Job { Command = JobCommand.Install, PackageName = "bar", Constraint = null, Fixed = true },
                    new Job { Command = JobCommand.Uninstall, PackageName = "foobar", Constraint = null, Fixed = false },
                },
                request.GetJobs().ToArray());
        }

        [TestMethod]
        public void TestUpdateAndUpdateAll()
        {
            var request = new Request();
            request.Update("foo");
            request.UpdateAll();

            CollectionAssert.AreEqual(
                new[]
                {
                    new Job { Command = JobCommand.Update, PackageName = "foo", Constraint = null, Fixed = false },
                    new Job { Command = JobCommand.UpdateAll, PackageName = null, Constraint = null, Fixed = false },
                },
                request.GetJobs().ToArray());
        }
    }
}

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
using Bucket.Downloader;
using Bucket.Tester;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsGuardConfig
    {
        [TestMethod]
        [ExpectedExceptionAndMessage(
            typeof(TransportException),
            "Your configuration does not allow connections")]
        public void TestProhibitUri()
        {
            Guard.That.ProhibitUri(new Config(), "http://github.com/foo/bar");
        }

        [TestMethod]
        public void TestProhibitUriAllowNonSecureHttp()
        {
            var config = new Mock<Config>(true, null);
            config.Setup((o) => o.Get(Settings.SecureHttp, ConfigOptions.None))
                  .Returns(() => false);

            var tester = new TesterIOConsole();
            Guard.That.ProhibitUri(config.Object, "http://github.com/foo/bar", tester.Mock());

            StringAssert.Contains(tester.GetDisplay(), "Warning: Accessing github.com over http which is an insecure protocol.");
        }
    }
}

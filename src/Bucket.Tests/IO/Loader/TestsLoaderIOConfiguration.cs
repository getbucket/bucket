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
using Bucket.IO.Loader;
using Bucket.Tester;
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Bucket.Tests.IO.Loader
{
    [TestClass]
    public class TestsLoaderIOConfiguration
    {
        private Config config;
        private IIO io;
        private TesterIOConsole tester;

        [TestInitialize]
        public void Initialize()
        {
            config = new Config();
            tester = new TesterIOConsole();
            io = tester.Mock();
        }

        [TestMethod]
        [DataFixture("io-configuration.json", "foo.com", "footoken", "x-oauth-basic")]
        [DataFixture("io-configuration.json", "bar.com", "bar-token", "oauth2")]
        [DataFixture("io-configuration.json", "baz.com", "baz-token", "oauth2")]
        [DataFixture("io-configuration.json", "aux.com", "aux-private-token", "private-token")]
        [DataFixture("io-configuration.json", "foobar.com", "foobar", "foobar-password")]
        public void TestLoad(JObject json, string expectedHost, string expectedUsername, string expectedPassword)
        {
            config.Merge(json);
            new LoaderIOConfiguration(io).Load(config);

            Assert.IsTrue(io.HasAuthentication(expectedHost));
            var (username, password) = io.GetAuthentication(expectedHost);

            Assert.AreEqual(expectedUsername, username);
            Assert.AreEqual(expectedPassword, password);
        }

        [TestMethod]
        [DataFixture("io-configuration-github-invalid.json")]
        [ExpectedException(typeof(ConfigException))]
        public void TestLoadInvalid(JObject json)
        {
            config.Merge(json);
            new LoaderIOConfiguration(io).Load(config);
        }
    }
}

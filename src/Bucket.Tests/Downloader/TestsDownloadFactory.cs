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
using Bucket.IO;
using Bucket.Package;
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bucket.Tests.Downloader
{
    [TestClass]
    public class TestsDownloadFactory
    {
        private Config config;

        [TestInitialize]
        public void Initialize()
        {
            config = new Config();
        }

        [TestMethod]
        [DataFixture("create-downloader-default.json")]
        public void TestCreateManager(JObject data)
        {
            config.Merge(data);
            var manager = DownloadFactory.CreateManager(IONull.That, config);

            var field = typeof(DownloadManager).GetField("preferSource", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(true, field.GetValue(manager));

            field = typeof(DownloadManager).GetField("preferDist", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(false, field.GetValue(manager));
        }

        [TestMethod]
        [DataFixture("create-downloader-preferences.json")]
        public void TestCreateManagerWithPreferences(JObject data)
        {
            config.Merge(data);
            var manager = DownloadFactory.CreateManager(IONull.That, config);

            var field = typeof(DownloadManager).GetField("preferences", BindingFlags.Instance | BindingFlags.NonPublic);
            var preferences = ((IEnumerable<(string Pattern, InstallationSource Prefer)>)field.GetValue(manager)).ToArray();

            Assert.IsTrue(Array.Exists(preferences, (item) => item.Pattern == "foo/bar" && item.Prefer == InstallationSource.Source));
            Assert.IsTrue(Array.Exists(preferences, (item) => item.Pattern == "foo/*" && item.Prefer == InstallationSource.Dist));
        }
    }
}

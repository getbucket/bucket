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
using Bucket.Package;
using Bucket.Package.Loader;
using Bucket.Tests.Support;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Bucket.Tests.Package.Loader
{
    [TestClass]
    public class TestsLoaderPackage
    {
        private ILoaderPackage loader;

        [TestInitialize]
        public void Initialize()
        {
            loader = new LoaderPackage();
        }

        [TestMethod]
        [DataFixture("package-full-amount.json")]
        public void TestLoadFullAmount(ConfigBucket config)
        {
            config.Dist = new ConfigResource()
            {
                Type = "zip",
                Uri = "https://github.com/foo/bar.zip",
                Shasum = "foo",
                Reference = "1.0.0",
                Mirrors = new[] { "foo" },
            };

            config.Source = new ConfigResource()
            {
                Type = "vcs",
                Uri = "https://github.com/foo/bar.git",
                Reference = "master",
                Mirrors = new[] { "bar" },
            };

            var package = loader.Load<IPackageComplete>(config);

            Assert.AreEqual("foobar", package.GetName());
            Assert.AreEqual("fooBar", package.GetNamePretty());
            Assert.AreEqual("1.0.0", package.GetVersionPretty());
            Assert.AreEqual(new DateTime(2019, 7, 9), package.GetReleaseDate());
            Assert.AreEqual("This is foobar's description.", package.GetDescription());
            Assert.AreEqual("foo, bar, foobar", string.Join(", ", package.GetKeywords()));
            Assert.AreEqual("https://github.com/foo/bar", package.GetHomepage());
            Assert.AreEqual(PackageType.Library, package.GetPackageType());
            Assert.AreEqual("http://github.com/notification", package.GetNotificationUri());

            CollectionAssert.AreEqual(
                new[]
                {
                    "foobar requires foo ^1.0",
                    "foobar requires bar ~2.2",
                }, Arr.Map(package.GetRequires(), (link) => link.ToString()));

            CollectionAssert.AreEqual(
                new[]
                {
                    "foobar requires-dev (for development) foo-dev ^1.0",
                    "foobar requires-dev (for development) bar-dev ~2.2",
                }, Arr.Map(package.GetRequiresDev(), (link) => link.ToString()));

            CollectionAssert.AreEqual(
                new[]
                {
                    "foobar replaces foo-replace 1.6",
                }, Arr.Map(package.GetReplaces(), (link) => link.ToString()));

            CollectionAssert.AreEqual(
                new[]
                {
                    "foobar provides foo-provide 1.2",
                }, Arr.Map(package.GetProvides(), (link) => link.ToString()));

            CollectionAssert.AreEqual(
                new[]
                {
                    "foobar conflicts bar-conflict 2.7",
                }, Arr.Map(package.GetConflicts(), (link) => link.ToString()));

            CollectionAssert.AreEqual(
                new[]
                {
                    "bar:bar-suggest",
                }, Arr.Map(
                    new SortedDictionary<string, string>(package.GetSuggests()),
                    (suggest) => $"{suggest.Key}:{suggest.Value}"));

            CollectionAssert.AreEqual(
                new[]
                {
                    "email:foo@support.com",
                    "issues:https://github.com/foo/bar/issues",
                }, Arr.Map(
                    new SortedDictionary<string, string>(package.GetSupport()),
                    (support) => $"{support.Key}:{support.Value}"));

            CollectionAssert.AreEqual(
                new[]
                {
                    "aux:auux",
                    "foo:bar",
                }, Arr.Map(
                    new SortedDictionary<string, string>(package.GetScripts()),
                    (script) => $"{script.Key}:{script.Value}"));

            Assert.AreEqual("foo", string.Join(", ", Arr.Map(package.GetAuthors(), (author) => author.Name)));
            Assert.AreEqual("mit", string.Join(", ", package.GetLicenses()));
            Assert.IsFalse(package.IsDeprecated);
            Assert.AreEqual("release/", string.Join(", ", package.GetArchives()));
            Assert.AreEqual("bin/", string.Join(", ", package.GetBinaries()));
            Assert.AreEqual("foo/bar", (string)package.GetExtra()["plugin"][0]);

            Assert.AreEqual("zip", package.GetDistType());
            Assert.AreEqual("https://github.com/foo/bar.zip", package.GetDistUri());
            Assert.AreEqual("1.0.0", package.GetDistReference());
            Assert.AreEqual("foo", package.GetDistShasum());
            Assert.AreEqual("foo", string.Join(", ", package.GetDistMirrors()));

            Assert.AreEqual("vcs", package.GetSourceType());
            Assert.AreEqual("https://github.com/foo/bar.git", package.GetSourceUri());
            Assert.AreEqual("master", package.GetSourceReference());
            Assert.AreEqual("bar", string.Join(", ", package.GetSourceMirrors()));
        }

        [TestMethod]
        public void TestLoadDeprecated()
        {
            var config = new ConfigBucket()
            {
                Name = "dummy",
                Version = "1.0.0",
                Deprecated = "vendor/foo",
            };

            var package = loader.Load(config);
            Assert.AreEqual("vendor/foo", package.GetReplacementPackage());
            Assert.AreEqual(true, package.IsDeprecated);

            config = new ConfigBucket()
            {
                Name = "dummy",
                Version = "1.0.0",
                Deprecated = "true",
            };

            package = loader.Load(config);
            Assert.AreEqual("true", package.GetReplacementPackage());
            Assert.AreEqual(true, package.IsDeprecated);

            config = new ConfigBucket()
            {
                Name = "dummy",
                Version = "1.0.0",
                Deprecated = string.Empty,
            };

            package = loader.Load(config);
            Assert.AreEqual(string.Empty, package.GetReplacementPackage());
            Assert.AreEqual(false, package.IsDeprecated);

            config = new ConfigBucket()
            {
                Name = "dummy",
                Version = "1.0.0",
                Deprecated = null,
            };

            package = loader.Load(config);
            Assert.AreEqual(null, package.GetReplacementPackage());
            Assert.AreEqual(false, package.IsDeprecated);
        }
    }
}

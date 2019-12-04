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
using Bucket.Package.Dumper;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.Tests.Package.Dumper
{
    [TestClass]
    public class TestsDumperPackage
    {
        private DumperPackage dumper;
        private Mock<IPackageRoot> package;

        [TestInitialize]
        public void Initialize()
        {
            dumper = new DumperPackage();
            package = new Mock<IPackageRoot>();
            package.Setup((o) => o.GetNamePretty()).Returns("foo");
            package.Setup((o) => o.GetVersionPretty()).Returns("1.2");
            package.Setup((o) => o.GetVersion()).Returns("1.2.0.0");
        }

        [TestMethod]
        public void TestRequiredProperty()
        {
            var bucket = dumper.Dump<ConfigBucket>(package.Object);
            Assert.AreEqual("foo", bucket.Name);
            Assert.AreEqual("1.2", bucket.Version);
            Assert.AreEqual("1.2.0.0", bucket.VersionNormalized);
        }

        [TestMethod]
        public void TestRootPackage()
        {
            package.Setup((o) => o.GetMinimumStability()).Returns(Stabilities.Beta);
            var bucket = dumper.Dump<ConfigBucket>(package.Object);
            Assert.AreEqual(Stabilities.Beta, bucket.MinimumStability);
        }

        [TestMethod]
        public void TestFullPackage()
        {
            package.Setup((o) => o.GetPackageType()).Returns("library");
            package.Setup((o) => o.GetReleaseDate()).Returns(new DateTime(2019, 8, 7));
            package.Setup((o) => o.GetAuthors()).Returns(new[]
            {
                new ConfigAuthor { Name = "foo" },
                new ConfigAuthor { Name = "bar" },
            });
            package.Setup((o) => o.GetHomepage()).Returns("https://example.org/");
            package.Setup((o) => o.GetDescription()).Returns("foobar");
            package.Setup((o) => o.GetKeywords()).Returns(new[] { "foo", "bar", "baz" });
            package.Setup((o) => o.GetBinaries()).Returns(new[] { "foo/bar", "foo/baz" });
            package.Setup((o) => o.GetLicenses()).Returns(new[] { "MIT" });
            package.Setup((o) => o.GetRepositories()).Returns(new[]
            {
                new ConfigRepositoryVcs { Type = "git", Uri = "https://example.org/" },
            });
            package.Setup((o) => o.GetArchives()).Returns(new[] { "foo/bar" });

            var requires = new[]
            {
                new Link("foo", "foo/bar", new Constraint("=", "1.2.0.0"), "requires", "1.2"),
                new Link("aux", "aux/bar", new Constraint("=", "1.6.0.0"), "requires", "1.6"),
            };

            package.Setup((o) => o.GetRequires()).Returns(requires);
            package.Setup((o) => o.GetRequiresDev()).Returns(new[]
            {
                new Link("foo", "foo/bar", new Constraint("=", "1.2.0.0"), "requires (for development)", "1.2"),
                new Link("aux", "aux/bar", new Constraint("=", "1.6.0.0"), "requires (for development)", "1.6"),
            });
            package.Setup((o) => o.GetProvides()).Returns(new[]
            {
                new Link("foo", "foo/bar", new Constraint("=", "1.2.0.0"), "provides", "1.2"),
                new Link("aux", "aux/bar", new Constraint("=", "1.6.0.0"), "provides", "1.6"),
            });
            package.Setup((o) => o.GetReplaces()).Returns(new[]
            {
                new Link("foo", "foo/bar", new Constraint("=", "1.2.0.0"), "replaces", "1.2"),
                new Link("aux", "aux/bar", new Constraint("=", "1.6.0.0"), "replaces", "1.6"),
            });
            package.Setup((o) => o.GetConflicts()).Returns(new[]
            {
                new Link("foo", "foo/bar", new Constraint("=", "1.2.0.0"), "conflicts", "1.2"),
                new Link("aux", "aux/bar", new Constraint("=", "1.6.0.0"), "conflicts", "1.6"),
            });
            package.Setup((o) => o.GetNotificationUri()).Returns("https://example.org/");
            package.Setup((o) => o.IsDeprecated).Returns(false);
            package.Setup((o) => o.GetMinimumStability()).Returns(Stabilities.Beta);

            // source
            package.Setup((o) => o.GetSourceType()).Returns("git");
            package.Setup((o) => o.GetSourceUri()).Returns("https://example.org/");
            package.Setup((o) => o.GetSourceReference()).Returns("dev-master");
            package.Setup((o) => o.GetSourceMirrors()).Returns(new[]
            {
                "https://example.org/",
            });

            // dist
            package.Setup((o) => o.GetDistType()).Returns("zip");
            package.Setup((o) => o.GetDistUri()).Returns("https://example.org/");
            package.Setup((o) => o.GetDistReference()).Returns("dev-master");
            package.Setup((o) => o.GetDistShasum()).Returns("1234567890123456789012345678901234567890");
            package.Setup((o) => o.GetDistMirrors()).Returns(new[]
            {
                "https://example.org/",
            });

            package.Setup((o) => o.GetSuggests()).Returns(new Dictionary<string, string>()
            {
                { "foo", "bar" },
                { "aux", "baz" },
            });

            package.Setup((o) => o.GetSupport()).Returns(new Dictionary<string, string>()
            {
                { "wiki", "https://example.org/" },
                { "email", "foo@bar.com" },
            });

            package.Setup((o) => o.GetScripts()).Returns(new Dictionary<string, string>()
            {
                { "foo", "bar" },
                { "aux", "auux" },
            });

            var bucket = dumper.Dump<ConfigBucket>(package.Object);

            Assert.AreEqual("library", bucket.PackageType);
            Assert.AreEqual(new DateTime(2019, 8, 7), bucket.ReleaseDate);

            Assert.AreEqual("foo", bucket.Authors[0].Name);
            Assert.AreEqual("bar", bucket.Authors[1].Name);

            Assert.AreEqual("https://example.org/", bucket.Homepage);
            Assert.AreEqual("foobar", bucket.Description);
            CollectionAssert.AreEqual(new[] { "bar", "baz", "foo" }, bucket.Keywords);
            CollectionAssert.AreEqual(new[] { "foo/bar", "foo/baz" }, bucket.Binaries);
            CollectionAssert.AreEqual(new[] { "MIT" }, bucket.Licenses);

            Assert.AreEqual("git", bucket.Repositories[0].Type);
            Assert.IsInstanceOfType(bucket.Repositories[0], typeof(ConfigRepositoryVcs));

            CollectionAssert.AreEqual(new[] { "foo/bar" }, bucket.Archive);

            void AssertLink(IDictionary<string, string> actual)
            {
                CollectionAssert.AreEqual(new[] { "aux/bar", "foo/bar" }, actual.Keys.ToArray());
                CollectionAssert.AreEqual(new[] { "1.6", "1.2" }, actual.Values.ToArray());
            }

            AssertLink(bucket.Requires);
            AssertLink(bucket.RequiresDev);
            AssertLink(bucket.Provides);
            AssertLink(bucket.Replaces);
            AssertLink(bucket.Conflicts);

            Assert.AreEqual("https://example.org/", bucket.NotificationUri);
            Assert.AreEqual(null, bucket.Deprecated);
            Assert.AreEqual(Stabilities.Beta, bucket.MinimumStability);

            Assert.AreEqual("git", bucket.Source.Type);
            Assert.AreEqual("https://example.org/", bucket.Source.Uri);
            Assert.AreEqual("dev-master", bucket.Source.Reference);
            Assert.AreEqual(null, bucket.Source.Shasum);
            CollectionAssert.AreEqual(new[] { "https://example.org/" }, bucket.Source.Mirrors);

            Assert.AreEqual("zip", bucket.Dist.Type);
            Assert.AreEqual("https://example.org/", bucket.Dist.Uri);
            Assert.AreEqual("dev-master", bucket.Dist.Reference);
            Assert.AreEqual("1234567890123456789012345678901234567890", bucket.Dist.Shasum);
            CollectionAssert.AreEqual(new[] { "https://example.org/" }, bucket.Dist.Mirrors);

            CollectionAssert.AreEqual(
                new[]
            {
                "aux:baz",
                "foo:bar",
            }, Arr.Map(bucket.Suggests, (item) => item.Key + ":" + item.Value));

            CollectionAssert.AreEqual(
               new[]
            {
                "email:foo@bar.com",
                "wiki:https://example.org/",
            }, Arr.Map(bucket.Support, (item) => item.Key + ":" + item.Value));

            CollectionAssert.AreEqual(
               new[]
            {
                "aux:auux",
                "foo:bar",
            }, Arr.Map(new SortedDictionary<string, string>(bucket.Scripts), (item) => item.Key + ":" + item.Value));
        }
    }
}

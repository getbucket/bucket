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

#pragma warning disable CA1054

using Bucket.Cache;
using Bucket.Configuration;
using Bucket.Downloader;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Tester;
using Bucket.Tests.Support.MockExtension;
using Bucket.Util;
using Bucket.Util.SCM;
using GameBox.Console.Exception;
using GameBox.Console.Output;
using GameBox.Console.Process;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;
using SException = System.Exception;

namespace Bucket.Tests.Downloader
{
    [TestClass]
    public class TestsDownloaderGit
    {
        private IFileSystem fileSystem;
        private string root;
        private TesterIOConsole tester;
        private IIO io;
        private string bucketPath;

        [TestInitialize]
        public void Initialize()
        {
            fileSystem = new FileSystemLocal();
            root = Helper.GetTestFolder<TestsDownloaderGit>().Replace("\\", "/", StringComparison.Ordinal);
            tester = new TesterIOConsole();
            io = tester.Mock();
            bucketPath = WinCompat("bucketPath");
            fileSystem.Delete(root);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                fileSystem.Delete(root);
            }
#pragma warning disable CA1031
            catch (SException)
#pragma warning restore CA1031
            {
                // ignore.
            }

            // reset the static version cache.
            var flag = BindingFlags.Static | BindingFlags.NonPublic;
            var versionField = typeof(Git).GetField("version", flag);
            versionField.SetValue(null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidArgumentException))]
        public void TestInstallForPackageWithoutSourceReference()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference()).Returns(() => null);

            var downloader = CreateDownloaderMock();
            downloader.Install(packageMock.Object, "/path");
        }

        [TestMethod]
        public void TestInstall()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("1234567890123456789012345678901234567890");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://example.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetSourceUri())
                .Returns("https://example.com/bucket/bucket");
            packageMock.Setup((o) => o.GetVersionPretty())
                .Returns("dev-master");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git --version");
            processMock.Setup(expectedGitCommand, "git version 1.0.0");

            expectedGitCommand = WinCompat("git clone --no-checkout 'https://example.com/bucket/bucket' 'bucketPath' && cd 'bucketPath' && git remote add bucket 'https://example.com/bucket/bucket' && git fetch bucket");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git branch -r");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            expectedGitCommand = WinCompat("git checkout master --");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            expectedGitCommand = WinCompat("git reset --hard 1234567890123456789012345678901234567890 --");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Install(packageMock.Object, "bucketPath");

            processMock.VerifyAll();
        }

        [TestMethod]
        public void TestInstallWithCache()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("1234567890123456789012345678901234567890");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://example.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetSourceUri())
                .Returns("https://example.com/bucket/bucket");
            packageMock.Setup((o) => o.GetVersionPretty())
                .Returns("dev-master");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git --version");
            processMock.Setup(expectedGitCommand, "git version 2.3.1");

            var config = new Config();
            SetupConfig(config);
            var cachePath = config.Get(Settings.CacheVcsDir) + $"/{CacheFileSystem.FormatCacheFolder("https://example.com/bucket/bucket")}/";

            expectedGitCommand = WinCompat($"git clone --mirror 'https://example.com/bucket/bucket' '{cachePath}'");
            processMock.Setup(expectedGitCommand, returnValue: () =>
            {
                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                }

                Directory.CreateDirectory(cachePath);
                return 0;
            });

            expectedGitCommand = WinCompat($"git clone --no-checkout '{cachePath}' 'bucketPath' --dissociate --reference '{cachePath}' && cd 'bucketPath' && git remote set-url origin 'https://example.com/bucket/bucket' && git remote add bucket 'https://example.com/bucket/bucket'");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git branch -r");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            expectedGitCommand = WinCompat("git checkout master --");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            expectedGitCommand = WinCompat("git reset --hard 1234567890123456789012345678901234567890 --");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            var downloader = CreateDownloaderMock(config: config, process: processMock.Object);
            downloader.Install(packageMock.Object, "bucketPath");

            processMock.VerifyAll();
        }

        [TestMethod]
        public void TestInstallUsesVariousProtocolsAndSetsPushUrlForGithub()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/mirrors/bucket", "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetSourceUri())
                .Returns("https://github.com/bucket/bucket");
            packageMock.Setup((o) => o.GetVersionPretty())
                .Returns("1.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git --version");
            processMock.Setup(expectedGitCommand, "git version 1.0.0");

            expectedGitCommand = WinCompat("git clone --no-checkout 'https://github.com/mirrors/bucket' 'bucketPath' && cd 'bucketPath' && git remote add bucket 'https://github.com/mirrors/bucket' && git fetch bucket");
            processMock.Setup(expectedGitCommand, actualError: "error 1", returnValue: () => 1);

            expectedGitCommand = WinCompat("git clone --no-checkout 'git@github.com:mirrors/bucket' 'bucketPath' && cd 'bucketPath' && git remote add bucket 'git@github.com:mirrors/bucket' && git fetch bucket");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote set-url origin 'https://github.com/bucket/bucket'");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            expectedGitCommand = WinCompat("git remote set-url --push origin 'git@github.com:bucket/bucket.git'");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            expectedGitCommand = WinCompat("git branch -r");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            expectedGitCommand = WinCompat("git checkout ref -- && git reset --hard ref --");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Install(packageMock.Object, "bucketPath");

            processMock.VerifyAll();
        }

        [TestMethod]
        [DataRow(new[] { "ssh" }, "git@github.com:bucket/bucket", "git@github.com:bucket/bucket.git")]
        [DataRow(new[] { "https", "ssh", "git" }, "https://github.com/bucket/bucket", "git@github.com:bucket/bucket.git")]
        [DataRow(new[] { "https" }, "https://github.com/bucket/bucket", "https://github.com/bucket/bucket.git")]
        public void TestInstallAndSetPushUrlUseCustomVariousProtocolsForGithub(string[] protocols, string uri, string pushUri)
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetSourceUri())
                .Returns("https://github.com/bucket/bucket");
            packageMock.Setup((o) => o.GetVersionPretty())
                .Returns("1.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git --version");
            processMock.Setup(expectedGitCommand, "git version 1.0.0");

            expectedGitCommand = WinCompat($"git clone --no-checkout '{uri}' 'bucketPath' && cd 'bucketPath' && git remote add bucket '{uri}' && git fetch bucket");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat($"git remote set-url --push origin '{pushUri}'");
            processMock.Setup(expectedGitCommand, expectedCwd: bucketPath);

            var config = new Config();
            var merged = new JObject
            {
                ["config"] = new JObject(),
            };
            merged["config"][Settings.GithubProtocols] = new JArray(protocols);
            config.Merge(merged);

            var downloader = CreateDownloaderMock(config: config, process: processMock.Object);
            downloader.Install(packageMock.Object, "bucketPath");

            processMock.VerifyAll();
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "Failed to execute git clone")]
        public void TestDownloadThrowsRuntimeExceptionIfGitCommandFails()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://example.com/bucket/bucket" });

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git --version");
            processMock.Setup(expectedGitCommand, "git version 1.0.0");

            expectedGitCommand = WinCompat("git clone --no-checkout 'https://example.com/bucket/bucket' 'bucketPath' && cd 'bucketPath' && git remote add bucket 'https://example.com/bucket/bucket' && git fetch bucket");
            processMock.Setup(expectedGitCommand, returnValue: () => 1);

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Install(packageMock.Object, "bucketPath");
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidArgumentException), "missing reference information.")]
        public void TestUpdateforPackageWithoutSourceReference()
        {
            var packaginitialPackageMock = new Mock<IPackage>();
            var sourceinitialPackageMock = new Mock<IPackage>();
            sourceinitialPackageMock.Setup((o) => o.GetSourceReference())
                .Returns(() => null);

            var downloader = CreateDownloaderMock();
            downloader.Update(packaginitialPackageMock.Object, sourceinitialPackageMock.Object, "/path");
        }

        [TestMethod]
        public void TestUpdate()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git show-ref --head -d");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git status --porcelain --untracked-files=no");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote -v");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote set-url bucket 'https://github.com/bucket/bucket' && git rev-parse --quiet --verify 'ref^{commit}' || (git fetch bucket && git fetch --tags bucket)");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git branch -r");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git checkout ref -- && git reset --hard ref --");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(packageMock.Object, packageMock.Object, root);

            processMock.VerifyAll();
        }

        [TestMethod]
        public void TestUpdateWithNewRepositoryUri()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetSourceUri())
                .Returns("https://github.com/bucket/bucket");
            packageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git show-ref --head -d");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git status --porcelain --untracked-files=no");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote -v");
            processMock.Setup(expectedGitCommand, actualOut:
@"origin https://github.com/old/url (fetch)
origin https://github.com/old/url (push)
bucket https://github.com/old/url (fetch)
bucket https://github.com/old/url (push)
");

            expectedGitCommand = WinCompat("git remote set-url bucket 'https://github.com/bucket/bucket' && git rev-parse --quiet --verify 'ref^{commit}' || (git fetch bucket && git fetch --tags bucket)");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git branch -r");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git checkout ref -- && git reset --hard ref --");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git remote set-url origin 'https://github.com/bucket/bucket'");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git remote set-url --push origin 'git@github.com:bucket/bucket.git'");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(packageMock.Object, packageMock.Object, root);

            processMock.VerifyAll();
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "Failed to clone")]
        public void TestUpdateThrowsRuntimeExceptionIfGitCommandFails()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git show-ref --head -d");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git status --porcelain --untracked-files=no");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote -v");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote set-url bucket 'https://github.com/bucket/bucket' && git rev-parse --quiet --verify 'ref^{commit}' || (git fetch bucket && git fetch --tags bucket)");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root), returnValue: () => 1);

            expectedGitCommand = WinCompat("git remote set-url bucket 'git@github.com:bucket/bucket' && git rev-parse --quiet --verify 'ref^{commit}' || (git fetch bucket && git fetch --tags bucket)");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root), returnValue: () => 1);

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(packageMock.Object, packageMock.Object, root);
        }

        [TestMethod]
        public void TestUpdateDoesntThrowsRuntimeExceptionIfGitCommandFailsAtFirstButIsAbleToRecover()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "/foo/bar", "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git show-ref --head -d");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git status --porcelain --untracked-files=no");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote -v");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote set-url bucket '' && git rev-parse --quiet --verify 'ref^{commit}' || (git fetch bucket && git fetch --tags bucket)");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root), returnValue: () => 1);

            expectedGitCommand = WinCompat("git --version");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git remote set-url bucket 'https://github.com/bucket/bucket' && git rev-parse --quiet --verify 'ref^{commit}' || (git fetch bucket && git fetch --tags bucket)");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root), returnValue: () => 0);

            expectedGitCommand = WinCompat("git branch -r");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git checkout ref -- && git reset --hard ref --");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(packageMock.Object, packageMock.Object, root);

            processMock.VerifyAll();
            processMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TestDowngradeShowsMessage()
        {
            var oldPackageMock = new Mock<IPackage>();
            oldPackageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            oldPackageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "/foo/bar", "https://github.com/bucket/bucket" });
            oldPackageMock.Setup((o) => o.GetVersion())
                .Returns("1.2.0.0");
            oldPackageMock.Setup((o) => o.GetVersionPretty())
               .Returns("1.2.0");

            var newPackageMock = new Mock<IPackage>();
            newPackageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            newPackageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            newPackageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");
            newPackageMock.Setup((o) => o.GetVersionPretty())
               .Returns("1.0.0");

            var processMock = new Mock<IProcessExecutor>();
            processMock.Setup(null, returnValue: () => 0);

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(oldPackageMock.Object, newPackageMock.Object, root);

            StringAssert.Contains(tester.GetDisplay(), "Downgrading");
        }

        [TestMethod]
        public void TestNotUsingDowngradingWithReferences()
        {
            var oldPackageMock = new Mock<IPackage>();
            oldPackageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            oldPackageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "/foo/bar", "https://github.com/bucket/bucket" });
            oldPackageMock.Setup((o) => o.GetVersion())
                .Returns("dev-ref-1");

            var newPackageMock = new Mock<IPackage>();
            newPackageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            newPackageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            newPackageMock.Setup((o) => o.GetVersion())
                .Returns("dev-ref-2");

            var processMock = new Mock<IProcessExecutor>();
            processMock.Setup(null, returnValue: () => 0);

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(oldPackageMock.Object, newPackageMock.Object, root);

            StringAssert.Contains(tester.GetDisplay(), "Updating");
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(RuntimeException), "has unpushed changes on the current branch:")]
        public void TestUpdateHasUnpushedChangesThrowException()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git show-ref --head -d");
            var stdout =
@"c5bd1552fedbd7cbfc981ab2b1d825891993e345 HEAD
c5bd1552fedbd7cbfc981ab2b1d825891993e345 refs/heads/foo
e59430132f0a258a6d68c3731b94c2887a87154f refs/heads/master
c5bd1552fedbd7cbfc981ab2b1d825891993e345 refs/remotes/origin/foo
e59430132f0a258a6d68c3731b94c2887a87154f refs/remotes/origin/master
";
            processMock.Setup(expectedGitCommand, actualOut: stdout, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git diff --name-status origin/foo...foo --");
            stdout =
@"M       .gitignore
M       .gitlab-ci.yml
M       CHANGELOG.md
M       README.md";
            processMock.Setup(expectedGitCommand, actualOut: stdout, expectedCwd: WinCompat(root));

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(packageMock.Object, packageMock.Object, root);

            processMock.VerifyAll();
        }

        [TestMethod]
        public void TestHasLocalChangeInteractWithUser()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git show-ref --head -d");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git status --porcelain --untracked-files=no");
            var stdout =
@" M 1-foo/bar.cs
 M 2-foo/baz.cs
 M 3-foobar.json
 M 4-foo/aux/bar.cs
 M 5-foobar.json
 M 6-foo/aux/bar.cs
 M 7-foo/aux/bar.cs
 M 8-foo/aux/bar.cs
 M 9-foo/aux/bar.cs
 M 10-foo/aux/bar.cs
 M 11-foo/aux/bar.cs
 M 12-foo/aux/bar.cs
";
            processMock.Setup(expectedGitCommand, actualOut: stdout, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git stash --include-untracked");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git diff HEAD");
            stdout =
@"index 8ba5bbe..9cda3f0 100644
--- a/README.md
+++ b/README.md
@@ -1,5 +1,6 @@
 foobarbaz
 foobarbaz

+foo
 foobarbaz
 foobarbaz
";
            processMock.Setup(expectedGitCommand, actualOut: stdout, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git stash pop");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git log ref..ref --pretty=format:'%h - %an: %s'");
            stdout =
@"c5bd155 - foo: bar
ccf0f2f - bar: baz
ac027b9 - foo: baz";
            processMock.Setup(expectedGitCommand, actualOut: stdout, expectedCwd: WinCompat(root));

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            tester.SetInputs(new[] { "v", "d", "?", "s" });
            io = tester.Mock(AbstractTester.OptionVerbosity(OutputOptions.VerbosityVerbose));

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(packageMock.Object, packageMock.Object, root);

            var display = tester.GetDisplay();

            StringAssert.Contains(display, "The package has modified files");
            StringAssert.Contains(display, "2 more files modified, choose \"v\" to view the full list.");
            StringAssert.Contains(display, "Discard changes [y,n,v,d,s,?,h]?");
            StringAssert.Contains(display, "stash changes and try to reapply them after the update.");
            StringAssert.Contains(display, "Re-applying stashed changes");
            StringAssert.Contains(display, "Pulling in changes:");

            processMock.VerifyAll();
        }

        [TestMethod]
        public void TestHasLocalChangeDisabledInteractWithUser()
        {
            var config = new Config();
            var merged = new JObject
            {
                ["config"] = new JObject(),
            };
            merged["config"][Settings.DiscardChanges] = true;
            config.Merge(merged);

            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("ref");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git show-ref --head -d");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git status --porcelain --untracked-files=no");
            var stdout =
@" M foo/bar.cs
 M foo/baz.cs
";
            processMock.Setup(expectedGitCommand, actualOut: stdout, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git reset --hard");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            io = tester.Mock(
                AbstractTester.OptionVerbosity(OutputOptions.VerbosityVerbose),
                AbstractTester.Interactive(false));

            var downloader = CreateDownloaderMock(config: config, process: processMock.Object);
            downloader.Update(packageMock.Object, packageMock.Object, root);

            processMock.VerifyAll();
        }

        [TestMethod]
        public void TestReferenceIsBranchAndFetchItUseRemoteName()
        {
            var packageMock = new Mock<IPackage>();
            packageMock.Setup((o) => o.GetSourceReference())
                .Returns("feature/bucket-foo");
            packageMock.Setup((o) => o.GetSourceUris())
                .Returns(new[] { "https://github.com/bucket/bucket" });
            packageMock.Setup((o) => o.GetVersion())
                .Returns("1.0.0.0");
            packageMock.Setup((o) => o.GetVersionPretty())
                .Returns("1.0.0");

            var processMock = new Mock<IProcessExecutor>();
            var expectedGitCommand = WinCompat("git branch -r");
            var stdout =
@"  origin/0.1
  origin/0.2
  origin/master
  bucket/master
  bucket/feature/bucket-foo";
            processMock.Setup(expectedGitCommand, actualOut: stdout, expectedCwd: WinCompat(root));

            expectedGitCommand = WinCompat("git checkout -B 1.0.0 'bucket/feature/bucket-foo' -- && git reset --hard 'bucket/feature/bucket-foo' --");
            processMock.Setup(expectedGitCommand, expectedCwd: WinCompat(root));

            if (!Directory.Exists(root + "/.git"))
            {
                Directory.CreateDirectory(root + "/.git");
            }

            var downloader = CreateDownloaderMock(process: processMock.Object);
            downloader.Update(packageMock.Object, packageMock.Object, root);

            processMock.VerifyAll();
        }

        [TestMethod]
        public void TestRemove()
        {
            var packageMock = new Mock<IPackage>();

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup((o) => o.Delete("bucketPath"));
            fileSystemMock.Setup((o) => o.Exists("bucketPath/.git", FileSystemOptions.Directory)).Returns(true);
            fileSystemMock.As<IReportPath>().Setup((o) => o.ApplyRootPath("bucketPath")).Returns("bucketPath");

            var processMock = new Mock<IProcessExecutor>();

            var expectedGitCommand = WinCompat("git show-ref --head -d");
            processMock.Setup(expectedGitCommand);

            expectedGitCommand = WinCompat("git status --porcelain --untracked-files=no");
            processMock.Setup(expectedGitCommand);

            var downloader = CreateDownloaderMock(process: processMock.Object, fileSystem: fileSystemMock.Object);
            downloader.Remove(packageMock.Object, "bucketPath");

            processMock.VerifyAll();
        }

        [TestMethod]
        public void TestGetInstallationSource()
        {
            var downloader = CreateDownloaderMock();
            Assert.AreEqual(InstallationSource.Source, downloader.InstallationSource);
        }

        protected static Config SetupConfig(Config config)
        {
            config = config ?? new Config();

            if (config.Has(Settings.Home))
            {
                return config;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), "bucket-test", Path.GetRandomFileName()).Replace("\\", "/", StringComparison.Ordinal);
            var merged = new JObject()
            {
                { "config", new JObject() },
            };

            merged["config"]["home"] = tempPath;
            config.Merge(merged);

            return config;
        }

        protected DownloaderGit CreateDownloaderMock(IIO io = null, Config config = null, IProcessExecutor process = null, IFileSystem fileSystem = null)
        {
            io = io ?? this.io;
            config = SetupConfig(config);
            process = process ?? new BucketProcessExecutor();
            fileSystem = fileSystem ?? this.fileSystem;

            return new DownloaderGit(io, config, process, fileSystem);
        }

        private string WinCompat(string command)
        {
            if (!Platform.IsWindows)
            {
                return command;
            }

            command = command.Replace("cd ", "cd /D ", StringComparison.Ordinal);
            command = command.Replace("bucketPath", Environment.CurrentDirectory.Replace("\\", "/", StringComparison.Ordinal) + "/bucketPath", StringComparison.Ordinal);
            command = command.Replace("'", "\"", StringComparison.Ordinal);
            return command;
        }
    }
}

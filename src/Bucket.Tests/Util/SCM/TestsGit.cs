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

#pragma warning disable CA1822

using Bucket.Configuration;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Tests.Support;
using Bucket.Util;
using Bucket.Util.SCM;
using GameBox.Console.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Util.SCM
{
    [TestClass]
    public class TestsGit
    {
        private static string root;
        private Git git;
        private Config config;
        private IProcessExecutor process;
        private BaseFileSystem fileSystem;

        [TestInitialize]
        public void Initialize()
        {
            root = Helper.GetTestFolder<TestsGit>();
            fileSystem = new FileSystemLocal(root);
            process = new BucketProcessExecutor();
            config = new Config();

            fileSystem.Delete();

            git = new Git(IONull.That, config, process, fileSystem);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                fileSystem.Delete();
            }
            catch (System.IO.IOException)
            {
                // ignore.
            }
        }

        [TestMethodOnline]
        [Timeout(300000)]
        [DataRow("https://gitlab.com/pages/gitbook.git")]
        [DataRow("https://gitlab.com/pages/gitbook")]
        [DataRow("https://gitlab.com/pages/hugo")]
        [DataRow("https://github.com/remy/mit-license.git")]

        // todo: wait implement auth.
        // [DataRow("git@gitlab.com:pages/gitbook.git")]
        // [DataRow("git@github.com:remy/mit-license.git")]
        public void TestSyncMirror(string path)
        {
            Assert.AreEqual(true, git.SyncMirror(path));
        }

        [TestMethodOnline]
        [Timeout(300000)]
        [DataRow("https://gitlab.com/pages/gitbook.git", "HEAD")]
        public void TestFetchReferenceOrSyncMirror(string path, string reference)
        {
            Assert.AreEqual(false, git.FetchReferenceOrSyncMirror(path, reference));
            Assert.AreEqual(true, git.FetchReferenceOrSyncMirror(path, reference));
        }

        [TestMethod]
        public void TestGetVersion()
        {
            // Test not throw exception.
            git.GetVersion();
        }
    }
}

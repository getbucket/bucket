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
using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Tester;
using Bucket.Tests.Support.MockExtension;
using GameBox.Console;
using GameBox.Console.EventDispatcher;
using GameBox.Console.Output;
using GameBox.Console.Process;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using BEventDispatcher = Bucket.EventDispatcher.EventDispatcher;
using SException = System.Exception;

namespace Bucket.Tests.EventDispatcher
{
    [TestClass]
    public class TestsEventDispatcher
    {
        private IEventDispatcher dispatcher;
        private Bucket bucket;
        private TesterIOConsole tester;
        private IIO io;
        private Mock<IPackageRoot> package;
        private Mock<EventArgs> eventArgs;
        private Config config;
        private Mock<IProcessExecutor> process;
        private Mock<IFileSystem> fileSystem;

        [TestInitialize]
        public void Initialize()
        {
            bucket = new Bucket();
            tester = new TesterIOConsole();
            eventArgs = new Mock<EventArgs>();
            fileSystem = new Mock<IFileSystem>();
            process = new Mock<IProcessExecutor>();
            io = tester.Mock(
                AbstractTester.OptionVerbosity(OutputOptions.VerbosityVeryVerbose),
                AbstractTester.OptionStdErrorSeparately(true));
            dispatcher = new BEventDispatcher(bucket, io, process.Object, fileSystem.Object);
            package = new Mock<IPackageRoot>();
            config = new Config();
            bucket.SetPackage(package.Object);
            bucket.SetConfig(config);
        }

        [TestMethod]
        public void TestAddListeners()
        {
            var listener = new Mock<EventHandler>();
            dispatcher.AddListener("foo", listener.Object);
            dispatcher.Dispatch("foo", this, eventArgs.Object);
            listener.Verify((o) => o.Invoke(this, eventArgs.Object), Times.Once);
        }

        [TestMethod]
        public void TestDispatchNullArgs()
        {
            var listener = new Mock<EventHandler>();
            dispatcher.AddListener("foo", listener.Object);
            dispatcher.Dispatch("foo", this);
            listener.Verify(
                (o) => o.Invoke(this, It.Is<BucketEventArgs>(value => value.Name == "foo")), Times.Once);
        }

        [TestMethod]
        public void TestDispatchScript()
        {
            package.Setup((o) => o.GetScripts()).Returns(new Dictionary<string, string>()
            {
                { "foo", "echo foo" },
                { "bar", "echo bar" },
            });

            var listener = new Mock<EventHandler>();
            dispatcher.AddListener("foo", listener.Object);
            dispatcher.Dispatch("foo", this);

            Assert.AreEqual("> echo foo", tester.GetDisplayError().Trim());
            listener.Verify(
                (o) => o.Invoke(this, It.Is<BucketEventArgs>(value => value.Name == "foo")), Times.Once);
            process.Verify(
                (o) => o.Execute("echo foo", out It.Ref<string[]>.IsAny, out It.Ref<string[]>.IsAny, null));
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(ScriptExecutionException), "Error Output: foo error")]
        public void TestDispatchScriptReturnError()
        {
            package.Setup((o) => o.GetScripts()).Returns(new Dictionary<string, string>()
            {
                { "foo", "echo foo" },
                { "bar", "echo bar" },
            });

            process.Setup("echo foo", actualError: "foo error", returnValue: () => ExitCodes.GeneralException);
            var listener = new Mock<EventHandler>();
            dispatcher.AddListener("foo", listener.Object);

            try
            {
                dispatcher.Dispatch("foo", this);
            }
            catch (SException)
            {
                StringAssert.Contains(
                    tester.GetDisplayError(),
                    "Script \"echo foo\" handling the \"foo\" event returned with error code: 1");
                throw;
            }
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(
            typeof(RuntimeException),
            "Circular call to script handler \"foo\" detected. Event stack [foo].")]
        public void TestDispatchCircularCall()
        {
            var listener = new Mock<EventHandler>();
            listener.Setup((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()))
                .Callback(() =>
            {
                dispatcher.Dispatch("foo", this);
            });

            dispatcher.AddListener("foo", listener.Object);
            dispatcher.Dispatch("foo", this);
        }

        [TestMethod]
        public void TestDispatchWithLocalBinaries()
        {
            package.Setup((o) => o.GetScripts()).Returns(new Dictionary<string, string>()
            {
                { "foo", "foo.bat" },
            });

            package.Setup((o) => o.GetBinaries()).Returns(new[] { "bin/foo.bat" });

            dispatcher.Dispatch("foo", this, new BucketEventArgs("foo", new[] { "arg1", "arg2" }));
            process.Verify(
                (o) => o.Execute("call bin/foo.bat arg1 arg2", out It.Ref<string[]>.IsAny, out It.Ref<string[]>.IsAny, null));
        }

        [TestMethod]
        public void TestRemoveListener()
        {
            var listener = new Mock<EventHandler>();
            dispatcher.AddListener("foo", listener.Object);
            dispatcher.Dispatch("foo", this, eventArgs.Object);
            listener.Verify((o) => o.Invoke(this, eventArgs.Object), Times.Once);

            dispatcher.RemoveListener("foo", listener.Object);
            dispatcher.Dispatch("foo", this);
            listener.Verify((o) => o.Invoke(this, eventArgs.Object), Times.Once);
        }

        [TestMethod]
        public void TestRemoveListenerNotInfluencesScripts()
        {
            var listener = new Mock<EventHandler>();
            dispatcher.AddListener("foo", listener.Object);
            package.Setup((o) => o.GetScripts()).Returns(new Dictionary<string, string>()
            {
                { "foo", "echo foo" },
            });

            dispatcher.Dispatch("foo", this, eventArgs.Object);
            listener.Verify((o) => o.Invoke(this, eventArgs.Object), Times.Once);
            process.Verify(
               (o) => o.Execute("echo foo", out It.Ref<string[]>.IsAny, out It.Ref<string[]>.IsAny, null), Times.Once);

            dispatcher.RemoveListener("foo", listener.Object);

            dispatcher.Dispatch("foo", this, eventArgs.Object);
            listener.Verify((o) => o.Invoke(this, eventArgs.Object), Times.Once);
            process.Verify(
               (o) => o.Execute("echo foo", out It.Ref<string[]>.IsAny, out It.Ref<string[]>.IsAny, null), Times.Exactly(2));
        }

        [TestMethod]
        public void TestRemoveAllListener()
        {
            var fooFirst = new Mock<EventHandler>();
            var fooSecond = new Mock<EventHandler>();
            var bar = new Mock<EventHandler>();
            dispatcher.AddListener("foo", fooFirst.Object);
            dispatcher.AddListener("foo", fooSecond.Object);
            dispatcher.AddListener("bar", bar.Object);

            dispatcher.Dispatch("foo", this);
            dispatcher.Dispatch("bar", this);

            fooFirst.Verify((o) => o.Invoke(this, It.IsAny<EventArgs>()), Times.Once);
            fooSecond.Verify((o) => o.Invoke(this, It.IsAny<EventArgs>()), Times.Once);
            bar.Verify((o) => o.Invoke(this, It.IsAny<EventArgs>()), Times.Once);

            dispatcher.RemoveListener("foo");
            dispatcher.Dispatch("foo", this);
            dispatcher.Dispatch("bar", this);

            fooFirst.Verify((o) => o.Invoke(this, It.IsAny<EventArgs>()), Times.Once);
            fooSecond.Verify((o) => o.Invoke(this, It.IsAny<EventArgs>()), Times.Once);
            bar.Verify((o) => o.Invoke(this, It.IsAny<EventArgs>()), Times.Exactly(2));
        }

        [TestMethod]
        public void TestHasListener()
        {
            var listener = new Mock<EventHandler>();
            dispatcher.AddListener("foo", listener.Object);

            Assert.IsTrue(dispatcher.HasListener("foo"));
            Assert.IsFalse(dispatcher.HasListener("bar"));
        }

        [TestMethod]
        public void TestHasListenerWithScripts()
        {
            package.Setup((o) => o.GetScripts()).Returns(new Dictionary<string, string>()
            {
                { "foo", "echo foo" },
            });

            Assert.IsTrue(dispatcher.HasListener("foo"));
            Assert.IsFalse(dispatcher.HasListener("bar"));
        }

        [TestMethod]
        public void TestAddRemoveSubscriber()
        {
            var foo = new Mock<EventHandler>();
            var bar = new Mock<EventHandler>();
            var eventSubscriber = new Mock<IEventSubscriber>();
            eventSubscriber.Setup((o) => o.GetSubscribedEvents())
                .Returns(new Dictionary<string, EventHandler>()
                {
                    { "foo", foo.Object },
                    { "bar", bar.Object },
                });

            dispatcher.AddSubscriber(eventSubscriber.Object);
            dispatcher.Dispatch("foo", this);

            foo.Verify((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()), Times.Once);
            bar.Verify((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()), Times.Never);

            dispatcher.RemoveSubscriber(eventSubscriber.Object);
            dispatcher.Dispatch("bar", this);

            foo.Verify((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()), Times.Once);
            bar.Verify((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()), Times.Never);
        }

        [TestMethod]
        public void TestPropagationStopped()
        {
            var fooFirst = new Mock<EventHandler>();
            var fooSecond = new Mock<EventHandler>();
            var args = new BucketEventArgs("foo");

            dispatcher.AddListener("foo", fooFirst.Object);
            dispatcher.AddListener("foo", fooSecond.Object);

            dispatcher.Dispatch("foo", this, args);
            fooFirst.Verify((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()), Times.Once);
            fooSecond.Verify((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()), Times.Once);

            fooFirst.Setup((o) => o.Invoke(this, It.IsAny<BucketEventArgs>())).Callback(() => args.StopPropagation());
            dispatcher.Dispatch("foo", this, args);
            fooFirst.Verify((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()), Times.Exactly(2));
            fooSecond.Verify((o) => o.Invoke(this, It.IsAny<BucketEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void TestAddBinariesFolderInPathVariable()
        {
            fileSystem.Setup((o) => o.Exists(It.IsAny<string>(), FileSystemOptions.Directory)).Returns(true);
            var listener = new Mock<EventHandler>();
            dispatcher.AddListener("foo", listener.Object);
            dispatcher.Dispatch("foo", this, eventArgs.Object);

            // todo: File path needs to be optimized.
            string expected = config.Get(Settings.BinDir);
            expected = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, expected));
            var path = Environment.GetEnvironmentVariable("PATH");
            StringAssert.Contains(path, expected);
        }
    }
}

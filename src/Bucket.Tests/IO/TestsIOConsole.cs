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

using Bucket.IO;
using Bucket.Tester;
using Bucket.Util;
using GameBox.Console.Formatter;
using GameBox.Console.Helper;
using GameBox.Console.Input;
using GameBox.Console.Output;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Diagnostics;

namespace Bucket.Tests.IO
{
    [TestClass]
    public class TestsIOConsole
    {
        [TestMethod]
        public void TestIsInteractive()
        {
            var mockInput = new Mock<IInput>();
            mockInput.SetupSequence((o) => o.IsInteractive)
                    .Returns(true)
                    .Returns(false);

            var mockOutput = new Mock<IOutput>();

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);

            Assert.IsTrue(ioConsole.IsInteractive);
            Assert.IsFalse(ioConsole.IsInteractive);
        }

        [TestMethod]
        public void TestIsDecorated()
        {
            var mockInput = new Mock<IInput>();
            var mockOutput = new Mock<IOutput>();
            mockOutput.SetupSequence((o) => o.IsDecorated)
                .Returns(true)
                .Returns(false);

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);
            Assert.IsTrue(ioConsole.IsDecorated);
            Assert.IsFalse(ioConsole.IsDecorated);
        }

        [TestMethod]
        public void TestIsDebug()
        {
            var mockInput = new Mock<IInput>();
            var mockOutput = new Mock<IOutput>();
            mockOutput.SetupSequence((o) => o.IsDebug)
                .Returns(true)
                .Returns(false);

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);
            Assert.IsTrue(ioConsole.IsDebug);
            Assert.IsFalse(ioConsole.IsDebug);
        }

        [TestMethod]
        public void TestIsVerbose()
        {
            var mockInput = new Mock<IInput>();
            var mockOutput = new Mock<IOutput>();
            mockOutput.SetupSequence((o) => o.IsVerbose)
                .Returns(true)
                .Returns(false);

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);
            Assert.IsTrue(ioConsole.IsVerbose);
            Assert.IsFalse(ioConsole.IsVerbose);
        }

        [TestMethod]
        public void TestIsVeryVerbose()
        {
            var mockInput = new Mock<IInput>();
            var mockOutput = new Mock<IOutput>();
            mockOutput.SetupSequence((o) => o.IsVeryVerbose)
                .Returns(true)
                .Returns(false);

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);
            Assert.IsTrue(ioConsole.IsVeryVerbose);
            Assert.IsFalse(ioConsole.IsVeryVerbose);
        }

        [TestMethod]
        public void TestWrite()
        {
            var mockInput = new Mock<IInput>();
            var mockOutput = new Mock<IOutput>();

            mockOutput.Setup((o) => o.Options)
                .Returns(OutputOptions.VerbosityNormal);

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);
            ioConsole.Write("foo bar menghanyu zzz", true);

            mockOutput.Verify((o) => o.Write("foo bar menghanyu zzz", true, OutputOptions.VerbosityNormal));
        }

        [TestMethod]
        public void TestWriteError()
        {
            var mockInput = new Mock<IInput>();
            var mockOutput = new Mock<IOutputConsole>();

            mockOutput.Setup((o) => o.Options)
                .Returns(OutputOptions.VerbosityNormal);

            mockOutput.Setup((o) => o.GetErrorOutput())
                .Returns(mockOutput.Object);

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);
            ioConsole.WriteError("foo bar menghanyu zzz", true);

            mockOutput.Verify((o) => o.Write("foo bar menghanyu zzz", true, OutputOptions.VerbosityNormal));
        }

        [TestMethod]
        public void TestWriteWithDebugging()
        {
            var mockInput = new Mock<IInput>();
            var mockOutput = new Mock<IOutput>();

            mockOutput.Setup((o) => o.Options)
                .Returns(OutputOptions.VerbosityNormal);

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);
            var stopwatch = new Stopwatch();
            ioConsole.SetDebugging(stopwatch);
            stopwatch.Start();
            ioConsole.Write("hello world", true);

            mockOutput.Verify((o) => o.Write(It.IsRegex(@"^\[(.*)/(.*)\] hello world$"), true, OutputOptions.VerbosityNormal));
        }

        [TestMethod]
        public void TestOverwrite()
        {
            var mockInput = new Mock<IInput>();
            var mockOutput = new Mock<IOutput>();

            mockOutput.Setup((o) => o.Options)
                .Returns(OutputOptions.VerbosityNormal);

            mockOutput.Setup((o) => o.Formatter)
                .Returns(new OutputFormatter());

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);

            ioConsole.Write("something (<question>strlen = 23</question>)");
            ioConsole.Overwrite("shorter (<comment>12</comment>)", false);
            ioConsole.Overwrite("something longer than initial (<info>34</info>)", false);

            mockOutput.Verify((o) => o.Write("something (<question>strlen = 23</question>)", true, OutputOptions.VerbosityNormal), Times.Once());
            mockOutput.Verify((o) => o.Write(Str.Repeat("\x08", 23), false, OutputOptions.VerbosityNormal), Times.Once());
            mockOutput.Verify((o) => o.Write("shorter (<comment>12</comment>)", false, OutputOptions.VerbosityNormal), Times.Once());
            mockOutput.Verify((o) => o.Write(Str.Repeat(11), false, OutputOptions.VerbosityNormal), Times.Once());
            mockOutput.Verify((o) => o.Write(Str.Repeat("\x08", 11), false, OutputOptions.VerbosityNormal), Times.Once());
            mockOutput.Verify((o) => o.Write(Str.Repeat("\x08", 12), false, OutputOptions.VerbosityNormal), Times.Once());
            mockOutput.Verify((o) => o.Write("something longer than initial (<info>34</info>)", false, OutputOptions.VerbosityNormal), Times.Once());
        }

        [TestMethod]
        public void TestNonSatisfiedVerbosityWriteNotRecordLastMessage()
        {
            var mockInput = new Mock<IInput>();
            var mockErrorOutput = new Mock<IOutput>();
            var mockOutput = new Mock<IOutputConsole>();

            mockOutput.Setup((o) => o.Options)
                .Returns(OutputOptions.VerbosityNormal | OutputOptions.OutputPlain);

            mockOutput.Setup((o) => o.GetErrorOutput())
                .Returns(mockErrorOutput.Object);

            var ioConsole = new IOConsole(mockInput.Object, mockOutput.Object);
            ioConsole.WriteError("foo", false);
            ioConsole.WriteError("foo bar baz", true, Verbosities.Debug);
            ioConsole.OverwriteError("bar", false);

            mockErrorOutput.Verify((o) => o.Write("foo", false, OutputOptions.VerbosityNormal), Times.Once());
            mockErrorOutput.Verify((o) => o.Write("foo bar baz", true, OutputOptions.VerbosityDebug), Times.Once());
            mockErrorOutput.Verify((o) => o.Write("\x08\x08\x08", false, OutputOptions.VerbosityNormal), Times.Once());
            mockErrorOutput.Verify((o) => o.Write("bar", false, OutputOptions.VerbosityNormal), Times.Once());
        }

        [TestMethod]
        public void TestAsk()
        {
            var tester = new TesterIOConsole();
            tester.SetInputs(new[] { "menghanyu" });
            var ioConsole = tester.Mock();
            var answer = ioConsole.Ask("What's your name?", "miaomiao");
            Assert.AreEqual("menghanyu", (string)answer);
            Assert.AreEqual("What's your name?", tester.GetDisplay());
        }

        [TestMethod]
        public void TestAskConfirmation()
        {
            var tester = new TesterIOConsole();
            tester.SetInputs(new[] { "yes" });

            var ioConsole = tester.Mock();
            var answer = ioConsole.AskConfirmation("Do you like me?", false);
            Assert.AreEqual(true, answer);
        }

        [TestMethod]
        public void TestAskChoice()
        {
            var tester = new TesterIOConsole();
            tester.SetInputs(new[] { "1", "menghanyu" });

            var choices = new[]
            {
                "miaomiao",
                "menghanyu",
                "tutu",
            };

            var ioConsole = tester.Mock();
            var answer = ioConsole.AskChoice("What's your name?", choices, "tutu");
            Assert.AreEqual(1, answer);

            answer = ioConsole.AskChoice("What's your name?", choices, "miaomiao");
            Assert.AreEqual(1, answer);
        }

        [TestMethod]
        public void TestAskChoiceMult()
        {
            var tester = new TesterIOConsole();
            tester.SetInputs(new[] { "0,tutu", "menghanyu" });

            var choices = new[]
            {
                "miaomiao",
                "menghanyu",
                "tutu",
            };

            var ioConsole = tester.Mock();
            var answer = ioConsole.AskChoiceMult("What's your name?", choices, "tutu");
            CollectionAssert.AreEqual(new[] { 0, 2 }, answer);

            answer = ioConsole.AskChoiceMult("What's your name?", choices, "miaomiao");
            CollectionAssert.AreEqual(new[] { 1 }, answer);
        }

        [TestMethod]
        public void TestAskAndValidate()
        {
            var tester = new TesterIOConsole();
            tester.SetInputs(new[] { "menghanyu", "miaomiao" });

            var ioConsole = tester.Mock();
            var answer = ioConsole.AskAndValidate("What's your name?", (value) =>
            {
                if (value == "menghanyu")
                {
                    throw new System.Exception("Not allowed to be called menghanyu");
                }

                return value;
            });

            Assert.AreEqual("miaomiao", (string)answer);
        }

        [TestMethod]
        public void TestSetHelper()
        {
            var tester = new TesterIOConsole();
            tester.SetInputs(new[] { "menghanyu" });

            var ioConsole = tester.Mock();
            ioConsole.SetHelperQuestion(new HelperQuestionDefault());
            var answer = ioConsole.Ask("What's your name?", "miaomiao");
            Assert.AreEqual("menghanyu", (string)answer);
            Assert.AreEqual(
@" What's your name? [miaomiao]:
 > ", tester.GetDisplay());
        }

        [TestMethod]
        public void TestSetGetAuthentication()
        {
            var tester = new TesterIOConsole();
            var ioConsole = tester.Mock();

            ioConsole.SetAuthentication("foo", "foo", "bar");
            var (username, password) = ioConsole.GetAuthentication("foo");

            Assert.AreEqual("foo", username);
            Assert.AreEqual("bar", password);
        }

        [TestMethod]
        public void TestHasAuthentication()
        {
            var tester = new TesterIOConsole();
            var ioConsole = tester.Mock();

            Assert.IsFalse(ioConsole.HasAuthentication("foo"));

            ioConsole.SetAuthentication("foo", "foo", "bar");

            Assert.IsTrue(ioConsole.HasAuthentication("foo"));
        }
    }
}

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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.IO
{
    [TestClass]
    public class TestsIONull
    {
        [TestMethod]
        public void TestIsInteractive()
        {
            var ioNull = new IONull();
            Assert.IsFalse(ioNull.IsInteractive);
        }

        [TestMethod]
        public void TestIsDecorated()
        {
            var ioNull = new IONull();
            Assert.IsFalse(ioNull.IsDecorated);
        }

        [TestMethod]
        public void TestIsDebug()
        {
            var ioNull = new IONull();
            Assert.IsFalse(ioNull.IsDebug);
        }

        [TestMethod]
        public void TestIsVerbose()
        {
            var ioNull = new IONull();
            Assert.IsFalse(ioNull.IsVerbose);
        }

        [TestMethod]
        public void TestIsVeryVerbose()
        {
            var ioNull = new IONull();
            Assert.IsFalse(ioNull.IsVeryVerbose);
        }

        [TestMethod]
        public void TestAsk()
        {
            var ioNull = new IONull();
            Assert.AreEqual("menghanyu", (string)ioNull.Ask("What's your name?", "menghanyu"));
            Assert.AreEqual(null, (string)ioNull.Ask("What's your name?"));
        }

        [TestMethod]
        public void TestAskConfirmation()
        {
            var ioNull = new IONull();
            Assert.AreEqual(true, ioNull.AskConfirmation("Do you like me?", true));
            Assert.AreEqual(false, ioNull.AskConfirmation("Do you like me?", false));
        }

        [TestMethod]
        public void TestAskAndValidate()
        {
            var ioNull = new IONull();
            Assert.AreEqual(null, ioNull.AskAndValidate("Do you like me?", (value) => "foo"));
        }

        [TestMethod]
        public void TestAskChoice()
        {
            var ioNull = new IONull();
            var answer = ioNull.AskChoice("What's you name?", new[] { "miaomiao", "menghan" }, "unknow");
            Assert.AreEqual(-1, answer);

            answer = ioNull.AskChoice("What's you name?", new[] { "miaomiao", "menghan" }, "menghan");
            Assert.AreEqual(1, answer);

            answer = ioNull.AskChoice("What's you name?", new[] { "miaomiao", "menghan" }, 0);
            Assert.AreEqual(0, answer);
        }

        [TestMethod]
        public void TestAskChoiceMult()
        {
            var ioNull = new IONull();
            var answer = ioNull.AskChoiceMult("What's you name?", new[] { "miaomiao", "menghan", "tutu" }, new[] { "menghan", "miaomiao" });
            CollectionAssert.AreEqual(new[] { 1, 0 }, answer);

            answer = ioNull.AskChoiceMult("What's you name?", new[] { "miaomiao", "menghan", "tutu" }, new[] { 2, 0 });
            CollectionAssert.AreEqual(new[] { 2, 0 }, answer);
        }

        [TestMethod]
        public void TestWrite()
        {
            var ioNull = new IONull();
            ioNull.Write("not throw exception");
        }

        [TestMethod]
        public void TestWriteError()
        {
            var ioNull = new IONull();
            ioNull.WriteError("not throw exception");
        }

        [TestMethod]
        public void TestOverwrite()
        {
            var ioNull = new IONull();
            ioNull.Overwrite("not throw exception");
        }

        [TestMethod]
        public void TestOverwriteError()
        {
            var ioNull = new IONull();
            ioNull.OverwriteError("not throw exception");
        }

        [TestMethod]
        public void TestWriteWithLoggerLevel()
        {
            var ioNull = new IONull();
            var message = "not throw exception";
            ioNull.Debug(message);
            ioNull.Info(message);
            ioNull.Notice(message);
            ioNull.Warning(message);
            ioNull.Error(message);
            ioNull.Critical(message);
            ioNull.Alert(message);
            ioNull.Emergency(message);
        }
    }
}

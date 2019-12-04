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

using Bucket.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BaseQuestion = GameBox.Console.Question.Question;

namespace Bucket.Tests.Tester
{
    [TestClass]
    public class TestsTesterHelperQuestion
    {
        [TestMethod]
        public void TestTesterHelperQuestion()
        {
            var tester = new TesterHelperQuestion();
            tester.SetInputs(new[] { "menghanyu" });

            var question = new BaseQuestion("What's your name?", "What?");
            Assert.AreEqual("menghanyu", (string)tester.Ask(question));
            Assert.AreEqual("What's your name?", tester.GetDisplay());
        }

        [TestMethod]
        public void TestTesterHelperQuestionDefaultValue()
        {
            var tester = new TesterHelperQuestion();
            tester.SetInputs(new[] { string.Empty });

            var question = new BaseQuestion("What's your name?", "What?");
            Assert.AreEqual("What?", (string)tester.Ask(question));
            Assert.AreEqual("What's your name?", tester.GetDisplay());
        }
    }
}

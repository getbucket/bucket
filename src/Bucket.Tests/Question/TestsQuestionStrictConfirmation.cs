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

using Bucket.Question;
using Bucket.Tester;
using GameBox.Console.Exception;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Bucket.Tests.Question
{
    [TestClass]
    public class TestsQuestionStrictConfirmation
    {
        public static IEnumerable<object[]> GetAskConfirmationBadData()
        {
            return new[]
            {
                new object[] { "not correct" },
                new object[] { "no more" },
                new object[] { "yes please" },
                new object[] { "yellow" },
            };
        }

        public static IEnumerable<object[]> GetAskConfirmationData()
        {
            return new[]
            {
                new object[] { string.Empty, true, true },
                new object[] { string.Empty, false, false },
                new object[] { "y", true, false },
                new object[] { "yes", true, false },
                new object[] { "n", false, true },
                new object[] { "no", false, true },
            };
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidArgumentException), "Please answer yes, y, no, or n.")]
        [DynamicData("GetAskConfirmationBadData", DynamicDataSourceType.Method)]
        public void TestAskConfirmationBadAnswer(string answer)
        {
            var tester = new TesterHelperQuestion();
            tester.SetInputs(new[] { answer });

            var question = new QuestionStrictConfirmation("Do you like French fries?");
            question.SetMaxAttempts(1);

            tester.Ask(question);
        }

        [TestMethod]
        [DynamicData("GetAskConfirmationData", DynamicDataSourceType.Method)]
        public void TestAskConfirmation(string answer, bool expected, bool defaultValue)
        {
            var tester = new TesterHelperQuestion();
            tester.SetInputs(new[] { answer });

            var question = new QuestionStrictConfirmation("Do you like French fries?", defaultValue);
            question.SetMaxAttempts(1);

            Assert.AreEqual(expected, (bool)tester.Ask(question));
        }

        [TestMethod]
        public void TestAskConfirmationWithCustomTrueAndFalseAnswer()
        {
            var tester = new TesterHelperQuestion();
            tester.SetInputs(new[] { "ab" });
            var question = new QuestionStrictConfirmation("Do you like French fries?", false,
                "^ab$", "^cdefg$");
            question.SetMaxAttempts(1);
            Assert.AreEqual(true, (bool)tester.Ask(question));

            tester.SetInputs(new[] { "cdefg" });
            Assert.AreEqual(false, (bool)tester.Ask(question));
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidArgumentException), "Please answer ab or cdefg.")]
        public void TestAskConfirmationWithCustomErrorMessage()
        {
            var tester = new TesterHelperQuestion();
            tester.SetInputs(new[] { "foo" });
            var question = new QuestionStrictConfirmation("Do you like French fries?", false,
                "^ab$", "^cdefg$", "Please answer ab or cdefg.");
            question.SetMaxAttempts(1);
            tester.Ask(question);
        }
    }
}

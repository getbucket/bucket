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

using GameBox.Console.Exception;
using GameBox.Console.Util;
using System;
using System.Text.RegularExpressions;
using BaseQuestion = GameBox.Console.Question.Question;

namespace Bucket.Question
{
    /// <summary>
    /// Represents a strict yes/no question.
    /// </summary>
    public class QuestionStrictConfirmation : BaseQuestion
    {
        private readonly string trueAnswerRegex;
        private readonly string falseAnswerRegex;
        private readonly string errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestionStrictConfirmation"/> class.
        /// </summary>
        /// <param name="question">The question to ask to the user.</param>
        /// <param name="defaultValue">The default answer to return, true or false.</param>
        /// <param name="trueAnswerRegex">A regex to match the "yes" answer.</param>
        /// <param name="falseAnswerRegex">A regex to match the "false" answer.</param>
        /// <param name="errorMessage">A verification error message.</param>
        public QuestionStrictConfirmation(string question, bool defaultValue = true,
            string trueAnswerRegex = "^y(?:es)?$", string falseAnswerRegex = "^no?$",
            string errorMessage = "Please answer yes, y, no, or n.")
           : base(question, defaultValue)
        {
            this.trueAnswerRegex = trueAnswerRegex;
            this.falseAnswerRegex = falseAnswerRegex;
            this.errorMessage = errorMessage;
            SetNormalizer(GetDefaultNormalizer());
            SetValidator(GetDefaultValidator());
        }

        /// <summary>
        /// Gets the default normalizer.
        /// </summary>
        /// <returns>Returns the default normalizer.</returns>
        private Func<Mixture, Mixture> GetDefaultNormalizer()
        {
            return (value) =>
            {
                string answer = value.ToString().Trim();

                // If it is a Boolean value, it means that the default value is used
                // , because the user input value must be string.
                if (value.IsBoolean && (answer == "True" || answer == "False"))
                {
                    return value;
                }

                var answerIsTrue = Regex.IsMatch(answer, trueAnswerRegex, RegexOptions.IgnoreCase);
                if (answerIsTrue)
                {
                    return true;
                }

                var answerIsFalse = Regex.IsMatch(answer, falseAnswerRegex, RegexOptions.IgnoreCase);
                if (answerIsFalse)
                {
                    return false;
                }

                return null;
            };
        }

        /// <summary>
        /// Gets the default validator.
        /// </summary>
        /// <returns>Returns the default validator.</returns>
        private Func<Mixture, Mixture> GetDefaultValidator()
        {
            return (answer) =>
            {
                if (!(answer is null) && answer.IsBoolean)
                {
                    return answer;
                }

                throw new InvalidArgumentException(errorMessage);
            };
        }
    }
}

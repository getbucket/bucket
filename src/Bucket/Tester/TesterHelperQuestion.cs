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

using GameBox.Console.Helper;
using GameBox.Console.Input;
using GameBox.Console.Tester;
using GameBox.Console.Util;
using BaseQuestion = GameBox.Console.Question.Question;

namespace Bucket.Tester
{
    /// <summary>
    /// Eases the testing of <see cref="HelperQuestion"/>.
    /// </summary>
    public class TesterHelperQuestion : AbstractTester
    {
        /// <summary>
        /// An array of strings representing each input passed to the command input stream.
        /// </summary>
        private string[] inputs;

        /// <summary>
        /// Initializes a new instance of the <see cref="TesterHelperQuestion"/> class.
        /// </summary>
        /// <param name="helper">The helper question instance.</param>
        public TesterHelperQuestion(HelperQuestion helper = null)
        {
            Helper = helper ?? new HelperQuestion();
        }

        /// <summary>
        /// Gets the <see cref="HelperQuestion"/> instance.
        /// </summary>
        protected HelperQuestion Helper { get; }

        /// <summary>
        /// Sets an array of strings representing each input passed to the command input stream.
        /// </summary>
        /// <param name="inputs">The array of strings.</param>
        /// <returns>The current instance.</returns>
        public TesterHelperQuestion SetInputs(string[] inputs)
        {
            this.inputs = inputs;
            return this;
        }

        /// <summary>
        /// Executes the helper question test.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="options">The option with <see cref="AbstractTester"/>.</param>
        /// <returns>The user answer.</returns>
        public Mixture Ask(BaseQuestion question, params Mixture[] options)
        {
            var input = new InputArgs();
            if (options.TryGet("interactive", out Mixture exists))
            {
                input.SetInteractive(exists);
            }

            if (inputs != null && inputs.Length > 0)
            {
                input.SetInputStream(CreateStream(inputs));
            }

            Initialize(options);
            return Helper.Ask(input, Output, question);
        }
    }
}

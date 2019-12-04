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
using Bucket.Util;
using GameBox.Console.Helper;
using GameBox.Console.Input;
using GameBox.Console.Output;
using GameBox.Console.Question;
using GameBox.Console.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using BaseQuestion = GameBox.Console.Question.Question;

namespace Bucket.IO
{
    /// <summary>
    /// The Input/Output helper with console.
    /// </summary>
    public class IOConsole : BaseIO
    {
        private static readonly IDictionary<Verbosities, OutputOptions> VerbosityMapping =
            new Dictionary<Verbosities, OutputOptions>()
            {
                { Verbosities.Quiet, OutputOptions.VerbosityQuiet },
                { Verbosities.Normal, OutputOptions.VerbosityNormal },
                { Verbosities.Verbose, OutputOptions.VerbosityVerbose },
                { Verbosities.VeryVerbose, OutputOptions.VerbosityVeryVerbose },
                { Verbosities.Debug, OutputOptions.VerbosityDebug },
            };

        private HelperQuestion helper;
        private Stopwatch stopwatch;
        private string lastMessageError;
        private string lastMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="IOConsole"/> class.
        /// </summary>
        /// <param name="input">The std input instance.</param>
        /// <param name="output">The std output instance.</param>
        public IOConsole(IInput input, IOutput output)
        {
            SetInput(input);
            SetOutput(output);
        }

        /// <inheritdoc />
        public override bool IsInteractive => Input.IsInteractive;

        /// <inheritdoc />
        public override bool IsDecorated => Output.IsDecorated;

        /// <inheritdoc />
        public override bool IsVerbose => Output.IsVerbose;

        /// <inheritdoc />
        public override bool IsVeryVerbose => Output.IsVeryVerbose;

        /// <inheritdoc />
        public override bool IsDebug => Output.IsDebug;

        /// <summary>
        /// Gets the std input instance.
        /// </summary>
        protected IInput Input { get; private set; }

        /// <summary>
        /// Gets the std output instance.
        /// </summary>
        protected IOutput Output { get; private set; }

        /// <summary>
        /// Sets the debugging status.
        /// </summary>
        /// <param name="stopwatch">The stop watch for recording time.</param>
        public void SetDebugging(Stopwatch stopwatch = null)
        {
            this.stopwatch = stopwatch;
        }

        /// <inheritdoc />
        public override Mixture Ask(string question, Mixture defaultValue = null)
        {
            var helperQuestion = GetHelperQuestion();
            return helperQuestion.Ask(Input, Output, new BaseQuestion(question, defaultValue));
        }

        /// <inheritdoc />
        public override Mixture AskPassword(string question, bool fallback = false)
        {
            var helperQuestion = GetHelperQuestion();
            var questionInstance = new BaseQuestion(question);
            questionInstance.SetPassword(true);
            questionInstance.SetPasswordFallback(fallback);

            return helperQuestion.Ask(Input, Output, questionInstance);
        }

        /// <inheritdoc />
        public override Mixture AskAndValidate(string question, Func<Mixture, Mixture> validator,
            int attempts = 0, Mixture defaultValue = null)
        {
            var helperQuestion = GetHelperQuestion();
            var questionInstance = new BaseQuestion(question, defaultValue);
            questionInstance.SetMaxAttempts(attempts);
            questionInstance.SetValidator(validator);

            return helperQuestion.Ask(Input, Output, questionInstance);
        }

        /// <inheritdoc />
        public override int AskChoice(string question, string[] choices, Mixture defaultValue,
            int attempts = 0, string errorMessage = "Value \"{0}\" is invalid")
        {
            var helperQuestion = GetHelperQuestion();
            var questionInstance = new QuestionChoice(question, choices, defaultValue);
            questionInstance.SetMaxAttempts(attempts);
            questionInstance.SetErrorMessage(errorMessage);

            var expect = helperQuestion.Ask(Input, Output, questionInstance);
            return Array.FindIndex(choices, (choice) => choice == expect);
        }

        /// <inheritdoc />
        public override int[] AskChoiceMult(string question, string[] choices, Mixture defaultValue,
            int attempts = 0, string errorMessage = "Value \"{0}\" is invalid")
        {
            var helperQuestion = GetHelperQuestion();
            var questionInstance = new QuestionChoice(question, choices, defaultValue);
            questionInstance.SetMaxAttempts(attempts);
            questionInstance.SetErrorMessage(errorMessage);
            questionInstance.SetMultiselect(true);

            var answer = helperQuestion.Ask(Input, Output, questionInstance);
            var results = new List<int>();
            foreach (var expect in (string[])answer)
            {
                results.Add(Array.FindIndex(choices, (choice) => choice == expect));
            }

            return results.ToArray();
        }

        /// <inheritdoc />
        public override bool AskConfirmation(string question, bool defaultValue = true)
        {
            var helperQuestion = GetHelperQuestion();
            var questionInstance = new QuestionStrictConfirmation(question, defaultValue);
            return helperQuestion.Ask(Input, Output, questionInstance) ?? defaultValue;
        }

        /// <inheritdoc />
        public override void Write(string message, bool newLine = true,
            Verbosities verbosity = Verbosities.Normal)
        {
            DoWrite(message, newLine, false, verbosity);
        }

        /// <inheritdoc />
        public override void WriteError(string message, bool newLine = true,
            Verbosities verbosity = Verbosities.Normal)
        {
            DoWrite(message, newLine, true, verbosity);
        }

        /// <inheritdoc />
        public override void Overwrite(string message, bool newLine = true,
            int size = -1, Verbosities verbosity = Verbosities.Normal)
        {
            DoOverwrite(new string[] { message }, newLine, size, false, verbosity);
        }

        /// <inheritdoc />
        public override void OverwriteError(string message, bool newLine = true,
            int size = -1, Verbosities verbosity = Verbosities.Normal)
        {
            DoOverwrite(new string[] { message }, newLine, size, true, verbosity);
        }

        /// <summary>
        /// Gets the question helper.
        /// </summary>
        /// <returns>The question helper instance.</returns>
        public HelperQuestion GetHelperQuestion()
        {
            return helper ?? (helper = new HelperQuestion());
        }

        /// <summary>
        /// Sets the question helper.
        /// </summary>
        /// <param name="helper">The question helper instance.</param>
        public void SetHelperQuestion(HelperQuestion helper)
        {
            this.helper = helper;
        }

        /// <summary>
        /// Sets the std input instance.
        /// </summary>
        /// <param name="input">The std input instance.</param>
        protected internal void SetInput(IInput input)
        {
            Input = input;
        }

        /// <summary>
        /// Sets the std output instance.
        /// </summary>
        /// <param name="output">The std output instance.</param>
        protected internal void SetOutput(IOutput output)
        {
            Output = output;
        }

        /// <summary>
        /// Determine if verbosities is satisfied.
        /// </summary>
        protected static bool SatisfyVerbosity(IOutput output, Verbosities verbosity)
        {
            if (!VerbosityMapping.TryGetValue(verbosity, out OutputOptions options))
            {
                return false;
            }

            return SatisfyVerbosity(output, options);
        }

        /// <inheritdoc cref="SatisfyVerbosity(IOutput, Verbosities)" />
        protected static bool SatisfyVerbosity(IOutput output, OutputOptions options)
        {
            // todo: If GameBox.Console provides a more friendly
            // way to get Verbosity, we can optimize this process.
            // issues: 17
            var verbosities = OutputOptions.VerbosityDebug | OutputOptions.VerbosityQuiet |
                              OutputOptions.VerbosityVerbose | OutputOptions.VerbosityVeryVerbose |
                              OutputOptions.VerbosityNormal;

            var outputVerbosity = (verbosities & output.Options) > 0 ? verbosities & output.Options : OutputOptions.VerbosityNormal;
            var optionsVerbosity = (verbosities & options) > 0 ? verbosities & options : OutputOptions.VerbosityNormal;

            return optionsVerbosity <= outputVerbosity;
        }

        /// <summary>
        /// Writes a message to the output.
        /// </summary>
        /// <param name="message">A single string.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="stderr">Whether is stderr output.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        private void DoWrite(string message, bool newLine = false, bool stderr = false,
            Verbosities verbosity = Verbosities.Normal)
        {
            DoWrite(new string[] { message }, newLine, stderr, verbosity);
        }

        /// <summary>
        /// Writes a message to the output.
        /// </summary>
        /// <param name="messages">An array of string.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="stderr">Whether is stderr output.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        private void DoWrite(string[] messages, bool newLine = false, bool stderr = false,
            Verbosities verbosity = Verbosities.Normal)
        {
            if (!VerbosityMapping.TryGetValue(verbosity, out OutputOptions options))
            {
                return;
            }

            if (stopwatch != null && stopwatch.IsRunning)
            {
                var memoryUsage = AbstractHelper.FormatMemory(Environment.WorkingSet);
                var timeSpent = stopwatch.Elapsed;
                messages = Arr.Map(messages, (message) =>
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        return $"[{memoryUsage}/{timeSpent.TotalSeconds.ToString("0.00")}s] {message}";
                    }

                    return message;
                });
            }

            if (stderr && Output is IOutputConsole consoleOutput)
            {
                var errorOutput = consoleOutput.GetErrorOutput();
                Array.ForEach(messages, (message) =>
                {
                    errorOutput.Write(message, newLine, options);
                });

                if (SatisfyVerbosity(errorOutput, options))
                {
                    lastMessageError = string.Join(newLine ? Environment.NewLine : string.Empty, messages);
                }

                return;
            }

            Array.ForEach(messages, (message) =>
            {
                Output.Write(message, newLine, options);
            });

            if (SatisfyVerbosity(Output, options))
            {
                lastMessage = string.Join(newLine ? Environment.NewLine : string.Empty, messages);
            }
        }

        /// <summary>
        /// Overwrites a previous message to the output.
        /// </summary>
        /// <param name="messages">An array of string.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="size">The size of line will overwrites.</param>
        /// <param name="stderr">Whether is stderr output.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        private void DoOverwrite(string[] messages, bool newLine = false, int size = -1,
            bool stderr = false, Verbosities verbosity = Verbosities.Normal)
        {
            var message = string.Join(newLine ? Environment.NewLine : string.Empty, messages);

            if (size < 0)
            {
                // since overwrite is supposed to overwrite last message.
                size = AbstractHelper.StrlenWithoutDecoration(
                    Output.Formatter,
                    (stderr && Output is IOutputConsole) ? lastMessageError : lastMessage);
            }

            // let's fill its length with backspaces
            DoWrite(Str.Repeat("\x08", size), false, stderr, verbosity);

            DoWrite(message, false, stderr, verbosity);

            // In cmd.exe on Win8.1 (possibly 10?), the line can not
            // be cleared, so we need to track the length of previous
            // output and fill it with spaces to make sure the line
            // is cleared.
            var fill = size - AbstractHelper.StrlenWithoutDecoration(Output.Formatter, message);
            if (fill > 0)
            {
                DoWrite(Str.Repeat(fill), false, stderr, verbosity);
                DoWrite(Str.Repeat("\x08", fill), false, stderr, verbosity);
            }

            if (newLine)
            {
                DoWrite(string.Empty, true, stderr, verbosity);
            }

            var output = Output;
            if (stderr && Output is IOutputConsole consoleOutput)
            {
                output = consoleOutput.GetErrorOutput();
            }

            if (!SatisfyVerbosity(output, verbosity))
            {
                return;
            }

            if (stderr)
            {
                lastMessageError = message;
            }
            else
            {
                lastMessage = message;
            }
        }
    }
}

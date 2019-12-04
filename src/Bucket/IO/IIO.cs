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

using GameBox.Console.Util;
using System;

namespace Bucket.IO
{
    /// <summary>
    /// The Input/Output helper interface.
    /// </summary>
    public interface IIO
    {
        /// <summary>
        /// Gets a value indicating whether is this input means interactive.
        /// </summary>
        bool IsInteractive { get; }

        /// <summary>
        /// Gets a value indicating whether is decorated.
        /// </summary>
        bool IsDecorated { get; }

        /// <summary>
        /// Gets a value indicating whether the verbosity is set to <see cref="Verbosities.Verbose"/>.
        /// </summary>
        bool IsVerbose { get; }

        /// <summary>
        /// Gets a value indicating whether the verbosity is set to <see cref="Verbosities.VeryVerbose"/>.
        /// </summary>
        bool IsVeryVerbose { get; }

        /// <summary>
        /// Gets a value indicating whether the verbosity is set to <see cref="Verbosities.Debug"/>.
        /// </summary>
        bool IsDebug { get; }

        /// <summary>
        /// Writes a message to the output.
        /// </summary>
        /// <param name="message">An single string.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        void Write(string message, bool newLine = true, Verbosities verbosity = Verbosities.Normal);

        /// <summary>
        /// Writes a message to the error output.
        /// </summary>
        /// <param name="message">An single string.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        void WriteError(string message, bool newLine = true, Verbosities verbosity = Verbosities.Normal);

        /// <summary>
        /// Overwrites a previous message to the output.
        /// </summary>
        /// <param name="message">An single string.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="size">The size of line will overwrites.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        void Overwrite(string message, bool newLine = true, int size = -1, Verbosities verbosity = Verbosities.Normal);

        /// <summary>
        /// Overwrites a previous message to the error output.
        /// </summary>
        /// <param name="message">An single string.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="size">The size of line will overwrites.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        void OverwriteError(string message, bool newLine = true, int size = -1, Verbosities verbosity = Verbosities.Normal);

        /// <summary>
        /// Asks a question to the user.
        /// </summary>
        /// <param name="question">The question to ask.</param>
        /// <param name="defaultValue">The default answer if none is given by the user.</param>
        /// <returns>The user answer.</returns>
        Mixture Ask(string question, Mixture defaultValue = null);

        /// <summary>
        /// Asks a password question to the user.
        /// </summary>
        /// <param name="question">The question to ask.</param>
        /// <param name="fallback">Whether to allow password input to be downgraded.</param>
        /// <returns>The user answer.</returns>
        Mixture AskPassword(string question, bool fallback = false);

        /// <summary>
        /// Asks for a value and validates the response.
        /// </summary>
        /// <param name="question">The question to ask.</param>
        /// <param name="validator">The validator callback.</param>
        /// <param name="attempts">Max number of times to ask before giving up.</param>
        /// <param name="defaultValue">The default answer if none is given by the user.</param>
        /// <returns>The user answer.</returns>
        Mixture AskAndValidate(string question, Func<Mixture, Mixture> validator, int attempts = 0, Mixture defaultValue = null);

        /// <summary>
        /// Asks a confirmation to the user.
        /// The question will be asked until the user answers by nothing, yes, or no.
        /// </summary>
        /// <param name="question">The question to ask.</param>
        /// <param name="defaultValue">The default answer if the user enters nothing.</param>
        /// <returns>True if the user has confirmed, false otherwise.</returns>
        bool AskConfirmation(string question, bool defaultValue = true);

        /// <summary>
        /// Asks the user to select a value.
        /// </summary>
        /// <param name="question">The question to ask.</param>
        /// <param name="choices">List of choices to pick from.</param>
        /// <param name="defaultValue">The default answer if the user enters nothing.</param>
        /// <param name="attempts">Max number of times to ask before giving up.</param>
        /// <param name="errorMessage">Message which will be shown if invalid value from choice list would be picked.</param>
        /// <returns>The selected value.(the key of the choices array).</returns>
        int AskChoice(string question, string[] choices, Mixture defaultValue, int attempts = 0, string errorMessage = "Value \"{0}\" is invalid");

        /// <summary>
        /// Asks the user to select mult a value.
        /// </summary>
        /// <param name="question">The question to ask.</param>
        /// <param name="choices">List of choices to pick from.</param>
        /// <param name="defaultValue">The default answer if the user enters nothing.</param>
        /// <param name="attempts">Max number of times to ask before giving up.</param>
        /// <param name="errorMessage">Message which will be shown if invalid value from choice list would be picked.</param>
        /// <returns>The selected values.(the key of the choices array).</returns>
        int[] AskChoiceMult(string question, string[] choices, Mixture defaultValue, int attempts = 0, string errorMessage = "Value \"{0}\" is invalid");

        /// <summary>
        /// Set a repository authentication.
        /// </summary>
        /// <param name="repositoryName">The repository name.</param>
        /// <param name="username">The repository username.</param>
        /// <param name="password">The repository password.</param>
        void SetAuthentication(string repositoryName, string username, string password);

        /// <summary>
        /// Whether has the repository authentication.
        /// </summary>
        /// <param name="repositoryName">The repository name.</param>
        bool HasAuthentication(string repositoryName);

        /// <summary>
        /// Get the specified repository authentication.
        /// </summary>
        /// <param name="repositoryName">The repository name.</param>
        /// <returns>Returns the repository authentication. if not found return null tuples.</returns>
        (string Username, string Password) GetAuthentication(string repositoryName);
    }
}

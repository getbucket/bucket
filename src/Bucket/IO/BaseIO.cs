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

using Bucket.Logger;
using GameBox.Console.Util;
using System;
using System.Collections.Generic;

namespace Bucket.IO
{
    /// <summary>
    /// Base class for Bucket input/output.
    /// </summary>
    public abstract class BaseIO : BaseLogger, IIO, IResetAuthentications
    {
        private IDictionary<string, (string Username, string Password)> authentications;

        /// <inheritdoc />
        public virtual bool IsInteractive => false;

        /// <inheritdoc />
        public virtual bool IsDecorated => false;

        /// <inheritdoc />
        public virtual bool IsVerbose => false;

        /// <inheritdoc />
        public virtual bool IsVeryVerbose => false;

        /// <inheritdoc />
        public virtual bool IsDebug => false;

        /// <inheritdoc />
        public void ResetAuthentications()
        {
            // todo: implement ResetAuthentications()
        }

        /// <inheritdoc />
        public void SetAuthentication(string repositoryName, string username, string password)
        {
            if (authentications == null)
            {
                authentications = new Dictionary<string, (string Username, string Password)>();
            }

            authentications[repositoryName] = (username, password);
        }

        /// <inheritdoc />
        public bool HasAuthentication(string repositoryName)
        {
            if (authentications == null || !authentications.ContainsKey(repositoryName))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public (string Username, string Password) GetAuthentication(string repositoryName)
        {
            if (authentications == null ||
                !authentications.TryGetValue(repositoryName, out (string Username, string Password) authentication))
            {
                return (null, null);
            }

            return authentication;
        }

        /// <inheritdoc />
        public virtual Mixture Ask(string question, Mixture defaultValue = null)
        {
            return defaultValue;
        }

        /// <inheritdoc />
        public virtual Mixture AskPassword(string question, bool fallback = false)
        {
            return null;
        }

        /// <inheritdoc />
        public virtual Mixture AskAndValidate(string question, Func<Mixture, Mixture> validator,
            int attempts = 0, Mixture defaultValue = null)
        {
            return defaultValue;
        }

        /// <inheritdoc />
        public virtual int AskChoice(string question, string[] choices, Mixture defaultValue,
            int attempts = 0, string errorMessage = "Value \"{0}\" is invalid")
        {
            if (defaultValue.IsInt)
            {
                return defaultValue;
            }

            var expect = defaultValue.ToString();
            return Array.FindIndex(choices, (choice) => choice == expect);
        }

        /// <inheritdoc />
        public virtual int[] AskChoiceMult(string question, string[] choices, Mixture defaultValue,
            int attempts = 0, string errorMessage = "Value \"{0}\" is invalid")
        {
            if (defaultValue.IsInt)
            {
                return defaultValue;
            }

            var results = new List<int>();
            foreach (var expect in (string[])defaultValue)
            {
                results.Add(Array.FindIndex(choices, (choice) => choice == expect));
            }

            return results.ToArray();
        }

        /// <inheritdoc />
        public virtual bool AskConfirmation(string question, bool defaultValue = true)
        {
            return defaultValue;
        }

        /// <inheritdoc />
        public virtual void Write(string message, bool newLine = true,
            Verbosities verbosity = Verbosities.Normal)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public virtual void WriteError(string message, bool newLine = true,
            Verbosities verbosity = Verbosities.Normal)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public virtual void Overwrite(string message, bool newLine = true, int size = -1,
            Verbosities verbosity = Verbosities.Normal)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public virtual void OverwriteError(string message, bool newLine = true, int size = -1,
            Verbosities verbosity = Verbosities.Normal)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Log(LogLevel level, string message, params object[] context)
        {
            if (message == null)
            {
                return;
            }

            message = string.Format(message, context);

            switch (level)
            {
                case LogLevel.Emergency:
                case LogLevel.Alert:
                case LogLevel.Critical:
                case LogLevel.Error:
                    WriteError($"<error>{message}</error>", true, Verbosities.Normal);
                    break;
                case LogLevel.Warning:
                    WriteError($"<warning>{message}</warning>", true, Verbosities.Normal);
                    break;
                case LogLevel.Notice:
                    WriteError($"<info>{message}</info>", true, Verbosities.Verbose);
                    break;
                case LogLevel.Info:
                    WriteError($"<info>{message}</info>", true, Verbosities.VeryVerbose);
                    break;
                default:
                    WriteError(message, true, Verbosities.Debug);
                    break;
            }
        }

        /// <inheritdoc />
        protected override string Interpolate(string message, IDictionary<string, object> context)
        {
            // todo: implement it.
            return message;
        }
    }
}

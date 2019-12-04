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

using Bucket.Console;
using Bucket.EventDispatcher;
using Bucket.FileSystem;
using Bucket.Plugin;
using Bucket.Util;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System;
using System.IO;

namespace Bucket.Command
{
    /// <summary>
    /// Represents a package verification command.
    /// </summary>
    public class CommandValidate : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("validate")
                .SetDescription("Validates a bucket.json.")
                .SetDefinition(
                    new InputArgument("file", InputArgumentModes.Optional, $"Path to {Factory.DefaultBucketFile} file."),
                    new InputOption("no-check-publish", null, InputOptionModes.ValueNone, "Do not check for publish errors."),
                    new InputOption("no-check-lock", null, InputOptionModes.ValueNone, "Do not check if lock file is up to date."),
                    new InputOption("with-dependencies", "A", InputOptionModes.ValueNone, "Also validate the bucket.json of all installed dependencies."),
                    new InputOption("strict", null, InputOptionModes.ValueNone, "Return a non-zero exit code for warnings as well as errors."))
                .SetHelp(@"
The validate command validates a given bucket.json
Exit codes in case of errors are:
64 validation warning(s), only when --strict is given
65 validation error(s)
66 file unreadable or missing
");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            var file = input.GetArgument("file") ?? Factory.GetBucketFile();
            var io = GetIO();
            var fileSystem = new FileSystemLocal();

            if (!fileSystem.Exists(file))
            {
                io.WriteError($"<error>{file} not found.</error>");
                return ExitCodes.FileNotFoundException;
            }

            var checkPublish = !input.GetOption("no-check-publish");
            var checkLock = !input.GetOption("no-check-lock");
            var isStrict = input.GetOption("strict");

            var validator = new ValidatorBucket(fileSystem, io);
            var (warnings, publishErrors, errors) = validator.Validate(file);

            var lockErrors = Array.Empty<string>();
            var bucket = Factory.Create(io, file, input.HasRawOption("--no-plugins"));
            var locker = bucket.GetLocker();
            if (locker.IsLocked() && !locker.IsFresh())
            {
                lockErrors = new[] { "The lock file is not up to date with the latest changes in bucket.json, it is recommended that you run `bucket update`." };
            }

            OuputResult(file, ref errors, ref warnings, checkPublish, publishErrors, checkLock, lockErrors, isStrict);

            int GetStrictExitCode()
            {
                return (isStrict && !warnings.Empty()) ? ExitCodes.ValidationWarning : GameBox.Console.ExitCodes.Normal;
            }

            var exitCode = errors.Length > 0 ? ExitCodes.ValidationErrors : GetStrictExitCode();

            if (input.GetOption("with-dependencies"))
            {
                var localInstalledRepository = bucket.GetRepositoryManager().GetLocalInstalledRepository();
                foreach (var package in localInstalledRepository.GetPackages())
                {
                    var path = bucket.GetInstallationManager().GetInstalledPath(package);
                    file = Path.Combine(path, "bucket.json");

                    if (!Directory.Exists(path) || !File.Exists(file))
                    {
                        continue;
                    }

                    (warnings, publishErrors, errors) = validator.Validate(file);
                    OuputResult(file, ref errors, ref warnings, checkPublish, publishErrors, isStrict: isStrict);

                    var depCode = !errors.Empty() ? ExitCodes.ValidationErrors : GetStrictExitCode();
                    exitCode = Math.Max(depCode, exitCode);
                }
            }

            var commandEvent = new CommandEventArgs(PluginEvents.Command, "validate", input, output);
            bucket.GetEventDispatcher().Dispatch(this, commandEvent);

            return Math.Max(exitCode, commandEvent.ExitCode);
        }

        private void OuputResult(
            string file,
            ref string[] errors,
            ref string[] warnings,
            bool checkPublish = false,
            string[] publishErrors = null,
            bool checkLock = false,
            string[] lockErrors = null,
            bool isStrict = false)
        {
            var io = GetIO();

            if (errors.Empty() && publishErrors.Empty() && warnings.Empty())
            {
                io.Write($"<info>{file} is valid.</info>");
            }
            else if (errors.Empty() && publishErrors.Empty())
            {
                io.WriteError($"<info>{file} is valid, but with a few warnings.</info>");
            }
            else if (errors.Empty())
            {
                io.WriteError($"<info>{file} is valid for simple usage with bucket but has strict errors that make it unable to be published as a package:</info>");
            }
            else
            {
                io.WriteError($"<error>{file} is invalid, the following errors/warnings were found:</error>");
            }

            if (checkPublish)
            {
                errors = Arr.Merge(errors, publishErrors);
            }
            else if (!isStrict)
            {
                warnings = Arr.Merge(warnings, publishErrors);
            }

            // If checking lock errors, display them as errors,
            // otherwise just show them as warnings Skip when
            // it is a strict check and we don't want to check
            // lock errors.
            if (checkLock)
            {
                errors = Arr.Merge(errors, lockErrors);
            }
            else if (!isStrict)
            {
                warnings = Arr.Merge(warnings, lockErrors);
            }

            var messages = new[]
            {
                ("error", errors),
                ("warning", warnings),
            };

            foreach (var (style, msgs) in messages)
            {
                foreach (var msg in msgs)
                {
                    io.WriteError($"  - <{style}>{msg}</{style}>");
                }
            }
        }
    }
}

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

using Bucket.Configuration;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.Installer;
using Bucket.IO;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.EventDispatcher;
using GameBox.Console.Process;
using GameBox.Console.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Bucket.EventDispatcher
{
    /// <summary>
    /// Bucket event dispatcher instance.
    /// </summary>
    public class EventDispatcher : IEventDispatcher
    {
        private readonly Bucket bucket;
        private readonly Stack<string> eventStack;
        private readonly IDictionary<string, IList<EventHandler>> listeners;
        private readonly IIO io;
        private readonly IFileSystem fileSystem;
        private readonly IProcessExecutor process;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDispatcher"/> class.
        /// </summary>
        public EventDispatcher(Bucket bucket, IIO io, IProcessExecutor process = null, IFileSystem fileSystem = null)
        {
            this.bucket = bucket;
            this.io = io;
            this.process = process ?? new BucketProcessExecutor(io);
            listeners = new Dictionary<string, IList<EventHandler>>();
            eventStack = new Stack<string>();
            this.fileSystem = fileSystem ?? new FileSystemLocal();
        }

        /// <inheritdoc />
        public virtual bool HasListener(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return false;
            }

            return GetListeners(new BucketEventArgs(eventName)).Count > 0;
        }

        /// <inheritdoc />
        public virtual void Dispatch(string eventName, object sender, EventArgs eventArgs = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (eventArgs is null)
            {
                eventArgs = new BucketEventArgs(eventName);
            }
            else if (!(eventArgs is BucketEventArgs))
            {
                eventArgs = new WrappedEventArgs(eventName, eventArgs);
            }

            DoDispatch(sender, (BucketEventArgs)eventArgs);
        }

        /// <inheritdoc />
        public virtual void AddListener(string eventName, EventHandler listener)
        {
            if (string.IsNullOrEmpty(eventName) || listener == null)
            {
                return;
            }

            if (!listeners.TryGetValue(eventName, out IList<EventHandler> handlers))
            {
                listeners[eventName] = handlers = new List<EventHandler>();
            }

            handlers.Add(listener);
        }

        /// <inheritdoc />
        public virtual void RemoveListener(string eventName, EventHandler listener = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (listener == null)
            {
                listeners.Remove(eventName);
                return;
            }

            if (listeners.TryGetValue(eventName, out IList<EventHandler> handlers))
            {
                handlers.Remove(listener);

                if (handlers.Count <= 0)
                {
                    listeners.Remove(eventName);
                }
            }
        }

        /// <inheritdoc />
        public virtual void AddSubscriber(IEventSubscriber subscriber)
        {
            foreach (var subscribed in subscriber.GetSubscribedEvents())
            {
                AddListener(subscribed.Key, subscribed.Value);
            }
        }

        /// <inheritdoc />
        public virtual void RemoveSubscriber(IEventSubscriber subscriber)
        {
            foreach (var subscribed in subscriber.GetSubscribedEvents())
            {
                RemoveListener(subscribed.Key, subscribed.Value);
            }
        }

        /// <summary>
        /// Execution script command.
        /// </summary>
        protected internal virtual void ExecuteScript(string callable, object sender, BucketEventArgs eventArgs)
        {
            var args = string.Join(Str.Space, Arr.Map(eventArgs.GetArguments(), ProcessExecutor.Escape));
            var exec = callable + (string.IsNullOrEmpty(args) ? string.Empty : (Str.Space + args));

            if (io.IsVerbose)
            {
                io.WriteError($"> {eventArgs.Name}: {exec}");
            }
            else
            {
                io.WriteError($"> {exec}");
            }

            var possibleLocalBinaries = bucket.GetPackage().GetBinaries() ?? Array.Empty<string>();
            foreach (var localBinary in possibleLocalBinaries)
            {
                if (!Regex.IsMatch(localBinary, $"\\b{Regex.Escape(callable)}$"))
                {
                    continue;
                }

                var caller = InstallerBinary.DetermineBinaryCaller(localBinary, fileSystem);
                exec = Regex.Replace(exec, $"^{Regex.Escape(callable)}", $"{caller} {localBinary}");
            }

            var exitCode = process.Execute(exec, out string stdout, out string stderr);
            if (exitCode != ExitCodes.Normal)
            {
                io.WriteError($"<error>Script \"{callable}\" handling the \"{eventArgs.Name}\" event returned with error code: {exitCode}</error>", verbosity: Verbosities.Quiet);
                throw new ScriptExecutionException($"Error Output: {stderr}", exitCode);
            }

            if (io.IsDebug)
            {
                io.WriteError(stdout);
            }
        }

        /// <summary>
        /// Triggers the listeners of an event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="eventArgs">The event object to pass to the event listeners.</param>
        protected virtual void DoDispatch(object sender, BucketEventArgs eventArgs)
        {
            PreparationEnvironment(bucket.GetConfig());
            PushEventStack(eventArgs);

            try
            {
                foreach (var listener in GetListeners(eventArgs))
                {
                    if (eventArgs is WrappedEventArgs wrappedEventArgs)
                    {
                        listener(sender, wrappedEventArgs.GetBaseEventArgs());
                    }
                    else
                    {
                        listener(sender, eventArgs);
                    }

                    if (eventArgs is IStoppableEvent stoppableEvent
                            && stoppableEvent.IsPropagationStopped)
                    {
                        break;
                    }
                }
            }
            finally
            {
                PopEventStack(eventArgs);
            }
        }

        /// <summary>
        /// Retrieves all listeners for a given event.
        /// </summary>
        protected virtual IList<EventHandler> GetListeners(BucketEventArgs eventArgs)
        {
            var collection = new List<EventHandler>();
            if (listeners.TryGetValue(eventArgs.Name, out IList<EventHandler> handlers))
            {
                collection.AddRange(handlers);
            }

            collection.AddRange(GetScriptListeners(eventArgs));
            return collection;
        }

        /// <summary>
        /// Finds all listeners defined as scripts in the package.
        /// </summary>
        protected virtual IList<EventHandler> GetScriptListeners(BucketEventArgs eventArgs)
        {
            var package = bucket.GetPackage();
            var scripts = package?.GetScripts();

            if (scripts == null || scripts.Count <= 0 ||
                !scripts.TryGetValue(eventArgs.Name, out string callable) || string.IsNullOrEmpty(callable))
            {
                return Array.Empty<EventHandler>();
            }

            void ScriptEventHandler(object sender, EventArgs args)
            {
                ExecuteScript(callable, sender, eventArgs);
            }

            return new EventHandler[] { ScriptEventHandler };
        }

        /// <summary>
        /// Prepare the event execution environment.
        /// </summary>
        /// <remarks>E.g add the bin dir to the PATH to make local binaries of deps usable in scripts.</remarks>
        protected virtual void PreparationEnvironment(Config config)
        {
            if (config == null)
            {
                return;
            }

            // todo: File path needs to be optimized.
            string binDir = config.Get(Settings.BinDir);
            binDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, binDir));

            if (!fileSystem.Exists(binDir, FileSystemOptions.Directory))
            {
                return;
            }

            const string pathStr = "PATH";
            var pathSeparator = Platform.IsWindows ? ';' : ':';
            string pathData = Terminal.GetEnvironmentVariable(pathStr);
            if (pathData != null && Array.Exists(pathData.Split(pathSeparator), (path) => path.Trim() == binDir))
            {
                return;
            }

            pathData = string.IsNullOrEmpty(pathData) ? binDir : $"{pathData}{pathSeparator}{binDir}";
            Terminal.SetEnvironmentVariable(pathStr, pathData);
        }

        /// <summary>
        /// Push into the event stack to avoid relying on loop calls.
        /// </summary>
        protected virtual void PushEventStack(BucketEventArgs eventArgs)
        {
            if (eventStack.Contains(eventArgs.Name))
            {
                throw new RuntimeException($"Circular call to script handler \"{eventArgs.Name}\" detected. {GetEventStackDebugMessage()}");
            }

            eventStack.Push(eventArgs.Name);
        }

        /// <summary>
        /// Pops the active event from the stack.
        /// </summary>
        protected virtual void PopEventStack(BucketEventArgs eventArgs)
        {
            var eventName = eventStack.Pop();
            Guard.Requires<UnexpectedException>(
                eventName == eventArgs.Name,
                $"The name of the event that pops up is inconsistent with the name of the event that pops up ({eventName} != {eventArgs.Name}), and the event stack is confused.");
        }

        /// <summary>
        /// Gets the debug message of the event stack.
        /// </summary>
        protected virtual string GetEventStackDebugMessage()
        {
            var previous = string.Join(", ", eventStack.ToArray());
            return $"Event stack [{previous}].";
        }
    }
}

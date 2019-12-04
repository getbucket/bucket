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

using Bucket.Exception;
using Bucket.IO;
using Bucket.Package;
using Bucket.Plugin.Capability;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Bucket.Util;
using GameBox.Console.EventDispatcher;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BVersionParser = Bucket.Package.Version.VersionParser;
using SException = System.Exception;

namespace Bucket.Plugin
{
    /// <summary>
    /// The plugin manager.
    /// </summary>
    public class PluginManager
    {
        /// <summary>
        /// Represents the plugin type.
        /// </summary>
        public const string PluginType = "bucket-plugin";

        /// <summary>
        /// Represents the required package name of the plug-in api.
        /// </summary>
        public const string PluginRequire = PluginType + "-api";

        private readonly IIO io;
        private readonly Bucket bucket;
        private readonly Bucket globalBucket;
        private readonly bool disablePlugins;
        private readonly IVersionParser versionParser;
        private readonly IList<IPlugin> plugins;
        private readonly IDictionary<string, IList<IPlugin>> activatedPlugins;
        private readonly ISet<string> loadedAssemblies;
        private readonly ISet<string> activatePackages;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginManager"/> class.
        /// </summary>
        public PluginManager(IIO io, Bucket bucket, Bucket globalBucket = null, bool disablePlugins = false)
        {
            this.io = io;
            this.bucket = bucket;
            this.globalBucket = globalBucket;
            this.disablePlugins = disablePlugins;
            versionParser = new BVersionParser();
            plugins = new List<IPlugin>();
            activatedPlugins = new Dictionary<string, IList<IPlugin>>();
            loadedAssemblies = new HashSet<string>();
            activatePackages = new HashSet<string>();
        }

        /// <summary>
        /// Returns an array of activate plugins.
        /// </summary>
        public virtual IPlugin[] GetPlugins()
        {
            return plugins.ToArray();
        }

        /// <summary>
        /// Gets global bucket or null when main bucket is not fully loaded.
        /// </summary>
        public virtual Bucket GetGlobalBucket()
        {
            return globalBucket;
        }

        /// <summary>
        /// Loads all plugins from currently installed plugin packages.
        /// </summary>
        public virtual void LoadInstalledPlugins()
        {
            if (disablePlugins)
            {
                return;
            }

            var repository = bucket.GetRepositoryManager().GetLocalInstalledRepository();
            var repositoryGlobal = globalBucket?.GetRepositoryManager().GetLocalInstalledRepository();

            LoadRepository(repository);
            LoadRepository(repositoryGlobal);
        }

        /// <summary>
        /// Register and activate a plugin package.
        /// </summary>
        /// <param name="package">The plugin package instance.</param>
        /// <param name="failOnMissing">
        /// By default this silently skips plugins that can not be found,
        /// but if set to true it fails with an exception.
        /// </param>
        public virtual void ActivatePackages(IPackage package, bool failOnMissing = false)
        {
            if (disablePlugins || !AssertPluginApiVersion(package))
            {
                return;
            }

            if (!activatePackages.Add(package.GetName()))
            {
                return;
            }

            IList<IPlugin> activatedPluginCollection = null;
            void LoadPlugin(string pluginPath)
            {
                Assembly assembly;
                try
                {
#pragma warning disable S3885
                    assembly = Assembly.Load(File.ReadAllBytes(pluginPath));
#pragma warning restore S3885
                    Guard.Requires<UnexpectedException>(assembly != null, $"The loaded assembly \"{pluginPath}\" should not be null, otherwise an exception is thrown.");

                    if (!loadedAssemblies.Add(assembly.FullName))
                    {
                        io.WriteError($"<warning>Plugin \"{pluginPath}\" ({assembly}) is loaded repeatedly, auto skip.</warning>");
                        return;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    io.WriteError($"<warning>The \"{package.GetName()}\" plugin library \"{pluginPath}\" directory is not found.</warning>");
                    return;
                }
                catch (FileNotFoundException)
                {
                    io.WriteError($"<warning>The \"{package.GetName()}\" plugin library \"{pluginPath}\" is not found.</warning>");
                    return;
                }
#pragma warning disable CA1031
                catch (SException ex)
#pragma warning restore CA1031
                {
                    io.WriteError($"<warning>The \"{package.GetName()}\" plugin library \"{pluginPath}\" could not loaded, Some errors occur during loading: {ex.Message}</warning>");
                    return;
                }

                foreach (var type in assembly.GetTypes())
                {
                    if (!typeof(IPlugin).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    IPlugin plugin;
                    try
                    {
                        plugin = (IPlugin)Activator.CreateInstance(type);
                        Guard.Requires<UnexpectedException>(plugin != null, $"The loaded class should not be null, otherwise an exception is thrown. Assembly is \"{pluginPath}\"");
                    }
                    catch (MissingMethodException ex)
                    {
                        if (failOnMissing)
                        {
                            throw new RuntimeException($"Plugin \"{package.GetName()}\" could not be created, The type \"{type}\" must have a no-argument constructor.", ex);
                        }

                        io.WriteError($"<warning>In \"{package.GetName()}\" plugin library \"{pluginPath}\". The type \"{type}\" must have a no-argument constructor.</warning>");
                        continue;
                    }

                    if (activatedPluginCollection == null &&
                        !activatedPlugins.TryGetValue(package.GetName(), out activatedPluginCollection))
                    {
                        activatedPlugins[package.GetName()] = activatedPluginCollection = new List<IPlugin>();
                    }

                    activatedPluginCollection.Add(plugin);
                    ActivatePlugin(plugin);
                }
            }

            var globalRepository = globalBucket?.GetRepositoryManager().GetLocalInstalledRepository();
            var installationPath = GetInstalledPath(package, globalRepository?.HasPackage(package) ?? false);
            var extraPlugins = ExtractExtraPluginData(package);
            foreach (var extraPlugin in extraPlugins)
            {
                var pluginPath = Path.Combine(installationPath, extraPlugin);
                var pluginPaths = new[] { pluginPath };
                if (pluginPath.Contains("*"))
                {
                    // todo: implement wildcard searching.
                    throw new NotImplementedException("Wildcard searching is not support. This feature will be completed in subsequent updates.");
                }

                Array.ForEach(pluginPaths, LoadPlugin);
            }
        }

        /// <summary>
        /// Deactivates a plugin package.
        /// </summary>
        public virtual void DeactivatePackage(IPackage package)
        {
            if (disablePlugins)
            {
                return;
            }

            activatePackages.Remove(package.GetName());

            if (!activatedPlugins.TryGetValue(package.GetName(), out IList<IPlugin> activatedPluginCollection))
            {
                return;
            }

            activatedPlugins.Remove(package.GetName());
            foreach (var plugin in activatedPluginCollection)
            {
                DeactivatePlugin(plugin);
                loadedAssemblies.Remove(plugin.GetType().Assembly.FullName);
            }
        }

        /// <summary>
        /// Uninstall a plugin package.
        /// </summary>
        public virtual void UninstallPackage(IPackage package)
        {
            if (disablePlugins)
            {
                return;
            }

            activatePackages.Remove(package.GetName());

            if (!activatedPlugins.TryGetValue(package.GetName(), out IList<IPlugin> activatedPluginCollection))
            {
                return;
            }

            activatedPlugins.Remove(package.GetName());
            foreach (var plugin in activatedPluginCollection)
            {
                DeactivatePlugin(plugin);
                UninstallPlugin(plugin);
                loadedAssemblies.Remove(plugin.GetType().Assembly.FullName);
            }
        }

        /// <summary>
        /// Adds a plugin, activates it and registers it with the event dispatcher.
        /// </summary>
        /// <remarks>
        /// Ideally plugin packages should be registered via <see cref="ActivatePackages"/>, but if
        /// you use Bucket programmatically and want to register a plugin class
        /// directly this is a valid way to do it.
        /// </remarks>
        public virtual void ActivatePlugin(IPlugin plugin)
        {
            io.WriteError($"Loading plugin {plugin}.", true, Verbosities.Debug);

            plugins.Add(plugin);
            plugin.Activate(bucket, io);
            if (plugin is IEventSubscriber eventSubscriber)
            {
                bucket.GetEventDispatcher().AddSubscriber(eventSubscriber);
            }
        }

        /// <summary>
        /// Removes a plugin, deactivates it and removes any listener the plugin has
        /// set on the plugin instance.
        /// </summary>
        /// <remarks>
        /// Ideally plugin packages should be deactivated via <see cref="DeactivatePackage"/>, but
        /// if you use Bucket programmatically and want to deregister a plugin class
        /// directly this is a valid way to do it.
        /// </remarks>
        public virtual void DeactivatePlugin(IPlugin plugin)
        {
            if (!plugins.Remove(plugin))
            {
                return;
            }

            io.WriteError($"Unloading plugin {plugin}.", true, Verbosities.Debug);
            plugin.Deactivate(bucket, io);
            if (plugin is IEventSubscriber eventSubscriber)
            {
                bucket.GetEventDispatcher().RemoveSubscriber(eventSubscriber);
            }
        }

        /// <summary>
        /// Notifies a plugin it is being uninstalled and should clean up.
        /// </summary>
        /// <remarks>
        /// Ideally plugin packages should be uninstalled via uninstallPackage, but if you use
        /// Bucket programmatically and want to deregister a plugin class directly this is a
        /// valid way to do it.
        /// </remarks>
        public virtual void UninstallPlugin(IPlugin plugin)
        {
            io.WriteError($"Uninstalling plugin {plugin}", true, Verbosities.Debug);
            plugin.Uninstall(bucket, io);
        }

        /// <summary>
        /// Get an array of the plugin capability implement.
        /// </summary>
        /// <param name="plugin">The plugin instance.</param>
        /// <param name="capability">What capabilities need to be get.</param>
        /// <param name="args">The capability constructor's arguments.</param>
        public virtual ICapability[] GetPluginCapabilities(IPlugin plugin, Type capability, params object[] args)
        {
            if (!(plugin is ICapable capable))
            {
                return Array.Empty<ICapability>();
            }

            if (!typeof(ICapability).IsAssignableFrom(capability))
            {
                throw new UnexpectedException($"Capability type \"{capability}\" must implement from {nameof(ICapability)}.");
            }

            // todo: using GameBox 3.0 need replace to Container.
            ConstructorInfo GetBestConstructor(Type capabilityType, ref object[] constructorArgs)
            {
                var argsType = Arr.Map(constructorArgs, (arg) => arg.GetType());
                while (true)
                {
                    var constructor = capabilityType.GetConstructor(argsType);
                    if (constructor != null || argsType.Length == 0)
                    {
                        return constructor;
                    }

                    Arr.Pop(ref argsType);
                    Arr.Pop(ref constructorArgs);
                }
            }

            var collection = new List<ICapability>();
            foreach (var capabilityType in GetCapabilityImplementationTypes(capable, capability))
            {
                var constructorArgs = Arr.Merge(new[] { plugin }, args);
                var constructor = GetBestConstructor(capabilityType, ref constructorArgs);

                if (constructor == null)
                {
                    io.WriteError($"<warning>The \"{plugin}\" plugin capability \"{capabilityType}\" is invalid, Because the constructor cannot be satisfied: ({string.Join<Type>(", ", Arr.Map(args, (arg) => arg.GetType()))}).</warning>");
                    continue;
                }

                var capabilityImplement = constructor.Invoke(constructorArgs);

                if (!(capabilityImplement is ICapability ret))
                {
                    throw new UnexpectedException($"Capability instance \"{capabilityImplement}\" must implement from {nameof(ICapability)}.");
                }

                collection.Add(ret);
            }

            return collection.ToArray();
        }

        /// <summary>
        /// Gets an array of capabilities from all activated plugin.
        /// </summary>
        /// <typeparam name="T">What capabilities need to be get.</typeparam>
        /// <param name="plugin">The plugin instance.</param>
        /// <param name="args">The capability constructor's arguments.</param>
        public virtual T[] GetPluginCapabilities<T>(IPlugin plugin, params object[] args)
            where T : ICapability
        {
            var capabilities = GetPluginCapabilities(plugin, typeof(T), args);
            return Arr.Map(capabilities, (capability) => (T)capability);
        }

        /// <summary>
        /// Gets an array of capabilities from all activated plugin.
        /// </summary>
        /// <param name="capability">What capabilities need to be get.</param>
        /// <param name="args">The capability constructor's arguments.</param>
        public virtual ICapability[] GetAllCapabilities(Type capability, params object[] args)
        {
            var collection = new List<ICapability>();
            foreach (var plugin in GetPlugins())
            {
                collection.AddRange(GetPluginCapabilities(plugin, capability, args));
            }

            return collection.ToArray();
        }

        /// <summary>
        /// Gets an array of capabilities from all activated plugin.
        /// </summary>
        /// <typeparam name="T">What capabilities need to be get.</typeparam>
        /// <param name="args">The capability constructor's arguments.</param>
        public virtual T[] GetAllCapabilities<T>(params object[] args)
            where T : ICapability
        {
            var capabilities = GetAllCapabilities(typeof(T), args);
            return Arr.Map(capabilities, (capability) => (T)capability);
        }

        /// <summary>
        /// Returns the plugin api version.
        /// </summary>
        protected virtual string GetPluginApiVersion()
        {
            return PluginConst.PluginApiVersion;
        }

        /// <summary>
        /// Assert whether the plugin api version matches the current bucket.
        /// </summary>
        protected virtual bool AssertPluginApiVersion(IPackage package)
        {
            if (package.GetPackageType() != PluginType)
            {
                return false;
            }

            IConstraint requireConstraint = null;
            foreach (var link in package.GetRequires())
            {
                if (link.GetTarget() == PluginRequire)
                {
                    requireConstraint = link.GetConstraint();
                    break;
                }
            }

            if (requireConstraint == null)
            {
                throw new RuntimeException(
                    $"Plugin \"{package.GetName()}\" is missing a require statement for a version of the \"{PluginRequire}\" package.");
            }

            var currentPluginApiVersion = GetPluginApiVersion();
            var currentPluginApiConstraint = new Constraint("==", versionParser.Normalize(currentPluginApiVersion));

            if (!requireConstraint.Matches(currentPluginApiConstraint))
            {
                io.WriteError($"<warning>The \"{package.GetName()}\" plugin was skipped because it requires a Plugin API version (\"{requireConstraint.GetPrettyString()}\") that does not match your Bucket installation (\"{currentPluginApiVersion}\"). You may need to run bucket update with the \"--no-plugins\" option.</warning>");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extract an array of plugin path in extra property.
        /// </summary>
        protected virtual string[] ExtractExtraPluginData(IPackage package)
        {
            var extra = package.GetExtra();
            if (extra == null || extra["plugin"] == null)
            {
                throw new RuntimeException($"Error while installing \"{package.GetName()}\", plugin packages should have a \"plugin\" defined in their \"extra\" key to be usable.");
            }

            // todo: maybe the code can be improved.
            var extraPlugin = extra["plugin"];
            if (extraPlugin.Type == JTokenType.Array)
            {
                var collection = new List<string>();
                foreach (string plugin in extraPlugin)
                {
                    collection.Add(plugin);
                }

                return collection.ToArray();
            }

            if (extraPlugin.Type == JTokenType.String)
            {
                return new[] { (string)extraPlugin };
            }

            throw new RuntimeException($"The plugin \"{package.GetName()}\" extra data is invalid, expected type is Array or String, actual is \"{extraPlugin.Type}\".");
        }

        /// <summary>
        /// Gets an array of the capability implementation type.
        /// </summary>
        /// <param name="capable">The capable instance.</param>
        /// <param name="capability">What capabilities need to be get.</param>
        protected virtual Type[] GetCapabilityImplementationTypes(ICapable capable, Type capability)
        {
            var collection = new List<Type>();
            var capabilities = capable.GetCapabilities();
            foreach (var capabilityImplementation in capabilities ?? Array.Empty<Type>())
            {
                if (capability.IsAssignableFrom(capabilityImplementation))
                {
                    collection.Add(capabilityImplementation);
                }
            }

            return collection.ToArray();
        }

        /// <summary>
        /// Retrieves the path a package is installed to.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="global">Whether is global package.</param>
        private string GetInstalledPath(IPackage package, bool global = false)
        {
            if (global)
            {
                return globalBucket.GetInstallationManager().GetInstalledPath(package);
            }

            return bucket.GetInstallationManager().GetInstalledPath(package);
        }

        /// <summary>
        /// Load all plugins and installers from a repository.
        /// </summary>
        /// <remarks>
        /// Please call this method as early as possible. because plugins in the specified
        /// repository that rely on events that have fired prior to loading will be missed.
        /// </remarks>
        private void LoadRepository(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            // We always give priority to activating highly
            // dependent weighted packages.
            var packages = repository.GetPackages();
            var sortedPackages = PackageSorter.SortPackages(packages);
            foreach (var package in sortedPackages)
            {
                if (!(package is IPackageComplete))
                {
                    continue;
                }

                if (package.GetPackageType() == PluginType)
                {
                    ActivatePackages(package);
                }
            }
        }
    }
}

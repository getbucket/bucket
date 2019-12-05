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
using Bucket.Installer;
using Bucket.IO;
using Bucket.Package;
using Bucket.Package.Loader;
using Bucket.Plugin;
using Bucket.Plugin.Capability;
using Bucket.Repository;
using Bucket.Tester;
using Bucket.Tests.Support;
using GameBox.Console;
using GameBox.Console.EventDispatcher;
using GameBox.Console.Output;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using BEventDispatcher = Bucket.EventDispatcher.EventDispatcher;

namespace Bucket.Tests.Plugin
{
    [TestClass]
    public class TestsPluginManager
    {
        private Mock<IRepositoryInstalled> repositoryInstalled;
        private Mock<RepositoryManager> repositoryManager;
        private Mock<InstallationManager> installationManager;
        private PluginManager pluginManager;
        private TesterIOConsole tester;
        private IIO io;
        private Bucket bucket;
        private Config config;
        private IEventDispatcher dispatcher;
        private string root;

        [TestInitialize]
        public void TestInitialize()
        {
            root = Helper.Fixtrue("Plugin");
            repositoryInstalled = new Mock<IRepositoryInstalled>();
            tester = new TesterIOConsole();
            io = tester.Mock(AbstractTester.OptionVerbosity(OutputOptions.VerbosityDebug));
            bucket = new Bucket();
            config = new Config();
            dispatcher = new BEventDispatcher(bucket, io);
            pluginManager = new PluginManager(io, bucket);
            repositoryManager = new Mock<RepositoryManager>(io, config, dispatcher, null);
            repositoryInstalled = new Mock<IRepositoryInstalled>();
            installationManager = new Mock<InstallationManager>();

            repositoryManager.Setup((o) => o.GetLocalInstalledRepository()).Returns(repositoryInstalled.Object);
            installationManager.Setup((o) => o.GetInstalledPath(It.IsAny<IPackage>()))
                .Returns(root);

            bucket.SetConfig(config);
            bucket.SetPluginManager(pluginManager);
            bucket.SetEventDispatcher(dispatcher);
            bucket.SetRepositoryManager(repositoryManager.Object);
            bucket.SetInstallationManager(installationManager.Object);
        }

        [TestMethod]
        [DataFixture("bucket-v1.json")]
        public void TestActivatePackage(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);

            Assert.AreEqual(2, pluginManager.GetPlugins().Length);

            var display = tester.GetDisplay();
            StringAssert.Contains(display, "Activate foo");
            StringAssert.Contains(display, "Activate bar");

            dispatcher.Dispatch("foo", this, null);
            dispatcher.Dispatch("bar", this, null);
            display = tester.GetDisplay();
            StringAssert.Contains(display, "Trigger foo event");
            StringAssert.Contains(display, "Trigger bar event");
        }

        [TestMethod]
        [DataFixture("bucket-v1.json")]
        public void TestDeactivatePackage(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);
            pluginManager.DeactivatePackage(package);

            dispatcher.Dispatch("foo", this, null);
            dispatcher.Dispatch("bar", this, null);

            var display = tester.GetDisplay();
            StringAssert.Contains(display, "Deactivate foo");
            StringAssert.Contains(display, "Deactivate bar");
            StringAssert.That.NotContains(display, "Trigger foo event");
            StringAssert.That.NotContains(display, "Trigger bar event");
        }

        [TestMethod]
        [DataFixture("bucket-v1.json")]
        public void TestUninstallPackage(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);
            pluginManager.UninstallPackage(package);

            var display = tester.GetDisplay();
            StringAssert.Contains(display, "Deactivate foo");
            StringAssert.Contains(display, "Deactivate bar");
            StringAssert.Contains(display, "Uninstall foo");
            StringAssert.Contains(display, "Uninstall bar");
        }

        [TestMethod]
        [DataFixture("bucket-v1.json")]
        public void TestLoadInstalledPlugins(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            repositoryInstalled.Setup((o) => o.GetPackages()).Returns(new[]
            {
                package,
            });

            pluginManager.LoadInstalledPlugins();

            dispatcher.Dispatch("foo", this, null);
            dispatcher.Dispatch("bar", this, null);

            var display = tester.GetDisplay();
            StringAssert.Contains(display, "Activate foo");
            StringAssert.Contains(display, "Activate bar");
            StringAssert.Contains(display, "Trigger foo event");
            StringAssert.Contains(display, "Trigger bar event");
        }

        [TestMethod]
        [DataFixture("bucket-v1.json")]
        public void TestGetAllCapabilities(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);

            var capabilities = pluginManager.GetAllCapabilities<ICommandProvider>(bucket, io);

            Assert.AreEqual(1, capabilities.Length);
            var command = capabilities[0].GetCommands().First();

            var testerCommand = new TesterCommand(command);
            Assert.AreEqual(ExitCodes.Normal, testerCommand.Execute());

            var display = testerCommand.GetDisplay();
            StringAssert.Contains(display, "Command foo");
        }

        [TestMethod]
        [DataFixture("bucket-v1.json")]
        public void TestGetPluginCapabilities(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);

            var plugins = pluginManager.GetPlugins();
            var foo = Array.Find(plugins, (plugin) => plugin.Name == "foo");
            var bar = Array.Find(plugins, (plugin) => plugin.Name == "bar");

            var capabilities = pluginManager.GetPluginCapabilities<ICommandProvider>(foo);
            Assert.AreEqual(1, capabilities.Length);

            capabilities = pluginManager.GetPluginCapabilities<ICommandProvider>(bar);
            Assert.AreEqual(0, capabilities.Length);
        }

        [TestMethod]
        [DataFixture("bucket-v1.json")]
        [ExpectedExceptionAndMessage(
            typeof(RuntimeException),
            "Plugin \"plugin-v1\" could not be created, The type \"Example.Plugin.PluginFailed\" must have a no-argument constructor.")]
        public void TestActivatePackagesFailOnMissing(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package, true);
        }

        [TestMethod]
        [DataFixture("bucket-v2.json")]
        public void TestLoadRepeatedlyPluginAssembly(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);

            var display = tester.GetDisplay();
            StringAssert.Contains(display, "is loaded repeatedly, auto skip");
        }

        [TestMethod]
        [DataFixture("bucket-v3.json")]
        public void TestUnsatisfiedApiVersion(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);

            var display = tester.GetDisplay();
            StringAssert.Contains(display, "plugin was skipped because it requires a Plugin API version");
        }

        [TestMethod]
        [DataFixture("bucket-v4.json", "is missing a require statement for a version of the \"bucket-plugin-api\" package")]
        [DataFixture("bucket-v5.json", "plugin packages should have a \"plugin\" defined in their \"extra\" key to be usable.")]
        [DataFixture("bucket-v6.json", "extra data is invalid, expected type is Array or String")]
        public void TestMissingRequiredData(ConfigBucket bucket, string expectedExceptionMessage)
        {
            var package = new LoaderPackage().Load(bucket);

            try
            {
                pluginManager.ActivatePackages(package);
                Assert.Fail($"Expected throw exception, and contains error message: {expectedExceptionMessage}");
            }
            catch (RuntimeException ex)
            {
                if (!ex.Message.Contains(expectedExceptionMessage, StringComparison.Ordinal))
                {
                    Assert.Fail($"Did not contain the expected exception message, expcted: \"{expectedExceptionMessage}\", actual: \"{ex.Message}\"");
                }
            }
        }

        [TestMethod]
        [DataFixture("bucket-v7.json")]
        public void TestPluginAssemblyNotFound(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);

            var display = tester.GetDisplay();
            StringAssert.Contains(display, "is not found.");
        }

        [TestMethod]
        [DataFixture("bucket-v8.json")]
        public void TestNotPluginTypePackage(ConfigBucket bucket)
        {
            var package = new LoaderPackage().Load(bucket);
            pluginManager.ActivatePackages(package);

            Assert.AreEqual(0, pluginManager.GetPlugins().Length);
        }
    }
}

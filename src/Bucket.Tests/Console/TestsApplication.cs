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

using Bucket.Command;
using Bucket.Exception;
using Bucket.IO;
using Bucket.Plugin;
using Bucket.Plugin.Capability;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BApplication = Bucket.Console.Application;

namespace Bucket.Tests.Console
{
    [TestClass]
    public class TestsApplication
    {
        private Bucket bucket;
        private TesterApplication testerApplication;
        private Mock<BApplication> application;
        private Mock<PluginManager> pluginManager;
        private Mock<ICommandProvider> commandProvider;

        [TestInitialize]
        public void Initialize()
        {
            bucket = new Bucket();
            application = new Mock<BApplication>() { CallBase = true };
            application.Setup((o) => o.GetBucket(It.IsAny<bool>(), It.IsAny<bool?>())).Returns(bucket);
            testerApplication = new TesterApplication(application.Object);
            pluginManager = new Mock<PluginManager>(IONull.That, bucket, null, false);
            bucket.SetPluginManager(pluginManager.Object);
            commandProvider = new Mock<ICommandProvider>();
            pluginManager.Setup((o) => o.GetAllCapabilities<ICommandProvider>(It.IsAny<object[]>()))
                .Returns(new[] { commandProvider.Object });
        }

        [TestMethod]
        public void TestCallCommandAbout()
        {
            var tester = new TesterApplication(new BApplication());

            Assert.AreEqual(ExitCodes.Normal, tester.Run("about --no-plugins"));
            StringAssert.Contains(
                tester.GetDisplay(), @"
Bucket - Package Dependency Manager
Bucket is a dependency manager tracking local dependencies of your projects and libraries.
See https://github.com/getbucket/bucket/wiki for more information.

");
        }

        [TestMethod]
        public void TestGetHelp()
        {
            StringAssert.Contains(
                application.Object.GetHelp(), @"
    ____             __        __ 
   / __ )__  _______/ /_____  / /_
  / __  / / / / ___/ //_/ _ \/ __/
 / /_/ / /_/ / /__/ ,< /  __/ /_  
/_____/\__,_/\___/_/|_|\___/\__/  

Bucket");
        }

        [TestMethod]
        public void TestGetLongVersion()
        {
            StringAssert.Contains(application.Object.GetLongVersion(), "Bucket");
        }

        [TestMethod]
        public void TestGetIO()
        {
            Assert.IsNotNull(application.Object.GetIO());
        }

        [TestMethod]
        public void TestLoadPluginCommands()
        {
            var commandMock = new Mock<BaseCommand>() { CallBase = true };
            commandMock.Object.SetName("plugin-command");
            commandProvider.Setup((o) => o.GetCommands()).Returns(new[] { commandMock.Object });

            commandMock.Setup((o) => o.Run(It.IsAny<IInput>(), It.IsAny<IOutput>()))
                .Returns(ExitCodes.Normal).Verifiable();

            testerApplication.Run("plugin-command");

            commandMock.Verify();
        }

        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(UnexpectedException), "returned an invalid value null. we expected an BaseCommand instance.")]
        public void TestLoadPluginCommandsContainsNullElement()
        {
            application.Object.CatchExceptions = false;
            var commandMock = new Mock<BaseCommand>() { CallBase = true };
            commandMock.Object.SetName("plugin-command");
            commandProvider.Setup((o) => o.GetCommands())
                .Returns(new[] { commandMock.Object, null });

            testerApplication.Run("plugin-command");
        }
    }
}

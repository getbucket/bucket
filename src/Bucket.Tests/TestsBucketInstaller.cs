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
using Bucket.Console;
using Bucket.Downloader;
using Bucket.Exception;
using Bucket.IO;
using Bucket.Json;
using Bucket.Package;
using Bucket.Repository;
using Bucket.Tester;
using Bucket.Tests.Mock;
using Bucket.Util;
using GameBox.Console.EventDispatcher;
using GameBox.Console.Output;
using GameBox.Console.Tester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using SException = System.Exception;

namespace Bucket.Tests
{
    [TestClass]
    public class TestsBucketInstaller
    {
        private IIO io;
        private TesterIOConsole tester;
        private string tempHome;

        public static IEnumerable<object[]> ProvideInstaller()
        {
            var cases = new List<object[]>();

            // when A requires B and B requires A, and A is a non-published
            // root package the install of B should succeed.
            var a = Helper.GetPackage<IPackageRoot>("A", "1.0.0");
            a.SetRequires(new[]
            {
                new Link("A", "B", Helper.Constraint("=", "1.0.0"), null, "1.0.0"),
            });

            var b = Helper.GetPackage<IPackageRoot>("B", "1.0.0");
            b.SetRequires(new[]
            {
                new Link("B", "A", Helper.Constraint("=", "1.0.0"), null, "1.0.0"),
            });

            cases.Add(new object[]
            {
                a,
                new IRepository[] { new RepositoryArray(new[] { b }) },
                new IPackage[] { b },
                Array.Empty<(IPackage, IPackage)>(),
                Array.Empty<IPackage>(),
            });

            // when A requires B and B requires A, and A is a published root
            // package only B should be installed, as A is the root
            a = Helper.GetPackage<IPackageRoot>("A", "1.0.0");
            a.SetRequires(new[]
            {
                new Link("A", "B", Helper.Constraint("=", "1.0.0"), null, "1.0.0"),
            });

            b = Helper.GetPackage<IPackageRoot>("B", "1.0.0");
            b.SetRequires(new[]
            {
                new Link("B", "A", Helper.Constraint("=", "1.0.0"), null, "1.0.0"),
            });

            cases.Add(new object[]
            {
                a,
                new IRepository[] { new RepositoryArray(new[] { a, b }) },
                new IPackage[] { b },
                Array.Empty<(IPackage, IPackage)>(),
                Array.Empty<IPackage>(),
            });

            return cases;
        }

        public static IEnumerable<object[]> ProvideIntegrationTests()
        {
            var fixtureDir = Helper.Fixtrue("Integration/BucketInstaller");
            var cases = new List<object[]>();

            var files = Directory.GetFiles(fixtureDir, "*.test", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var testData = ReadTestFile<TestData>(file, fixtureDir);

                try
                {
                    var message = testData.Test;
                    var bucketFile = JsonFile.Parse<ConfigBucket>(testData.Bucket);
                    var run = testData.Run.Trim();
                    var expect = testData.Expect;
                    var expectOutput = string.IsNullOrEmpty(testData.ExpectOutput) ? null : testData.ExpectOutput;
                    ConfigLocker lockerFile = null;
                    if (!string.IsNullOrEmpty(testData.Lock))
                    {
                        lockerFile = JsonFile.Parse<ConfigLocker>(testData.Lock);
                        if (string.IsNullOrEmpty(lockerFile.ContentHash))
                        {
                            // don't use testData.Bucket maybe indentation inconsistent.
                            lockerFile.ContentHash = Locker.GetContentHash(JsonConvert.SerializeObject(bucketFile));
                        }
                    }

                    ConfigInstalled installed = null;
                    if (!string.IsNullOrEmpty(testData.Installed))
                    {
                        installed = JsonFile.Parse<ConfigInstalled>(testData.Installed);
                    }

                    ConfigLocker expectLocker = null;
                    if (!string.IsNullOrEmpty(testData.ExpectLock))
                    {
                        expectLocker = JsonFile.Parse<ConfigLocker>(testData.ExpectLock);
                    }

                    object expectResult;
                    if (!string.IsNullOrEmpty(testData.ExpectException))
                    {
                        expectResult = testData.ExpectException;
                        if (!string.IsNullOrEmpty(testData.ExpectExitCode))
                        {
                            throw new RuntimeException("EXPECT-EXCEPTION and EXPECT-EXIT-CODE are mutually exclusive");
                        }
                    }
                    else if (!string.IsNullOrEmpty(testData.ExpectExitCode))
                    {
                        expectResult = int.Parse(testData.ExpectExitCode);
                    }
                    else
                    {
                        expectResult = 0;
                    }

                    cases.Add(new[]
                    {
                        file.Replace(fixtureDir, string.Empty, StringComparison.Ordinal).TrimStart('\\', '/'),
                        message,
                        bucketFile,
                        lockerFile,
                        installed,
                        run,
                        expectLocker,
                        expectOutput,
                        expect,
                        expectResult,
                    });
                }
#pragma warning disable CA1031
                catch (SException ex)
#pragma warning restore CA1031
                {
                    throw new UnexpectedException(
                        $"Test \"{file.Replace(fixtureDir, string.Empty, StringComparison.Ordinal)}\" is not valid: {ex.Message}", ex);
                }
            }

            return cases;
        }

        [TestInitialize]
        public void Initialize()
        {
            tester = new TesterIOConsole();
            io = tester.Mock(AbstractTester.OptionVerbosity(OutputOptions.VerbosityDebug));
        }

        [TestMethod]
        [DynamicData("ProvideInstaller", DynamicDataSourceType.Method)]
        public void TestInstaller(
            IPackageRoot packageRoot,
            IRepository[] repositories,
            IPackage[] expectedInstalled,
            (IPackage Initial, IPackage Target)[] expectedUpdated,
            IPackage[] expectedUninstalled)
        {
            var config = new Mock<Config>(true, null) { CallBase = true };
            var downloadManager = new Mock<DownloadManager>(null, false) { CallBase = true };
            var repositoryManager = new Mock<RepositoryManager>(IONull.That, config.Object, null, null) { CallBase = true };
            var installationManager = new MockInstallationManager();
            var jsonFile = new Mock<JsonFile>(string.Empty, null, null);
            var locker = new Mock<Locker>(IONull.That, jsonFile.Object, installationManager, "{}");
            var eventDispatcher = new Mock<IEventDispatcher>();
            var bucket = new Bucket();

            bucket.SetConfig(config.Object);
            bucket.SetDownloadManager(downloadManager.Object);
            bucket.SetRepositoryManager(repositoryManager.Object);
            bucket.SetLocker(locker.Object);
            bucket.SetInstallationManager(installationManager);
            bucket.SetEventDispatcher(eventDispatcher.Object);
            bucket.SetPackage((IPackageRoot)packageRoot.Clone());
            repositoryManager.Object.SetLocalInstalledRepository(new RepositoryArrayInstalled());

            foreach (var repository in repositories)
            {
                repositoryManager.Object.AddRepository(repository);
            }

            var installer = new BucketInstaller(io, bucket);
            Assert.AreEqual(0, installer.Run());

            CollectionAssert.AreEqual(expectedInstalled, installationManager.GetInstalledPackages());
            CollectionAssert.AreEqual(expectedUninstalled, installationManager.GetUninstalledPackages());

            var actualUpdated = installationManager.GetUpdatedPackages();
            Assert.AreEqual(expectedUpdated.Length, actualUpdated.Length);
            for (var i = 0; i < expectedUpdated.Length; i++)
            {
                Assert.AreEqual(expectedUpdated[i].Initial, actualUpdated[i].Initial);
                Assert.AreEqual(expectedUpdated[i].Target, actualUpdated[i].Target);
            }
        }

        [TestMethod]
        [DynamicData("ProvideIntegrationTests", DynamicDataSourceType.Method)]
        public void TestIntegration(
            string testFile,
            string message,
            ConfigBucket bucketConfig,
            ConfigLocker lockerConfig,
            ConfigInstalled installed,
            string run,
            ConfigLocker expectLocker,
            string expectOutput,
            string expect,
            object expectResult)
        {
            io = tester.Mock(AbstractTester.OptionVerbosity(OutputOptions.VerbosityNormal));

            // viriable "expect" may be the expected exception message,
            // or the action that is installationManager to trace.
            string expectedException = null;
            string expectedExceptionMessage = null;
            if (!(expectResult is int))
            {
                expectedExceptionMessage = expect.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
                expectedException = expectResult.ToString();
            }

            string GetDebugMessage()
            {
                return $"Some exceptions have occurred, in test file \"{testFile}\"({message}){Environment.NewLine}";
            }

            try
            {
                var factory = new Mock.MockFactory();
                var bucket = factory.CreateBucket(io, bucketConfig);
                tempHome = bucket.GetConfig().Get(Settings.Home);

                var installedJsonFile = new Mock<JsonFile>(string.Empty, null, null);
                installedJsonFile.Setup((o) => o.Read<ConfigInstalled>()).Returns(installed);
                installedJsonFile.Setup((o) => o.Exists()).Returns(installed != null);

                var repositoryManager = bucket.GetRepositoryManager();
                repositoryManager.SetLocalInstalledRepository(new MockRepositoryFilesystemInstalled(installedJsonFile.Object));

                var lockerJsonFile = new Mock<JsonFile>(string.Empty, null, null);
                lockerJsonFile.Setup((o) => o.Read<ConfigLocker>()).Returns(lockerConfig);
                lockerJsonFile.Setup((o) => o.Exists()).Returns(lockerConfig != null);

                ConfigLocker actualLocker = null;
                if (expectLocker != null)
                {
                    lockerJsonFile.Setup((o) => o.Write(It.Is<object>((arg) => arg is ConfigLocker)))
                        .Callback<object>((data) =>
                    {
                        actualLocker = (ConfigLocker)data;
                    });
                }

                var contents = JsonConvert.SerializeObject(bucketConfig);
                var locker = new Locker(io, lockerJsonFile.Object, bucket.GetInstallationManager(), contents);
                bucket.SetLocker(locker);

                var eventDispatcher = new Mock<IEventDispatcher>();
                bucket.SetEventDispatcher(eventDispatcher.Object);

                var installer = new BucketInstaller(io, bucket);
                var application = new Application();
                application.Get("install").SetCode((input, output) =>
                {
                    installer.SetDevMode(!input.GetOption("no-dev"))
                             .SetDryRun(input.GetOption("dry-run"))
                             .SetIgnorePlatformRequirements(input.GetOption("ignore-platform-reqs"));

                    return installer.Run();
                });

                application.Get("update").SetCode((input, output) =>
                {
                    string[] packages = input.GetArgument("packages");
                    var whitlistPackages = new HashSet<string>();
                    if (!(packages is null))
                    {
                        Array.ForEach(packages, (package) => whitlistPackages.Add(package));
                    }

                    installer.SetDevMode(!input.GetOption("no-dev"))
                    .SetUpdate(true)
                    .SetDryRun(input.GetOption("dry-run"))
                    .SetUpdateWhitelist(whitlistPackages.Empty() ? null : whitlistPackages)
                    .SetWhitelistTransitiveDependencies(input.GetOption("with-dependencies"))
                    .SetWhitelistAllDependencies(input.GetOption("with-all-dependencies"))
                    .SetPreferStable(input.GetOption("prefer-stable"))
                    .SetPreferLowest(input.GetOption("prefer-lowest"))
                    .SetIgnorePlatformRequirements(input.GetOption("ignore-platform-reqs"));

                    return installer.Run();
                });

                if (!Regex.IsMatch(run, @"^(install|update)\b"))
                {
                    throw new UnexpectedException("The run command only supports install and update");
                }

                var testerApplication = new TesterApplication(application);

                var actualExitCode = testerApplication.Run(run, AbstractTester.Interactive(false));
                var actualOutput = tester.GetDisplay();
                var appOutput = testerApplication.GetDisplay().Replace("\r", "\n", StringComparison.Ordinal);

                // Shouldn't check output and results if an exception was expected by this point
                if (!(expectResult is int expectExitCode))
                {
                    return;
                }

                Assert.AreEqual(expectExitCode, actualExitCode, actualOutput + appOutput);
                if (expectLocker != null)
                {
                    actualLocker.ContentHash = null;
                    Assert.IsTrue(Locker.LockerAreEqual(expectLocker, actualLocker), "Locked file expected and actual does not match.");
                }

                var installationManager = (MockInstallationManager)bucket.GetInstallationManager();

                Assert.AreEqual(
                    expect.Replace("\r", string.Empty, StringComparison.Ordinal).Trim(),
                    string.Join("\n", installationManager.GetTrace()).Trim());

                if (!string.IsNullOrEmpty(expectOutput))
                {
                    Assert.AreEqual(
                        expectOutput.Replace("\r", string.Empty, StringComparison.Ordinal).Trim(),
                        actualOutput.Replace("\r", string.Empty, StringComparison.Ordinal).Trim());
                }
            }
            catch (SException ex) when (!string.IsNullOrEmpty(expectedException))
            {
                if (expectedException != ex.GetType().Name)
                {
                    Assert.Fail($"{GetDebugMessage()}Need throw exception: {expectedException}, actual throw: {ex.GetType()}.");
                }

                if (!string.IsNullOrEmpty(expectedExceptionMessage) &&
                    !Str.Is(expectedExceptionMessage, ex.Message.Replace("\r\n", "\n", StringComparison.Ordinal)))
                {
                    Assert.Fail($"{GetDebugMessage()}Expected exception message: {expectedExceptionMessage}, actual message: {ex.Message}.");
                }

                return;
            }

            if (!string.IsNullOrEmpty(expectedException))
            {
                Assert.Fail($"{GetDebugMessage()}Need throw exception: {expectedException}.");
            }
        }

        protected static T ReadTestFile<T>(string file, string fixtureDir)
            where T : new()
        {
            var data = new T();
            var content = File.ReadAllText(file);
            var tokens = Regex.Split(content, @"(?:^|\n*|(?:\r\n)*)--([A-Z-]+)--\r?\n");

            var sectionInfos = new Dictionary<string, (PropertyInfo Property, SectionAttribute Options)>();
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttributes(typeof(SectionAttribute), false);
                if (attribute.Length <= 0)
                {
                    continue;
                }

                var sectionAttribute = (SectionAttribute)attribute[0];
                sectionInfos[Str.LowerDashes(property.Name).ToUpper()] = (property, sectionAttribute);
            }

            string section = null;
            (PropertyInfo Property, SectionAttribute Options) sectionMeta = (null, null);
            foreach (var token in tokens)
            {
                if (section == null && string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (section == null)
                {
                    if (!sectionInfos.TryGetValue(token, out sectionMeta))
                    {
                        throw new RuntimeException($"The test file \"{file.Replace(fixtureDir, string.Empty, StringComparison.Ordinal)}\" must not contain a section named \"{token}\".");
                    }

                    section = token;
                    continue;
                }

                sectionMeta.Property.SetValue(data, token);
                section = null;
            }

            foreach (var sectionInfo in sectionInfos)
            {
                if (!sectionInfo.Value.Options.Required)
                {
                    continue;
                }

                if (sectionInfo.Value.Property.GetValue(data) == null)
                {
                    throw new RuntimeException($"The test file \"{file.Replace(fixtureDir, string.Empty, StringComparison.Ordinal)}\" must have a section named \"{sectionInfo.Key}\".");
                }
            }

            return data;
        }

        protected sealed class TestData
        {
            [Section(Required = true)]
            public string Test { get; set; }

            [Section(Required = true)]
            public string Bucket { get; set; }

            [Section(Required = false)]
            public string Lock { get; set; }

            [Section(Required = false)]
            public string Installed { get; set; }

            [Section(Required = true)]
            public string Run { get; set; }

            [Section(Required = false)]
            public string ExpectLock { get; set; }

            [Section(Required = false)]
            public string ExpectOutput { get; set; }

            [Section(Required = false)]
            public string ExpectExitCode { get; set; }

            [Section(Required = false)]
            public string ExpectException { get; set; }

            [Section(Required = true)]
            public string Expect { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property)]
        private class SectionAttribute : Attribute
        {
            public bool Required { get; set; } = true;
        }
    }
}

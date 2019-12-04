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

using Bucket.Archive;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.Tester;
using Bucket.Tests.Support.MockExtension;
using GameBox.Console.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;

namespace Bucket.Tests.Archive
{
    [TestClass]
    public class TestsExtractorZip
    {
        private Mock<IFileSystem> fileSystem;
        private Mock<IProcessExecutor> process;
        private Mock<ExtractorZip> extractor;
        private TesterIOConsole tester;

        [TestInitialize]
        public void Initialize()
        {
            fileSystem = new Mock<IFileSystem>();
            process = new Mock<IProcessExecutor>();
            tester = new TesterIOConsole();
            extractor = new Mock<ExtractorZip>(
                tester.Mock(),
                fileSystem.Object,
                process.Object)
            { CallBase = true };
        }

        [TestMethod]
        public void TestExtractWithZipArchive()
        {
            var file = Helper.Fixtrue("archive/sample.zip");

            fileSystem.Setup((o) => o.Read(file))
                .Returns(File.OpenRead(file));
            fileSystem.Setup((o) => o.Write(Path.Combine("/path", "README.md"), It.IsAny<Stream>(), false))
                .Callback<string, Stream, bool>((path, stream, append) =>
                {
                    Assert.AreEqual("README", stream.ToText());
                })
                .Verifiable();

            fileSystem.Setup((o) => o.Write(Path.Combine("/path", "srcs/code.cs"), It.IsAny<Stream>(), false))
                .Callback<string, Stream, bool>((path, stream, append) =>
                {
                    Assert.AreEqual("# code", stream.ToText());
                })
                .Verifiable();

            extractor.Object.ExtractWithZipArchive(file, "/path");

            Moq.Mock.Verify(fileSystem, process);
        }

        [TestMethod]
        public void TestExtractWithZipArchiveFallback()
        {
            var file = Helper.Fixtrue("archive/sample.zip");

            process.Setup("unzip", returnValue: () => 0)
                .Verifiable();

            process.Setup((o) => o.Execute(
                    It.IsRegex(@"^unzip\s-qq"),
                    out It.Ref<string[]>.IsAny,
                    out It.Ref<string[]>.IsAny,
                    It.IsAny<string>()))
                .Returns(0)
                .Verifiable();

            fileSystem.Setup((o) => o.Read(file))
                    .Throws<UnexpectedException>();

            extractor.Object.ExtractWithZipArchive(file, "/path");

            Moq.Mock.Verify(fileSystem, process);

            StringAssert.Contains(
                tester.GetDisplay(),
                "Unzip with ZipArchive failed, falling back to unzip command");
        }

        [TestMethod]
        public void TestExtractWithUnzipCommand()
        {
            var file = Helper.Fixtrue("archive/sample.zip");

            process.Setup((o) => o.Execute(
                    It.IsRegex(@"^unzip\s-qq"),
                    out It.Ref<string[]>.IsAny,
                    out It.Ref<string[]>.IsAny,
                    It.IsAny<string>()))
                .Returns(0)
                .Verifiable();

            extractor.Object.ExtractWithUnzipCommand(file, "/path");

            Moq.Mock.Verify(process);
        }

        [TestMethod]
        public void TestExtractWithUnzipCommandFallback()
        {
            var file = Helper.Fixtrue("archive/sample.zip");

            process.Setup((o) => o.Execute(
                    It.IsRegex(@"^unzip\s-qq"),
                    out It.Ref<string[]>.IsAny,
                    out It.Ref<string[]>.IsAny,
                    It.IsAny<string>()))
                .Returns(1)
                .Verifiable();

            fileSystem.Setup((o) => o.Read(file))
                .Returns(File.OpenRead(file));
            fileSystem.Setup((o) => o.Write(Path.Combine("/path", "README.md"), It.IsAny<Stream>(), false))
                .Callback<string, Stream, bool>((path, stream, append) =>
                {
                    Assert.AreEqual("README", stream.ToText());
                })
                .Verifiable();

            fileSystem.Setup((o) => o.Write(Path.Combine("/path", "srcs/code.cs"), It.IsAny<Stream>(), false))
                .Callback<string, Stream, bool>((path, stream, append) =>
                {
                    Assert.AreEqual("# code", stream.ToText());
                })
                .Verifiable();

            extractor.Object.ExtractWithUnzipCommand(file, "/path");

            Moq.Mock.Verify(fileSystem, process);

            StringAssert.Contains(
                tester.GetDisplay(),
                "Unzip with unzip command failed, falling back to ZipArchive.");
        }
    }
}

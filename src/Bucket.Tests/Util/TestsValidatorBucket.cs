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

#pragma warning disable SA1204

using Bucket.FileSystem;
using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsValidatorBucket
    {
        private ValidatorBucket validator;
        private IFileSystem fileSystem;

        [TestInitialize]
        public void Init()
        {
            fileSystem = new FileSystemLocal(Helper.Fixtrue("Util/"));
            validator = new ValidatorBucket(fileSystem);
        }

        [TestMethod]
        [DataRow("invalid-bucket-1.json", "Required properties are missing from object: name, version.")]
        [DataRow("invalid-bucket-2.json", "Required properties are missing from object: version.")]
        [DataRow("invalid-bucket-3.json", "Property 'invalid-property' has not been defined and the schema does not allow additional properties.")]
        [DataRow("invalid-bucket-file-not-found.json", "Could not find file")]
        public void TestValidatorInvalidPackage(string file, string expected)
        {
            var (warnings, publishErrors, errors) = validator.Validate(file);
            Assert.AreEqual(1, errors.Length);
            StringAssert.Contains(errors[0], expected);
        }

        [TestMethod]
        [DynamicData("ValidateData", DynamicDataSourceType.Method)]
        public void TestValidate(string file, string[] expectedWarnings, string[] expectedPublishErrors, string[] expectedErrors)
        {
            var (warnings, publishErrors, errors) = validator.Validate(file);

            Array.ForEach(warnings, System.Console.WriteLine);
            Array.ForEach(publishErrors, System.Console.WriteLine);
            Array.ForEach(errors, System.Console.WriteLine);

            Assert.AreEqual(expectedWarnings.Length, warnings.Length);
            Assert.AreEqual(expectedPublishErrors.Length, publishErrors.Length);
            Assert.AreEqual(expectedErrors.Length, errors.Length);

            CollectionAssert.AreEqual(expectedWarnings, warnings);
            CollectionAssert.AreEqual(expectedPublishErrors, publishErrors);
            CollectionAssert.AreEqual(expectedErrors, errors);
        }

        public static IEnumerable<object[]> ValidateData()
        {
            return new[]
            {
                new object[]
                {
                    "bucket-validate-1.json",
                    new[]
                    {
                        "No license specified, it is recommended to do so. For closed-source software you may use \"proprietary\" as license.",
                        "bar, baz are required both in require and require-dev, this can lead to unexpected behavior.",
                    },
                    Array.Empty<string>(),
                    new[]
                    {
                        "The \"version\" property not allowed to be empty.",
                    },
                },
                new object[]
                {
                    "bucket-validate-2.json",
                    new[]
                    {
                        "It is recommended to add a provider name to the package (e.g. provide-name/package-name).",
                        "The package \"bar\" is pointing to a commit-ref, this is bad practice and can cause unforeseen issues.",
                        "The package \"boo\" is pointing to a commit-ref, this is bad practice and can cause unforeseen issues.",
                    },
                    new[]
                    {
                        "Name \"fooBar\" does not match the best practice (e.g. lower-cased/with-dashes). We suggest using \"foo-bar\" instead.",
                    },
                    Array.Empty<string>(),
                },
            };
        }
    }
}

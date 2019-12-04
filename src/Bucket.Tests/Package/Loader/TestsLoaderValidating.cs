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
using Bucket.Package;
using Bucket.Package.Loader;
using Bucket.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Package.Loader
{
    [TestClass]
    public class TestsLoaderValidating
    {
        private LoaderValidating loader;

        [TestInitialize]
        public void Initialize()
        {
            loader = new LoaderValidating(new LoaderPackage());
        }

        [TestMethod]
        [DataFixture("validating.json")]
        public void TestLoadFullAmountValidating(ConfigBucket config)
        {
            try
            {
                loader.Load(config);
                Assert.Fail("Validating need throw InvalidPackageException exception.");
            }
            catch (InvalidPackageException ex)
            {
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "Property \"version\" is invalid value (invalid-version): Invalid version string \"invalid-version\".",
                        "require.baz : invalid version constraint (Could not parse version constraint \"invalid-require\" : Invalid version string \"invalid-require\")",
                    }, ex.GetErrors());
            }

            CollectionAssert.AreEqual(
                    new[]
                    {
                        "Property \"type\" : invalid value (invalid-type(*)), must match [A-Za-z0-9-]+",
                        "Authors bar email : invalid value (email-invalid), must be a valid email address.",
                        "Property \"foo\" is invalid, please use: email, issues, forum, source, docs, wiki",
                    }, loader.GetWarnings());
        }

        [TestMethod]
        [DataFixture("validating-warnings.json")]
        public void TestLoadFullAmountValidatingWarnings(ConfigBucket config)
        {
            var package = loader.Load<IPackageComplete>(config);
            Assert.IsTrue(string.IsNullOrEmpty(package.GetPackageType()));
            Assert.AreEqual(2, package.GetSupport().Count);
            Assert.AreEqual(2, package.GetAuthors().Length);
            Assert.IsTrue(string.IsNullOrEmpty(package.GetAuthors()[1].Email));
        }
    }
}

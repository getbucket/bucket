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
using Bucket.Installer;
using Bucket.IO;
using Bucket.Repository;
using Newtonsoft.Json.Linq;

namespace Bucket.Tests.Mock
{
    public class MockFactory : Factory
    {
        public override Config CreateConfig(IIO io = null, string cwd = null)
        {
            var config = new Config(true, cwd);

            var merged = new JObject() { { "config", new JObject() } };
            merged["config"][Settings.Home] = Helper.GetTestFolder();
            merged["repositories"] = new JArray() { new JObject() };
            merged["repositories"][0][Config.DefaultRepositoryDomain] = false;
            config.Merge(merged);

            return config;
        }

        protected override void InitializeLocalInstalledRepository(IIO io, RepositoryManager manager, string vendorDir)
        {
            // noop.
        }

        protected override InstallationManager CreateInstallationManager()
        {
            return new MockInstallationManager();
        }

        protected override void InitializeDefaultInstallers(InstallationManager installationManager, Bucket bucket, IIO io)
        {
            // noop.
        }

        protected override void PurgePackages(IRepositoryInstalled repositoryInstalled, InstallationManager installationManager)
        {
            // noop.
        }
    }
}

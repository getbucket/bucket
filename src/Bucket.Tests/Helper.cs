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

using Bucket.Package;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Bucket.Util;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BPackage = Bucket.Package.Package;
using BVersionParser = Bucket.Package.Version.VersionParser;

namespace Bucket.Tests
{
    public static class Helper
    {
        private static IVersionParser versionParser;

        public static string GetHome()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                return home;
            }

            home = Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
            {
                return home;
            }

            return null;
        }

        public static string GetTestFolder()
        {
            return Path.Combine(Environment.CurrentDirectory, "tests", Path.GetRandomFileName());
        }

        public static string GetTestFolder<T>()
        {
            return Path.Combine(Environment.CurrentDirectory, "tests", typeof(T).Name);
        }

        public static string Fixtrue(string path)
        {
            return Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Fixtures\\" + path.Replace('/', '\\'));
        }

        public static IConstraint Constraint(string operation, string version)
        {
            var constraint = new Constraint(operation, GetVersionParser().Normalize(version));
            constraint.SetPrettyString(operation + Str.Space + version);
            return constraint;
        }

        public static T GetPackage<T>(string name, string version)
            where T : class, IPackage
        {
            var versionNormalize = GetVersionParser().Normalize(version);
            if (typeof(IPackageRoot).IsAssignableFrom(typeof(T)))
            {
                return new PackageRoot(name, versionNormalize, version) as T;
            }
            else if (typeof(IPackageComplete).IsAssignableFrom(typeof(T)))
            {
                return new PackageComplete(name, versionNormalize, version) as T;
            }
            else
            {
                return new BPackage(name, versionNormalize, version) as T;
            }
        }

        public static IPackage MockPackage(
            string name, string version, Link[] provides = null,
            Link[] replaces = null, Link[] requires = null, Link[] conflict = null,
            string type = null)
        {
            // todo: optimization MockPackage, use GetPackage<T>().
            provides = provides ?? Array.Empty<Link>();
            replaces = replaces ?? Array.Empty<Link>();
            requires = requires ?? Array.Empty<Link>();
            conflict = conflict ?? Array.Empty<Link>();

            var names = new[] { name.ToLower() };
            var package = new Mock<IPackage>();
            IRepository repository = null;
            names = names.Concat(provides.Select((link) => link.GetTarget())).ToArray();
            names = names.Concat(replaces.Select((link) => link.GetTarget())).ToArray();
            package.Setup((o) => o.GetName()).Returns(() => name.ToLower());
            package.Setup((o) => o.GetNamePretty()).Returns(() => name);
            package.Setup((o) => o.GetPackageType()).Returns(() => type);
            package.Setup((o) => o.GetVersion()).Returns(() => GetVersionParser().Normalize(version));
            package.Setup((o) => o.GetVersionPretty()).Returns(() => version);
            package.Setup((o) => o.GetNames()).Returns(() => names);
            package.Setup((o) => o.GetStability()).Returns(() => VersionParser.ParseStability(version));
            package.Setup((o) => o.GetProvides()).Returns(() => provides);
            package.Setup((o) => o.GetReplaces()).Returns(() => replaces);
            package.Setup((o) => o.GetConflicts()).Returns(conflict);
            package.Setup((o) => o.GetRequires()).Returns(() => requires);
            package.Setup((o) => o.ToString()).Returns(() => package.Object.GetNameUnique());
            package.Setup((o) => o.GetPrettyString()).Returns(package.Object.GetNamePretty() + Str.Space + package.Object.GetVersionPretty());
            package.Setup((o) => o.GetNameUnique()).Returns(() => package.Object.GetName() + "-" + package.Object.GetVersion());
            package.Setup((o) => o.Equals(It.IsAny<IPackage>())).Returns((IPackage other) =>
            {
                IPackage self = package.Object;
                if (self is PackageAlias packageAlias)
                {
                    self = packageAlias.GetAliasOf();
                }

                if (other is PackageAlias objPackageAlias)
                {
                    other = objPackageAlias.GetAliasOf();
                }

                return other.GetNameUnique() == self.GetNameUnique();
            });

            package.Setup((o) => o.SetRepository(It.IsAny<IRepository>())).Callback((IRepository repo) =>
            {
                repository = repo;
            });

            package.Setup((o) => o.GetRepository()).Returns(() => repository);

            package.SetupProperty((o) => o.Id);

            return package.Object;
        }

        public static IRepository MockRepository(params IPackage[] packages)
        {
            var repository = new Mock<IRepository>();
            repository.Setup(o => o.GetPackages()).Returns(() => packages);
            return repository.Object;
        }

        public static PackageAlias MockPackageAlias(IPackage package, string version)
        {
            var normalize = GetVersionParser().Normalize(version);
            return new PackageAlias(package is PackageAlias packageAlias ? packageAlias.GetAliasOf() : package, normalize, version);
        }

        public static IVersionParser GetVersionParser()
        {
            return versionParser ?? (versionParser = new BVersionParser());
        }
    }
}

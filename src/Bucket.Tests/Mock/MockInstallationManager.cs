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

using Bucket.DependencyResolver.Operation;
using Bucket.Exception;
using Bucket.Installer;
using Bucket.Package;
using Bucket.Repository;
using System.Collections.Generic;

namespace Bucket.Tests.Mock
{
    public class MockInstallationManager : InstallationManager
    {
        private readonly List<IPackage> installed;
        private readonly List<(IPackage Initial, IPackage Target)> updated;
        private readonly List<IPackage> uninstalled;
        private readonly List<string> trace;

        public MockInstallationManager()
        {
            installed = new List<IPackage>();
            updated = new List<(IPackage Initial, IPackage Target)>();
            uninstalled = new List<IPackage>();
            trace = new List<string>();
        }

        public string[] GetTrace()
        {
            return trace.ToArray();
        }

        public IPackage[] GetInstalledPackages()
        {
            return installed.ToArray();
        }

        public (IPackage Initial, IPackage Target)[] GetUpdatedPackages()
        {
            return updated.ToArray();
        }

        public IPackage[] GetUninstalledPackages()
        {
            return uninstalled.ToArray();
        }

        public override string GetInstalledPath(IPackage package)
        {
            return string.Empty;
        }

        public override bool IsPackageInstalled(IRepositoryInstalled installedRepository, IPackage package)
        {
            return installedRepository.HasPackage(package);
        }

        public override void Execute(IRepositoryInstalled installedRepository, IOperation operation)
        {
            GetExecuteMethod(operation.JobCommand)(installedRepository, operation);
        }

        protected override void Install(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationInstall operationInstall))
            {
                throw new UnexpectedException($"The Install operation must be {nameof(OperationInstall)} instance.");
            }

            installed.Add(operationInstall.GetPackage());
            trace.Add(operation.ToString());
            installedRepository.AddPackage((IPackage)operationInstall.GetPackage().Clone());
        }

        protected override void Update(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationUpdate operationUpdate))
            {
                throw new UnexpectedException($"The Update operation must be {nameof(OperationUpdate)} instance.");
            }

            updated.Add((operationUpdate.GetInitialPackage(), operationUpdate.GetTargetPackage()));
            trace.Add(operation.ToString());

            installedRepository.RemovePackage(operationUpdate.GetInitialPackage());
            installedRepository.AddPackage((IPackage)operationUpdate.GetTargetPackage().Clone());
        }

        protected override void Uninstall(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationUninstall operationUninstall))
            {
                throw new UnexpectedException($"The Uninstall operation must be {nameof(OperationUninstall)} instance.");
            }

            uninstalled.Add(operationUninstall.GetPackage());
            trace.Add(operation.ToString());
            installedRepository.RemovePackage(operationUninstall.GetPackage());
        }

        protected override void MarkAliasInstalled(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationMarkPackageAliasInstalled operationAliasInstall))
            {
                throw new UnexpectedException($"The mark package aliased installed operation must be {nameof(OperationMarkPackageAliasInstalled)} instance.");
            }

            installed.Add(operationAliasInstall.GetPackage());
            trace.Add(operation.ToString());
            base.MarkAliasInstalled(installedRepository, operation);
        }

        protected override void MarkAliasUninstalled(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationMarkPackageAliasUninstall operationAliasUninstall))
            {
                throw new UnexpectedException($"The mark package aliased uninstalled operation must be {nameof(OperationMarkPackageAliasUninstall)} instance.");
            }

            uninstalled.Add(operationAliasUninstall.GetPackage());
            trace.Add(operation.ToString());
            base.MarkAliasUninstalled(installedRepository, operation);
        }
    }
}

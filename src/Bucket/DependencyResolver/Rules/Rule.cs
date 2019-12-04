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
using Bucket.Package;
using Bucket.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bucket.DependencyResolver.Rules
{
    /// <summary>
    /// The base rule.
    /// </summary>
    public abstract class Rule
    {
        private static int objectId = 0;
        private readonly object reasonData;
        private readonly Job job;
        private int bitfield;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class.
        /// </summary>
        /// <param name="reason">The <see cref="Reason"/> describing the reason for generating this rule.</param>
        /// <param name="reasonData">Any data of the reason.</param>
        /// <param name="job">An array of jobs this rule was created from.</param>
        protected Rule(Reason reason, object reasonData, Job job = null)
        {
            this.reasonData = reasonData;
            this.job = job;

            bitfield = (0 << (int)BitFieldDefine.Disabled) |
                       ((int)reason << (int)BitFieldDefine.Reason) |
                       (255 << (int)BitFieldDefine.Type);
            ObjectId = Interlocked.Increment(ref objectId);
        }

        private enum BitFieldDefine
        {
            Type = 0,
            Reason = 8,
            Disabled = 16,
        }

        /// <summary>
        /// Gets or sets a value indicating whether the rule is enabled.
        /// </summary>
        public bool Enable
        {
            get
            {
                return ((bitfield & (255 << (int)BitFieldDefine.Disabled)) >> (int)BitFieldDefine.Disabled) == 0;
            }

            set
            {
                if (value)
                {
                    bitfield &= ~(255 << (int)BitFieldDefine.Disabled);
                }
                else
                {
                    bitfield = (bitfield & ~(255 << (int)BitFieldDefine.Disabled)) | (1 << (int)BitFieldDefine.Disabled);
                }
            }
        }

        /// <summary>
        /// Gets a unique identifier for the object.
        /// </summary>
        public int ObjectId { get; private set; }

        /// <summary>
        /// Gets a value indicating whether there are multiple literals.
        /// </summary>
        /// <returns>True if has multuple literals.</returns>
        public abstract bool IsAssertion { get; }

        /// <summary>
        /// Gets all packages literals.
        /// </summary>
        /// <returns>Returns all packages literals.</returns>
        public abstract int[] GetLiterals();

        /// <summary>
        /// Gets the reason to describing for generating this rule.
        /// </summary>
        /// <returns>Returns the reason constant.</returns>
        public Reason GetReason()
        {
            return (Reason)((bitfield & (255 << (int)BitFieldDefine.Reason)) >> (int)BitFieldDefine.Reason);
        }

        /// <summary>
        /// Gets the any data of the reason.
        /// </summary>
        /// <returns>Return the any data of the reason.</returns>
        public object GetReasonData()
        {
            return reasonData;
        }

        /// <summary>
        /// Gets the rule type.
        /// </summary>
        /// <returns>Returns the rule type.</returns>
        public RuleType GetRuleType()
        {
            return (RuleType)((bitfield & (255 << (int)BitFieldDefine.Type)) >> (int)BitFieldDefine.Type);
        }

        /// <summary>
        /// Gets the job this rule was created from.
        /// </summary>
        /// <returns>Return the job this rule was created from.</returns>
        public Job GetJob()
        {
            return job;
        }

        /// <summary>
        /// Gets the name of the require package name represented by rule.
        /// </summary>
        /// <returns>Returns the name of the require package name represented by rule.</returns>
        public string GetRequirePackageName()
        {
            if (GetReason() == Reason.JobInstall)
            {
                return GetReasonData().ToString();
            }

            if (GetReason() == Reason.PackageRequire && GetReasonData() is Link link)
            {
                return link.GetTarget();
            }

            return null;
        }

        /// <summary>
        /// Gets an pretty string to display.
        /// </summary>
        /// <param name="pool">The pool contains repositories that provide packages.</param>
        /// <param name="installedMap">Installed the package map.</param>
        /// <returns>Returns the pretty string.</returns>
        public string GetPrettyString(Pool pool, IDictionary<int, IPackage> installedMap = null)
        {
            var literals = GetLiterals();

            var ruleTextBuilder = new StringBuilder();
            foreach (var literal in literals)
            {
                ruleTextBuilder.Append(" | ");
                ruleTextBuilder.Append(pool.LiteralToPrettyString(literal, installedMap));
            }

            var ruleText = ruleTextBuilder.Length > 3 ?
                    ruleTextBuilder.ToString(3, ruleTextBuilder.Length - 3) :
                    string.Empty;

            switch (GetReason())
            {
                case Reason.InternalAllowUpdate:
                case Reason.PackageObsoletes:
                case Reason.InstalledPackageObsoletes:
                case Reason.PackageImplicitObsoletes:
                case Reason.PackageAlias:
                    return ruleText;
                case Reason.Learned:
                    return $"Conclusion: {ruleText}";
                case Reason.PackageSameName:
                    return $"Can only install one of: {FormatPackagesUnique(pool, literals)}.";
                case Reason.JobInstall:
                    return $"Install command rule ({ruleText})";
                case Reason.JobUninstall:
                    return $"Uninstall command rule ({ruleText})";
                case Reason.PackageConflict:
                    return FormatPackageConflict(pool, literals[0], literals[1]);
                case Reason.PackageRequire:
                    return FormatPackageRequire(pool, literals);
                default:
                    return $"({ruleText})";
            }
        }

        /// <summary>
        /// Format the package unique info (name and version).
        /// </summary>
        /// <param name="packages">An array of the packages.</param>
        /// <returns>Returns a string to describe packages.</returns>
        internal static string FormatPackagesUnique(IEnumerable<IPackage> packages)
        {
            var prepared = new Dictionary<string, PackageUnique>();

            foreach (var package in packages)
            {
                var packageName = package.GetName();
                if (!prepared.TryGetValue(packageName, out PackageUnique packageUnique))
                {
                    prepared[packageName] = packageUnique = new PackageUnique(packageName);
                }

                packageUnique[package.GetVersion()] = package.GetVersionPretty();
            }

            return string.Join<object>(", ", prepared.Values.ToArray());
        }

        /// <summary>
        /// Sets the rule type.
        /// </summary>
        /// <param name="type">The type of the rule.</param>
        internal void SetRuleType(RuleType type)
        {
            bitfield = (bitfield & ~(255 << (int)BitFieldDefine.Type)) | ((255 & (int)type) << (int)BitFieldDefine.Type);
        }

        /// <summary>
        /// Format the package unique info (name and version).
        /// </summary>
        /// <param name="pool">The pool contains repositories that provide packages.</param>
        /// <param name="literals">An array of the package literals.</param>
        /// <returns>Returns a string to describe packages.</returns>
        protected static string FormatPackagesUnique(Pool pool, IEnumerable<int> literals)
        {
            return FormatPackagesUnique(Arr.Map(literals, (literal) => pool.GetPackageByLiteral(literal)));
        }

        private static string FormatPackageConflict(Pool pool, int a, int b)
        {
            var package1 = pool.GetPackageByLiteral(a);
            return $"{package1.GetPrettyString()} conflicts with {FormatPackagesUnique(pool, new[] { b })}.";
        }

        private string FormatPackageRequire(Pool pool, int[] literals)
        {
            var sourceLiteral = Arr.Shift(ref literals);
            var sourcePackage = pool.GetPackageByLiteral(sourceLiteral);

            var requires = new LinkedList<IPackage>(
                Arr.Map(literals, (literal) => pool.GetPackageByLiteral(literal)));

            var text = new StringBuilder();
            if (reasonData is Link linkReasonData)
            {
                text.Append(linkReasonData.GetPrettyString(sourcePackage));
            }
            else
            {
                throw new UnexpectedException("Mistakes that should not occur.");
            }

            if (requires.Count > 0)
            {
                text.Append($" -> satisfiable by {FormatPackagesUnique(requires)}.");
                return text.ToString();
            }

            var targetName = linkReasonData.GetTarget();
            var providers = pool.WhatProvides(targetName, linkReasonData.GetConstraint(), true, true);
            if (providers.Length <= 0)
            {
                text.Append(" -> no matching package found.");
                return text.ToString();
            }

            text.Append(" -> satisfiable by ");
            text.Append(FormatPackagesUnique(providers));
            text.Append(" but these conflict with your requirements or minimum-stability.");
            return text.ToString();
        }

        private sealed class PackageUnique
        {
            private readonly IDictionary<string, string> versions;

            public PackageUnique(string name)
            {
                Name = name;
                versions = new Dictionary<string, string>();
            }

            public string Name { get; set; }

            public string this[string version]
            {
                set
                {
                    versions[version] = value;
                }
            }

            public override string ToString()
            {
                return $"{Name}[{string.Join(", ", versions.Values.ToArray())}]";
            }
        }
    }
}

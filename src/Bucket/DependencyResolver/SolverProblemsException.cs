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

using Bucket.Console;
using Bucket.Exception;
using Bucket.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bucket.DependencyResolver
{
    internal sealed class SolverProblemsException : RuntimeException
    {
        private readonly IEnumerable<Problem> problems;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolverProblemsException"/> class.
        /// </summary>
        public SolverProblemsException(IEnumerable<Problem> problems, IDictionary<int, IPackage> installedMap)
            : base(CreateMessage(problems, installedMap))
        {
            this.problems = problems;
        }

        /// <inheritdoc />
        public override int ExitCode => ExitCodes.DependencySolvingException;

        public Problem[] GetProblems()
        {
            return problems.ToArray();
        }

        private static string CreateMessage(IEnumerable<Problem> problems, IDictionary<int, IPackage> installedMap)
        {
            var message = new StringBuilder();
            message.Append(Environment.NewLine);

            var i = 0;
            foreach (var problem in problems)
            {
                message.Append("  Problem ");
                message.Append(++i);
                message.Append(problem.GetPrettyString(installedMap));
                message.Append(Environment.NewLine);
            }

            var text = message.ToString();
            if (text.Contains("could not be found") || text.Contains("no matching package found"))
            {
                message.Append(Environment.NewLine);
                message.Append("Potential causes:");
                message.Append(Environment.NewLine);
                message.Append(" - A typo in the package name");
                message.Append(Environment.NewLine);
                message.Append(" - The package is not available in a stable-enough version according to your minimum-stability setting");
                message.Append(Environment.NewLine);
                message.Append(" - It's a private package and you forgot to add a custom repository to find it");
                message.Append(Environment.NewLine);
            }

            return text.Length == message.Length ? text : message.ToString();
        }
    }
}

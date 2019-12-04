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

#pragma warning disable SA1600

using Bucket.Semver.Constraint;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// Indicates that the request is for content to be resolved.
    /// </summary>
    public sealed class Request
    {
        private readonly LinkedList<Job> jobs;

        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        public Request()
        {
            jobs = new LinkedList<Job>();
        }

        public void Install(string packageName, IConstraint constraint = null)
        {
            AddJob(JobCommand.Install, packageName, constraint);
        }

        public void Update(string packageName, IConstraint constraint = null)
        {
            AddJob(JobCommand.Update, packageName, constraint);
        }

        public void Uninstall(string packageName, IConstraint constraint = null)
        {
            AddJob(JobCommand.Uninstall, packageName, constraint);
        }

        /// <summary>
        /// Mark an existing package as being installed and having to remain installed.
        /// These jobs will not be tempered with by the solver.
        /// </summary>
        /// <param name="packageName">The package name.</param>
        /// <param name="constraint">The package version constraint.</param>
        public void Fix(string packageName, IConstraint constraint = null)
        {
            AddJob(JobCommand.Install, packageName, constraint, true);
        }

        public void UpdateAll()
        {
            jobs.AddLast(new Job()
            {
                Command = JobCommand.UpdateAll,
            });
        }

        public Job[] GetJobs()
        {
            return jobs.ToArray();
        }

        private void AddJob(JobCommand command, string packageName, IConstraint constraint = null, bool @fixed = false)
        {
            jobs.AddLast(new Job
            {
                Command = command,
                PackageName = packageName.ToLower(),
                Constraint = constraint,
                Fixed = @fixed,
            });
        }
    }
}

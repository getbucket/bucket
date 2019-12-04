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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Bucket.Tests.Support
{
    /// <summary>
    /// Indicates a test that requires online support.
    /// </summary>
    public class TestMethodOnlineAttribute : TestMethodAttribute
    {
        private readonly string ignoreMessage;
        private readonly string environmentVariable;

        public TestMethodOnlineAttribute(
            string ignoreMessage = "Tested only when the environment variable: BUCKET_TEST_ONLINE is set.",
            string environmentVariable = "BUCKET_TEST_ONLINE")
        {
            this.ignoreMessage = ignoreMessage;
            this.environmentVariable = environmentVariable;
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariable);

            if (string.IsNullOrEmpty(value) || value.ToLower() == "false" || value == "0")
            {
                var result = new TestResult()
                {
                    Outcome = UnitTestOutcome.Inconclusive,
                    TestFailureException = new System.Exception(ignoreMessage),
                };

                return new[] { result };
            }

            return base.Execute(testMethod);
        }
    }
}

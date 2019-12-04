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

using GameBox.Console.Process;
using Moq;
using Moq.Language.Flow;
using System;

namespace Bucket.Tests.Support.MockExtension
{
    public static class MockIProcessExecutor
    {
        private delegate int Execute(string command, out string[] stdout, out string[] stderr, string cwd);

        public static IReturnsResult<IProcessExecutor> Setup(this Mock<IProcessExecutor> process, string expectedCommand, string actualOut = null, string actualError = null, string expectedCwd = null, Func<int> returnValue = null)
        {
            return process.Setup((o) =>
                o.Execute(
                    It.Is<string>(actualCommand => expectedCommand == null || actualCommand == expectedCommand),
                    out It.Ref<string[]>.IsAny,
                    out It.Ref<string[]>.IsAny,
                    It.Is<string>(actualCwd => expectedCwd == null || actualCwd == expectedCwd)))
                .Returns(new Execute((string command, out string[] stdout, out string[] stderr, string cwd) =>
               {
                   stdout = new[] { actualOut };
                   stderr = new[] { actualError };
                   return returnValue?.Invoke() ?? 0;
               }));
        }
    }
}

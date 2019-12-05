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
    public static class ExtensionStringAssert
    {
        public static void NotContains(this StringAssert assert, string value, string substring, string message = null, params object[] parameters)
        {
            if (value == substring || value.Contains(substring, StringComparison.Ordinal))
            {
                Assert.Fail(message, parameters);
            }
        }
    }
}

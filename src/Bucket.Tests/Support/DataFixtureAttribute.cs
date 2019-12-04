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

using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Bucket.Tests.Support
{
    /// <summary>
    /// Import data test files from Fixture folder.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DataFixtureAttribute : Attribute, ITestDataSource
    {
        private readonly string file;
        private readonly object[] moreData;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFixtureAttribute"/> class.
        /// </summary>
        /// <param name="file">The file name. path will automatically use the path corresponding to the current test namespace in the Fixture directory.</param>
        /// <param name="moreData">An array of the other data.</param>
        public DataFixtureAttribute(string file, params object[] moreData)
        {
            this.file = file;
            this.moreData = moreData;
        }

        /// <inheritdoc />
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            var classNamespace = methodInfo.DeclaringType.Namespace;
            classNamespace = Regex.Replace(classNamespace, @"^Bucket\.Tests\.", string.Empty);
            classNamespace = Regex.Replace(classNamespace, @"^Bucket\.", string.Empty);
            classNamespace = classNamespace.Replace(".", "/", StringComparison.Ordinal);

            var location = Helper.Fixtrue($"{classNamespace}/{file}");

            if (!File.Exists(location))
            {
                throw new FileNotFoundException($"Fixture file \"{location}\" is not found.");
            }

            var parameters = methodInfo.GetParameters();
            if (parameters.Length <= 0)
            {
                throw new MissingMethodException("Method must have one parameter to receive file data.");
            }

            var parameterType = parameters[0].ParameterType;
            var content = File.ReadAllText(location);

            if (parameterType == typeof(JObject))
            {
                return new object[][]
                {
                    Arr.Merge(
                        new object[] { JObject.Parse(content) },
                        moreData),
                };
            }

            if (parameterType == typeof(Stream) || parameterType == typeof(FileStream))
            {
                return new object[][] { Arr.Merge(new object[] { File.OpenRead(file) }, moreData) };
            }

            if (parameterType.IsDefined(typeof(JsonObjectAttribute), false))
            {
                return new object[][]
                {
                    Arr.Merge(
                        new object[] { JsonConvert.DeserializeObject(content, parameterType) },
                        moreData),
                };
            }

            return new object[][] { Arr.Merge(new object[] { content }, moreData) };
        }

        /// <inheritdoc />
        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return $"{methodInfo.Name}({string.Join(", ", data)})";
        }
    }
}

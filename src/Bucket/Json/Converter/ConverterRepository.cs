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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Bucket.Json.Converter
{
    /// <summary>
    /// Repository conversion builder.
    /// </summary>
    internal sealed class ConverterRepository : ConverterBucketCreation<ConfigRepository>
    {
        /// <inheritdoc />
        public override ConfigRepository Create(Type objectType, JObject data)
        {
            if (!data.ContainsKey("type") || data["type"].Type != JTokenType.String)
            {
                throw new JsonSerializationException("Field: type is invalid, field is required, and type is string.");
            }

            var repositoryType = data["type"].Value<string>();
            var configurationType = Config.GetRepositoryConfiguration(repositoryType);
            return (ConfigRepository)Activator.CreateInstance(configurationType);
        }
    }
}

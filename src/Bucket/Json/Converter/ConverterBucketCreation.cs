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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.Json.Converter
{
    /// <summary>
    /// Represents the Bucket custom type creator.
    /// </summary>
    /// <typeparam name="T">The type of custom defiend.</typeparam>
    internal abstract class ConverterBucketCreation<T> : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            object CreateObject()
            {
                var data = JObject.Load(reader);
                var value = Create(objectType, data);
                if (value == null)
                {
                    throw new JsonSerializationException("No object created.");
                }

                using (var jObjectReader = CopyReaderForObject(reader, data))
                {
                    serializer.Populate(jObjectReader, value);
                }

                return value;
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                var collection = new LinkedList<T>();
                reader.Read();
                while (reader.TokenType != JsonToken.EndArray)
                {
                    collection.AddLast((T)CreateObject());
                    reader.Read();
                }

                return collection.ToArray();
            }

            return CreateObject();
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException($"{nameof(ConverterBucketCreation<T>)} should only be used while deserializing.");
        }

        /// <summary>
        /// Creates an object which will then be populated by the serializer.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="data">The data of json.</param>
        /// <returns>The created object.</returns>
        public abstract T Create(Type objectType, JObject data);

        /// <summary>
        /// Creates a new reader for the specified jObject by copying the settings
        /// from an existing reader.
        /// </summary>
        /// <param name="reader">The reader whose settings should be copied.</param>
        /// <param name="data">The jObject to create a new reader for.</param>
        /// <returns>The new disposable reader.</returns>
        private static JsonReader CopyReaderForObject(JsonReader reader, JObject data)
        {
            var jsonReader = data.CreateReader();
            jsonReader.Culture = reader.Culture;
            jsonReader.DateFormatString = reader.DateFormatString;
            jsonReader.DateParseHandling = reader.DateParseHandling;
            jsonReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
            jsonReader.FloatParseHandling = reader.FloatParseHandling;
            jsonReader.MaxDepth = reader.MaxDepth;
            jsonReader.SupportMultipleContent = reader.SupportMultipleContent;
            return jsonReader;
        }
    }
}

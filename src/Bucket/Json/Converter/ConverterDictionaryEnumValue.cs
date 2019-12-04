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
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Bucket.Json.Converter
{
    /// <summary>
    /// A converter that represents a dictionary type as an enumerated type.
    /// </summary>
    internal class ConverterDictionaryEnumValue<TValue> : JsonConverter
        where TValue : Enum
    {
        private readonly IDictionary<string, TValue> stringToEnum;
        private readonly IDictionary<TValue, string> enumToString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterDictionaryEnumValue{TValue}"/> class.
        /// </summary>
        public ConverterDictionaryEnumValue()
        {
            stringToEnum = new Dictionary<string, TValue>();
            enumToString = new Dictionary<TValue, string>();

            foreach (TValue value in Enum.GetValues(typeof(TValue)))
            {
                var member = value.GetAttribute<EnumMemberAttribute>();
                if (member == null)
                {
                    continue;
                }

                var enumString = member.Value.ToLower();
                stringToEnum[enumString] = value;
                enumToString[value] = enumString;
            }
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return IsA(objectType, typeof(IDictionary<,>)) &&
                    IsA(objectType.GetGenericArguments()[1], typeof(TValue));
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var keyType = objectType.GetGenericArguments()[0];
            var valueType = objectType.GetGenericArguments()[1];
            var intermediateDictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, typeof(string));
            var intermediateDictionary = (IDictionary)Activator.CreateInstance(intermediateDictionaryType);
            serializer.Populate(reader, intermediateDictionary);

            var ret = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
            foreach (DictionaryEntry pair in intermediateDictionary)
            {
                if (stringToEnum.TryGetValue(pair.Value.ToString().ToLower(), out TValue value))
                {
                    ret.Add(pair.Key, value);
                }
                else
                {
                    ret.Add(pair.Key, Enum.Parse(typeof(TValue), pair.Value.ToString(), true));
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var items = (IDictionary)value;
            writer.WriteStartObject();
            foreach (DictionaryEntry item in items)
            {
                writer.WritePropertyName(item.Key.ToString());

                if (enumToString.TryGetValue((TValue)item.Value, out string enumString))
                {
                    writer.WriteValue(enumString);
                }
                else
                {
                    writer.WriteValue(item.Value.ToString().ToLower());
                }
            }

            writer.WriteEndObject();
        }

        private static bool IsA(Type type, Type typeToBe)
        {
            if (!typeToBe.IsGenericTypeDefinition)
            {
                return typeToBe.IsAssignableFrom(type);
            }

            var toCheckTypes = new List<Type> { type };
            if (typeToBe.IsInterface)
            {
                toCheckTypes.AddRange(type.GetInterfaces());
            }

            var basedOn = type;
            while (basedOn.BaseType != null)
            {
                toCheckTypes.Add(basedOn.BaseType);
                basedOn = basedOn.BaseType;
            }

            return toCheckTypes.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeToBe);
        }
    }
}

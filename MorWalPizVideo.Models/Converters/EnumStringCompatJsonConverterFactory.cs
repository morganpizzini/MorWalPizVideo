using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Models.Converters
{
    public sealed class EnumStringCompatJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(EnumStringCompatJsonConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        private sealed class EnumStringCompatJsonConverter<TEnum> : JsonConverter<TEnum>
            where TEnum : struct, Enum
        {
            public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        return ParseString(reader.GetString());
                    case JsonTokenType.Number:
                        if (reader.TryGetInt64(out var int64Value))
                        {
                            return (TEnum)Enum.ToObject(typeof(TEnum), int64Value);
                        }

                        if (reader.TryGetUInt64(out var uint64Value))
                        {
                            return (TEnum)Enum.ToObject(typeof(TEnum), uint64Value);
                        }

                        throw new JsonException($"Unable to convert numeric value to {typeof(TEnum).Name}.");
                    default:
                        throw new JsonException($"Unexpected token type {reader.TokenType} when reading {typeof(TEnum).Name}.");
                }
            }

            public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            {
                var numericValue = Convert.ChangeType(value, Enum.GetUnderlyingType(typeof(TEnum)), CultureInfo.InvariantCulture);
                JsonSerializer.Serialize(writer, numericValue, options);
            }

            private static TEnum ParseString(string? rawValue)
            {
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    throw new JsonException($"Unable to convert an empty value to {typeof(TEnum).Name}.");
                }

                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                {
                    return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
                }

                if (Enum.TryParse<TEnum>(rawValue, true, out var parsedValue))
                {
                    return parsedValue;
                }

                foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    var enumMemberValue = field.GetCustomAttribute<EnumMemberAttribute>()?.Value;
                    if (string.Equals(enumMemberValue, rawValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return (TEnum)field.GetValue(null)!;
                    }
                }

                throw new JsonException($"The value '{rawValue}' is not valid for {typeof(TEnum).Name}.");
            }
        }
    }
}

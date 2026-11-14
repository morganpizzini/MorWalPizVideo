using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MorWalPizVideo.Models.Models.Serializers;

/// <summary>
/// Reads the legacy string representation as well as BSON numeric values without
/// requiring a rewrite of existing recipe documents.
/// </summary>
public sealed class TolerantDecimalStringSerializer : SerializerBase<string?>
{
    public override string? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        return reader.GetCurrentBsonType() switch
        {
            BsonType.Null => ReadNull(reader),
            BsonType.String => reader.ReadString(),
            BsonType.Decimal128 => reader.ReadDecimal128().ToString(),
            BsonType.Double => reader.ReadDouble().ToString("G29", CultureInfo.InvariantCulture),
            BsonType.Int32 => reader.ReadInt32().ToString(CultureInfo.InvariantCulture),
            BsonType.Int64 => reader.ReadInt64().ToString(CultureInfo.InvariantCulture),
            _ => throw new FormatException($"OAL must be a string or decimal BSON value, not {reader.GetCurrentBsonType()}.")
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string? value)
    {
        if (value is null)
            context.Writer.WriteNull();
        else
            context.Writer.WriteString(value);
    }

    private static string? ReadNull(IBsonReader reader)
    {
        reader.ReadNull();
        return null;
    }
}

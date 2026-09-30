using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.ShootingRange.Models;

public abstract record BaseEntity
{
    [BsonId]
    [BsonSerializer(typeof(ShootingRangeIdSerializer))]
    public string Id { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("creationDateTime")]
    public DateTime CreationDateTime { get; init; } = DateTime.Now;
}

public sealed class ShootingRangeIdSerializer : MongoDB.Bson.Serialization.Serializers.SerializerBase<string>
{
    public override string Deserialize(MongoDB.Bson.Serialization.BsonDeserializationContext context, MongoDB.Bson.Serialization.BsonDeserializationArgs args) => context.Reader.GetCurrentBsonType() == BsonType.ObjectId ? context.Reader.ReadObjectId().ToString() : context.Reader.ReadString();
    public override void Serialize(MongoDB.Bson.Serialization.BsonSerializationContext context, MongoDB.Bson.Serialization.BsonSerializationArgs args, string value)
    {
        if (ObjectId.TryParse(value, out var objectId)) context.Writer.WriteObjectId(objectId);
        else context.Writer.WriteString(value);
    }
}
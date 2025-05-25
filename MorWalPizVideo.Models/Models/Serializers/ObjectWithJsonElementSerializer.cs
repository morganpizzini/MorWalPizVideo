using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Text.Json;

namespace MorWalPizVideo.Server.Models.Serializers
{
    public class ObjectWithJsonElementSerializer : IBsonSerializer<object>
    {
        public Type ValueType => typeof(object);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is JsonElement jsonElement)
            {
                // Convert JsonElement to BsonDocument
                var json = jsonElement.GetRawText();
                var bsonDocument = BsonSerializer.Deserialize<BsonValue>(json);
                BsonValueSerializer.Instance.Serialize(context, bsonDocument);
            }
            else
            {
                BsonValueSerializer.Instance.Serialize(context, BsonValue.Create(value));
            }
        }

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonValue = BsonValueSerializer.Instance.Deserialize(context);
            return BsonTypeMapper.MapToDotNetValue(bsonValue);
        }
    }
}

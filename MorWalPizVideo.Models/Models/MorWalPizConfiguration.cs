using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System.Runtime.Serialization;
using System.Text.Json;

namespace MorWalPizVideo.Server.Models
{    [BsonIgnoreExtraElements]
    [DataContract]
    public record MorWalPizConfiguration(
        [property: DataMember][property: BsonElement("key")] string Key, // chiave della configurazione
        [property: DataMember][property: BsonElement("value")] object Value, // valore della configurazione
        [property: DataMember][property: BsonElement("type")] string Type, // tipo del valore (es: "bool", "string", "int", "datetime")
        [property: DataMember][property: BsonElement("description")] string Description // Nome leggibile della configurazione
    ) : BaseEntity
    {
    }
}

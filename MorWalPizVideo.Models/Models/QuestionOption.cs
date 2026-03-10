using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Represents an option for multiple choice or single choice questions
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record QuestionOption
    {
        [JsonConstructor]
        public QuestionOption(string optionId, string optionText, int order)
        {
            OptionId = optionId;
            OptionText = optionText;
            Order = order;
        }

        /// <summary>
        /// Unique identifier for this option
        /// </summary>
        [DataMember]
        [BsonElement("optionId")]
        public string OptionId { get; init; }

        /// <summary>
        /// Display text for this option
        /// </summary>
        [DataMember]
        [BsonElement("optionText")]
        public string OptionText { get; init; }

        /// <summary>
        /// Display order for this option
        /// </summary>
        [DataMember]
        [BsonElement("order")]
        public int Order { get; init; }
    }
}

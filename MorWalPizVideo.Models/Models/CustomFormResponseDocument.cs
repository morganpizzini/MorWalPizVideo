using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record CustomFormResponseDocument : BaseEntity
    {
        public CustomFormResponseDocument(string formId, string responseId, DateTime submittedAt, CustomFormAnswer[] answers)
        {
            FormId = formId;
            ResponseId = responseId;
            SubmittedAt = submittedAt;
            Answers = answers ?? Array.Empty<CustomFormAnswer>();
        }

        [DataMember]
        [BsonElement("formId")]
        public string FormId { get; init; }

        [DataMember]
        [BsonElement("responseId")]
        public string ResponseId { get; init; }

        [DataMember]
        [BsonElement("submittedAt")]
        public DateTime SubmittedAt { get; init; }

        [DataMember]
        [BsonElement("answers")]
        public CustomFormAnswer[] Answers { get; init; } = Array.Empty<CustomFormAnswer>();

        public CustomFormResponse ToResponse() => new(ResponseId, SubmittedAt, Answers);

        public static CustomFormResponseDocument FromResponse(string formId, CustomFormResponse response)
            => new(formId, response.ResponseId, response.SubmittedAt, response.Answers);
    }
}

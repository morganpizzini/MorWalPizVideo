using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Represents a custom form with questions and anonymous responses
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record CustomForm : BaseEntity
    {
        [JsonConstructor]
        public CustomForm(string title, string description, string url, CustomFormQuestion[] questions, bool active = true)
        {
            Title = title;
            Description = description;
            Url = url;
            Questions = questions ?? Array.Empty<CustomFormQuestion>();
            Responses = Array.Empty<CustomFormResponse>();
            Active = active;
        }

        /// <summary>
        /// Title of the form
        /// </summary>
        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }

        /// <summary>
        /// Description of the form
        /// </summary>
        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }

        /// <summary>
        /// URL-friendly path for accessing the form
        /// </summary>
        [DataMember]
        [BsonElement("url")]
        public string Url { get; init; }

        /// <summary>
        /// Whether the form is active and accepting responses
        /// </summary>
        [DataMember]
        [BsonElement("active")]
        public bool Active { get; init; } = true;

        /// <summary>
        /// Questions in this form
        /// </summary>
        [DataMember]
        [BsonElement("questions")]
        public CustomFormQuestion[] Questions { get; init; } = Array.Empty<CustomFormQuestion>();

        /// <summary>
        /// Anonymous responses to this form
        /// </summary>
        [DataMember]
        [BsonElement("responses")]
        public CustomFormResponse[] Responses { get; init; } = Array.Empty<CustomFormResponse>();

        /// <summary>
        /// Total number of responses received
        /// </summary>
        [BsonIgnore]
        public int ResponseCount => Responses.Length;

        /// <summary>
        /// Add a response to the form
        /// </summary>
        public CustomForm AddResponse(CustomFormResponse response)
        {
            var newResponses = Responses.Append(response).ToArray();
            return this with { Responses = newResponses };
        }

        /// <summary>
        /// Add a question to the form
        /// </summary>
        public CustomForm AddQuestion(CustomFormQuestion question)
        {
            var newQuestions = Questions.Append(question).ToArray();
            return this with { Questions = newQuestions };
        }

        /// <summary>
        /// Remove a question from the form
        /// </summary>
        public CustomForm RemoveQuestion(string questionId)
        {
            var newQuestions = Questions.Where(q => q.QuestionId != questionId).ToArray();
            return this with { Questions = newQuestions };
        }

        /// <summary>
        /// Update a question in the form
        /// </summary>
        public CustomForm UpdateQuestion(string questionId, CustomFormQuestion updatedQuestion)
        {
            var newQuestions = Questions.Select(q => 
                q.QuestionId == questionId ? updatedQuestion : q).ToArray();
            return this with { Questions = newQuestions };
        }

        /// <summary>
        /// Get a question by ID
        /// </summary>
        public CustomFormQuestion? GetQuestion(string questionId)
        {
            return Questions.FirstOrDefault(q => q.QuestionId == questionId);
        }

        /// <summary>
        /// Get responses for a specific question
        /// </summary>
        public CustomFormAnswer[] GetResponsesForQuestion(string questionId)
        {
            return Responses
                .SelectMany(r => r.Answers)
                .Where(a => a.QuestionId == questionId)
                .ToArray();
        }
    }
}

using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Represents an anonymous response to a custom form
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record CustomFormResponse
    {
        [JsonConstructor]
        public CustomFormResponse(string responseId, DateTime submittedAt, CustomFormAnswer[] answers)
        {
            ResponseId = responseId;
            SubmittedAt = submittedAt;
            Answers = answers ?? Array.Empty<CustomFormAnswer>();
        }

        /// <summary>
        /// Unique identifier for this response
        /// </summary>
        [DataMember]
        [BsonElement("responseId")]
        public string ResponseId { get; init; }

        /// <summary>
        /// When this response was submitted
        /// </summary>
        [DataMember]
        [BsonElement("submittedAt")]
        public DateTime SubmittedAt { get; init; }

        /// <summary>
        /// All answers in this response
        /// </summary>
        [DataMember]
        [BsonElement("answers")]
        public CustomFormAnswer[] Answers { get; init; } = Array.Empty<CustomFormAnswer>();
    }

    /// <summary>
    /// Base class for all form answers
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(OpenAnswer), typeof(MultipleChoiceAnswer), typeof(SingleChoiceAnswer))]
    public abstract record CustomFormAnswer
    {
        protected CustomFormAnswer(string questionId, AnswerType answerType)
        {
            QuestionId = questionId;
            AnswerType = answerType;
        }

        /// <summary>
        /// ID of the question this answer corresponds to
        /// </summary>
        [DataMember]
        [BsonElement("questionId")]
        public string QuestionId { get; init; }

        /// <summary>
        /// Type of answer (OpenText, MultipleChoice, SingleChoice)
        /// </summary>
        [DataMember]
        [BsonElement("answerType")]
        public AnswerType AnswerType { get; init; }
    }

    /// <summary>
    /// Answer for an open text question
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    [BsonDiscriminator("OpenAnswer")]
    public record OpenAnswer : CustomFormAnswer
    {
        [JsonConstructor]
        public OpenAnswer(string questionId, string textResponse)
            : base(questionId, AnswerType.OpenText)
        {
            TextResponse = textResponse;
        }

        /// <summary>
        /// The text response provided by the user
        /// </summary>
        [DataMember]
        [BsonElement("textResponse")]
        public string TextResponse { get; init; }
    }

    /// <summary>
    /// Answer for a multiple choice question (checkboxes)
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    [BsonDiscriminator("MultipleChoiceAnswer")]
    public record MultipleChoiceAnswer : CustomFormAnswer
    {
        [JsonConstructor]
        public MultipleChoiceAnswer(string questionId, string[] selectedOptionIds)
            : base(questionId, AnswerType.MultipleChoice)
        {
            SelectedOptionIds = selectedOptionIds ?? Array.Empty<string>();
        }

        /// <summary>
        /// IDs of the selected options
        /// </summary>
        [DataMember]
        [BsonElement("selectedOptionIds")]
        public string[] SelectedOptionIds { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Answer for a single choice question (radio buttons)
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    [BsonDiscriminator("SingleChoiceAnswer")]
    public record SingleChoiceAnswer : CustomFormAnswer
    {
        [JsonConstructor]
        public SingleChoiceAnswer(string questionId, string selectedOptionId)
            : base(questionId, AnswerType.SingleChoice)
        {
            SelectedOptionId = selectedOptionId;
        }

        /// <summary>
        /// ID of the selected option
        /// </summary>
        [DataMember]
        [BsonElement("selectedOptionId")]
        public string SelectedOptionId { get; init; }
    }
}

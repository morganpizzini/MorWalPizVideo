using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Base class for all custom form questions
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(OpenQuestion), typeof(MultipleChoiceQuestion), typeof(SingleChoiceQuestion))]
    public abstract record CustomFormQuestion
    {
        protected CustomFormQuestion(string questionId, string questionText, QuestionType questionType, bool isRequired, int order)
        {
            QuestionId = questionId;
            QuestionText = questionText;
            QuestionType = questionType;
            IsRequired = isRequired;
            Order = order;
        }

        /// <summary>
        /// Unique identifier for this question
        /// </summary>
        [DataMember]
        [BsonElement("questionId")]
        public string QuestionId { get; init; }

        /// <summary>
        /// The question text to display
        /// </summary>
        [DataMember]
        [BsonElement("questionText")]
        public string QuestionText { get; init; }

        /// <summary>
        /// Type of question (OpenText, MultipleChoice, SingleChoice)
        /// </summary>
        [DataMember]
        [BsonElement("questionType")]
        public QuestionType QuestionType { get; init; }

        /// <summary>
        /// Whether this question must be answered
        /// </summary>
        [DataMember]
        [BsonElement("isRequired")]
        public bool IsRequired { get; init; }

        /// <summary>
        /// Display order of the question in the form
        /// </summary>
        [DataMember]
        [BsonElement("order")]
        public int Order { get; init; }
    }

    /// <summary>
    /// Open text question allowing free-form text responses
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    [BsonDiscriminator("OpenQuestion")]
    public record OpenQuestion : CustomFormQuestion
    {
        [JsonConstructor]
        public OpenQuestion(string questionId, string questionText, bool isRequired, int order)
            : base(questionId, questionText, QuestionType.OpenText, isRequired, order)
        {
        }
    }

    /// <summary>
    /// Multiple choice question allowing multiple selections (checkboxes)
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    [BsonDiscriminator("MultipleChoiceQuestion")]
    public record MultipleChoiceQuestion : CustomFormQuestion
    {
        [JsonConstructor]
        public MultipleChoiceQuestion(string questionId, string questionText, bool isRequired, int order, QuestionOption[] options)
            : base(questionId, questionText, QuestionType.MultipleChoice, isRequired, order)
        {
            Options = options ?? Array.Empty<QuestionOption>();
        }

        /// <summary>
        /// Available options for this multiple choice question
        /// </summary>
        [DataMember]
        [BsonElement("options")]
        public QuestionOption[] Options { get; init; } = Array.Empty<QuestionOption>();
    }

    /// <summary>
    /// Single choice question allowing one selection only (radio buttons)
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    [BsonDiscriminator("SingleChoiceQuestion")]
    public record SingleChoiceQuestion : CustomFormQuestion
    {
        [JsonConstructor]
        public SingleChoiceQuestion(string questionId, string questionText, bool isRequired, int order, QuestionOption[] options)
            : base(questionId, questionText, QuestionType.SingleChoice, isRequired, order)
        {
            Options = options ?? Array.Empty<QuestionOption>();
        }

        /// <summary>
        /// Available options for this single choice question
        /// </summary>
        [DataMember]
        [BsonElement("options")]
        public QuestionOption[] Options { get; init; } = Array.Empty<QuestionOption>();
    }
}

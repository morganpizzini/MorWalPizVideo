using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Types of questions that can be asked in a custom form
    /// </summary>
    [DataContract]
    public enum QuestionType
    {
        /// <summary>
        /// Open text response
        /// </summary>
        [EnumMember]
        OpenText = 0,
        
        /// <summary>
        /// Multiple choice with checkboxes (multiple selections allowed)
        /// </summary>
        [EnumMember]
        MultipleChoice = 1,
        
        /// <summary>
        /// Single choice with radio buttons (one selection only)
        /// </summary>
        [EnumMember]
        SingleChoice = 2
    }

    /// <summary>
    /// Types of answers that can be provided for form questions
    /// </summary>
    [DataContract]
    public enum AnswerType
    {
        /// <summary>
        /// Text answer for open questions
        /// </summary>
        [EnumMember]
        OpenText = 0,
        
        /// <summary>
        /// Multiple option selections
        /// </summary>
        [EnumMember]
        MultipleChoice = 1,
        
        /// <summary>
        /// Single option selection
        /// </summary>
        [EnumMember]
        SingleChoice = 2
    }
}

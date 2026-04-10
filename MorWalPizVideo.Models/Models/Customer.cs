using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Customer : BaseEntity
    {
        [JsonConstructor]
        public Customer(
            string email,
            string? name,
            bool newsletterAccepted,
            bool termsAccepted,
            DateTime? termsAcceptedAt)
        {
            Email = email;
            Name = name;
            NewsletterAccepted = newsletterAccepted;
            TermsAccepted = termsAccepted;
            TermsAcceptedAt = termsAcceptedAt;
            LastLoginAt = DateTime.UtcNow;
        }

        [DataMember]
        [BsonElement("email")]
        public string Email { get; init; }

        [DataMember]
        [BsonElement("name")]
        public string? Name { get; init; }

        [DataMember]
        [BsonElement("lastLoginAt")]
        public DateTime? LastLoginAt { get; init; }

        [DataMember]
        [BsonElement("newsletterAccepted")]
        public bool NewsletterAccepted { get; init; }

        [DataMember]
        [BsonElement("termsAccepted")]
        public bool TermsAccepted { get; init; }

        [DataMember]
        [BsonElement("termsAcceptedAt")]
        public DateTime? TermsAcceptedAt { get; init; }
    }

    public record EmailLoginRequest(
        string Email,
        bool TermsAccepted,
        bool NewsletterAccepted,
        string RecaptchaToken
    );

    public record EmailVerificationRequest(
        string Email,
        string VerificationCode
    );

    public record LoginResponse(
        string CustomerId,
        string Email,
        string SessionToken
    );
}
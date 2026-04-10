using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Represents a topic of interest for discovering news and generating content
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record InsightTopic : BaseEntity
    {
        [JsonConstructor]
        public InsightTopic(string title, string description, string[] seedArguments, string[] preferredSources)
        {
            Title = title;
            Description = description;
            SeedArguments = seedArguments ?? Array.Empty<string>();
            PreferredSources = preferredSources ?? Array.Empty<string>();
        }

        /// <summary>
        /// Title of the topic
        /// </summary>
        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }

        /// <summary>
        /// Description of what this topic covers
        /// </summary>
        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }

        /// <summary>
        /// Seed arguments/keywords that define this topic
        /// </summary>
        [DataMember]
        [BsonElement("seedArguments")]
        public string[] SeedArguments { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Preferred news sources or scrapers to use for this topic
        /// </summary>
        [DataMember]
        [BsonElement("preferredSources")]
        public string[] PreferredSources { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Add a seed argument to the topic
        /// </summary>
        public InsightTopic AddSeedArgument(string argument)
        {
            var newArguments = SeedArguments.Append(argument).ToArray();
            return this with { SeedArguments = newArguments };
        }

        /// <summary>
        /// Remove a seed argument from the topic
        /// </summary>
        public InsightTopic RemoveSeedArgument(string argument)
        {
            var newArguments = SeedArguments.Where(a => a != argument).ToArray();
            return this with { SeedArguments = newArguments };
        }

        /// <summary>
        /// Add a preferred source to the topic
        /// </summary>
        public InsightTopic AddPreferredSource(string source)
        {
            var newSources = PreferredSources.Append(source).ToArray();
            return this with { PreferredSources = newSources };
        }

        /// <summary>
        /// Remove a preferred source from the topic
        /// </summary>
        public InsightTopic RemovePreferredSource(string source)
        {
            var newSources = PreferredSources.Where(s => s != source).ToArray();
            return this with { PreferredSources = newSources };
        }
    }
}
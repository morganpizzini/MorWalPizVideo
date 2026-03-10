using System.Text.Json;
using System.Text.Json.Serialization;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Converters
{
    /// <summary>
    /// Custom JSON converter for polymorphic CustomFormQuestion deserialization
    /// Handles the _t discriminator field to determine the concrete type
    /// </summary>
    public class CustomFormQuestionJsonConverter : JsonConverter<CustomFormQuestion>
    {
        private const string DiscriminatorPropertyName = "_t";

        public override CustomFormQuestion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Clone the reader to peek at the JSON without consuming it
            var readerCopy = reader;
            
            if (readerCopy.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            // Read through the object to find the discriminator
            string? discriminator = null;
            while (readerCopy.Read())
            {
                if (readerCopy.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (readerCopy.TokenType == JsonTokenType.PropertyName)
                {
                    string? propertyName = readerCopy.GetString();
                    readerCopy.Read(); // Move to the value

                    if (propertyName == DiscriminatorPropertyName)
                    {
                        discriminator = readerCopy.GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(discriminator))
            {
                throw new JsonException($"Missing discriminator field '{DiscriminatorPropertyName}' in CustomFormQuestion JSON");
            }

            // Deserialize to the appropriate concrete type based on discriminator
            return discriminator switch
            {
                "OpenQuestion" => JsonSerializer.Deserialize<OpenQuestion>(ref reader, options),
                "MultipleChoiceQuestion" => JsonSerializer.Deserialize<MultipleChoiceQuestion>(ref reader, options),
                "SingleChoiceQuestion" => JsonSerializer.Deserialize<SingleChoiceQuestion>(ref reader, options),
                _ => throw new JsonException($"Unknown discriminator value '{discriminator}' for CustomFormQuestion")
            };
        }

        public override void Write(Utf8JsonWriter writer, CustomFormQuestion value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write discriminator field
            writer.WriteString(DiscriminatorPropertyName, value switch
            {
                OpenQuestion => "OpenQuestion",
                MultipleChoiceQuestion => "MultipleChoiceQuestion",
                SingleChoiceQuestion => "SingleChoiceQuestion",
                _ => throw new JsonException($"Unknown CustomFormQuestion type: {value.GetType().Name}")
            });

            // Write all properties
            writer.WriteString("questionId", value.QuestionId);
            writer.WriteString("questionText", value.QuestionText);
            writer.WriteNumber("questionType", (int)value.QuestionType);
            writer.WriteBoolean("isRequired", value.IsRequired);
            writer.WriteNumber("order", value.Order);

            // Write type-specific properties
            if (value is MultipleChoiceQuestion mcq)
            {
                writer.WritePropertyName("options");
                JsonSerializer.Serialize(writer, mcq.Options, options);
            }
            else if (value is SingleChoiceQuestion scq)
            {
                writer.WritePropertyName("options");
                JsonSerializer.Serialize(writer, scq.Options, options);
            }

            writer.WriteEndObject();
        }
    }
}

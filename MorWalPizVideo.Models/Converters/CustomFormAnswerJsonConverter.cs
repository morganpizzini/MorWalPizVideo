using System.Text.Json;
using System.Text.Json.Serialization;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Converters
{
    /// <summary>
    /// Custom JSON converter for polymorphic CustomFormAnswer deserialization
    /// Handles the _t discriminator field to determine the concrete type
    /// </summary>
    public class CustomFormAnswerJsonConverter : JsonConverter<CustomFormAnswer>
    {
        private const string DiscriminatorPropertyName = "_t";

        public override CustomFormAnswer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                throw new JsonException($"Missing discriminator field '{DiscriminatorPropertyName}' in CustomFormAnswer JSON");
            }

            // Deserialize to the appropriate concrete type based on discriminator
            return discriminator switch
            {
                "OpenAnswer" => JsonSerializer.Deserialize<OpenAnswer>(ref reader, options),
                "MultipleChoiceAnswer" => JsonSerializer.Deserialize<MultipleChoiceAnswer>(ref reader, options),
                "SingleChoiceAnswer" => JsonSerializer.Deserialize<SingleChoiceAnswer>(ref reader, options),
                _ => throw new JsonException($"Unknown discriminator value '{discriminator}' for CustomFormAnswer")
            };
        }

        public override void Write(Utf8JsonWriter writer, CustomFormAnswer value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write discriminator field
            writer.WriteString(DiscriminatorPropertyName, value switch
            {
                OpenAnswer => "OpenAnswer",
                MultipleChoiceAnswer => "MultipleChoiceAnswer",
                SingleChoiceAnswer => "SingleChoiceAnswer",
                _ => throw new JsonException($"Unknown CustomFormAnswer type: {value.GetType().Name}")
            });

            // Write common properties
            writer.WriteString("questionId", value.QuestionId);
            writer.WriteNumber("answerType", (int)value.AnswerType);

            // Write type-specific properties
            if (value is OpenAnswer oa)
            {
                writer.WriteString("textResponse", oa.TextResponse);
            }
            else if (value is MultipleChoiceAnswer mca)
            {
                writer.WritePropertyName("selectedOptionIds");
                JsonSerializer.Serialize(writer, mca.SelectedOptionIds, options);
            }
            else if (value is SingleChoiceAnswer sca)
            {
                writer.WriteString("selectedOptionId", sca.SelectedOptionId);
            }

            writer.WriteEndObject();
        }
    }
}

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
                        discriminator = ReadDiscriminator(ref readerCopy);
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
                "BooleanAnswer" => JsonSerializer.Deserialize<BooleanAnswer>(ref reader, options),
                "EmailAnswer" => JsonSerializer.Deserialize<EmailAnswer>(ref reader, options),
                _ => throw new JsonException($"Unknown discriminator value '{discriminator}' for CustomFormAnswer")
            };
        }

        private static string? ReadDiscriminator(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                string? discriminator = null;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException($"Discriminator array '{DiscriminatorPropertyName}' must contain only strings");
                    }

                    // MongoDB writes the inheritance hierarchy from base to concrete type.
                    discriminator = reader.GetString();
                }

                return discriminator;
            }

            throw new JsonException($"Discriminator field '{DiscriminatorPropertyName}' must be a string or an array of strings");
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
                BooleanAnswer => "BooleanAnswer",
                EmailAnswer => "EmailAnswer",
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
            else if (value is BooleanAnswer ba)
            {
                writer.WriteBoolean("value", ba.Value);
            }
            else if (value is EmailAnswer ea)
            {
                writer.WriteString("email", ea.Email);
            }

            writer.WriteEndObject();
        }
    }
}

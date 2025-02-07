using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MorWalPizVideo.Domain
{
    public interface ITranslatorService
    {
        Task<string> TranslateTextWithHashtags(string text, string from, string to);
    }
    public class TranslatorServiceMock : ITranslatorService
    {
        public Task<string> TranslateTextWithHashtags(string text, string from, string to)
        {
            return Task.FromResult(text);
        }
    }
    public class TranslatorService : ITranslatorService
    {
        private readonly string _subscriptionKey;
        private readonly string _endpoint;
        private readonly string _region;

        public TranslatorService(string subscriptionKey, string endpoint, string region)
        {
            _subscriptionKey = subscriptionKey;
            _endpoint = endpoint;
            _region = region;
        }


        // Metodo che traduce un testo mantenendo gli hashtag
        public async Task<string> TranslateTextWithHashtags(string text, string from, string to)
        {
            var placeholders = new Dictionary<string, string>();

            // Trova gli hashtag e sostituiscili con segnaposti
            string processedText = Regex.Replace(text, @"#(\w+)", match =>
            {
                string hashtag = match.Value;  // Esempio: "#BuonGiorno"
                string wordWithoutHash = match.Groups[1].Value;  // "BuonGiorno"
                string placeholder = $"__HASHTAG{placeholders.Count}__";

                placeholders[placeholder] = wordWithoutHash; // Salvo l'hashtag senza "#"
                return placeholder;
            });

            // Traduci il testo senza hashtag
            string translatedText = await TranslateTextAsync(processedText, from, to);

            var placeholderHandle = string.Join(" | ", placeholders.Select(x => x.Value).ToList());

            string[] array = (await TranslateTextAsync(placeholderHandle, from, to))
                                .Split(" | ");

            for (int i = 0; i < array.Length; i++)
            {
                string placeholder = $"__HASHTAG{i}__";
                translatedText = translatedText.Replace(placeholder, $"#{array[i]}");
            }

            return translatedText;
        }

        public async Task<string> TranslateTextAsync(string text, string fromLanguage, string toLanguage)
        {
            //creare category
            // https://learn.microsoft.com/en-us/azure/ai-services/translator/custom-translator/quickstart

            // Costruisci l'URL
            string route = $"/translate?api-version=3.0&from={fromLanguage}&to={toLanguage}&formality=informal";
            string url = _endpoint + route;

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", _region);

            // Costruisci il payload
            var body = new object[] { new { Text = text } };
            string requestBody = JsonSerializer.Serialize(body);

            using HttpResponseMessage response = await client.PostAsync(url,
                new StringContent(requestBody, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode(); // Verifica che la risposta sia OK

            string responseBody = await response.Content.ReadAsStringAsync();
            // Deserializza il risultato
            var result = JsonSerializer.Deserialize<TranslationResponse[]>(responseBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result[0].Translations[0].Text; // Ritorna il testo tradotto
        }
    }

    // Modello per deserializzare la risposta dell'API
    public class TranslationResponse
    {
        public Translation[] Translations { get; set; }
    }

    public class Translation
    {
        public string Text { get; set; }
        public string To { get; set; }
    }
}

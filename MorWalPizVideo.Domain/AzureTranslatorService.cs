using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MorWalPizVideo.Domain
{
    //public class AzureOpenAITranslatorService : ITranslatorService
    //{
    //    private readonly string _apiKey;
    //    private readonly string _endpoint;

    //    public AzureOpenAITranslatorService(string apiKey, string endpoint)
    //    {
    //        _apiKey = apiKey;
    //        _endpoint = endpoint;
    //    }

    //    public async Task<string> TranslateTextWithHashtags(string text)
    //    {
    //        // Construct the URL
    //        string url = $"{_endpoint}/openai/deployments/gpt-4o/completions?api-version=2025-01-01-preview&api-key={_apiKey}";

    //        using HttpClient client = new HttpClient();
            
    //        // Construct the payload
    //        var body = new
    //        {
    //            messages = new[] {
    //              new { role= "system", content = "Sei copywriter armi/IPSC esperto in traduzioni. Crea un titolo YouTube partendo da parole chiave in italiano. inserisci hashtag. output: 'IT: <it> | EN: <en>'. Non tradurre 'No shoot, A zone, Double Alpha, Charlie'. Dizionario: Hit factor > Fattore, match > gara, Failure to engage > Mancato ingaggio"},
    //              new { role = "user", content = text}
    //            },
    //            max_completion_tokens = 1000
    //        };
    //        string requestBody = JsonSerializer.Serialize(body);

    //        using HttpResponseMessage response = await client.PostAsync(url,
    //            new StringContent(requestBody, Encoding.UTF8, "application/json"));
    //        try
    //        {
    //            response.EnsureSuccessStatusCode(); // Ensure the response is OK
    //        }catch
    //        {
    //            return string.Empty;
    //        }

    //        string responseBody = await response.Content.ReadAsStringAsync();
    //        // Deserialize the result
    //        var result = JsonSerializer.Deserialize<OpenAIResponse>(responseBody,
    //            new JsonSerializerOptions
    //            {
    //                PropertyNameCaseInsensitive = true
    //            });

    //        return result.Choices[0].Text.Trim(); // Return the translated text
    //    }
    //}

    //// Model to deserialize the OpenAI API response
    //public class OpenAIResponse
    //{
    //    public Choice[] Choices { get; set; }
    //}

    //public class Choice
    //{
    //    public string Text { get; set; }
    //}
}

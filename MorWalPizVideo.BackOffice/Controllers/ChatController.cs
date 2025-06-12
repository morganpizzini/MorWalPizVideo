using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MorWalPizVideo.BackOffice.DTOs;
using System.Text.Json;

namespace MorWalPizVideo.BackOffice.Controllers
{
  public class ChatController : ApplicationController
  {
    private readonly Kernel _kernel;

    public ChatController(Kernel kernel)
    {
      _kernel = kernel;
    }

    [HttpPost]
    public async Task<IActionResult> GetReviewDetails([FromBody] ReviewRequest reviewRequest)
    {
      var fileNames = string.Join("\n -", reviewRequest.Names.OrderBy(x => x));

      if (reviewRequest.Context == null)
      {
        reviewRequest.Context = string.Empty;
      }
      else
      {
        reviewRequest.Context = $"L'argomento più specifico sarà: {reviewRequest.Context.Trim()}.";
      }

      var prompt = @$"Sei un esperto di armi, del tiro dinamico, IPSC e IDPA.
                        {reviewRequest.Context}
                        Ti fornirò una lista, ogni elemento è una serie di parole chiavi. Il tuo compito è quello di
                        elaborare una frase di senso compiuto esaustiva del concetto espresso da quelle parole chiavi.
                        Ecco l'elenco:
                        {fileNames}
                        Un volta ottenuto il risultato, e conoscendo le meccaniche di engagement di Youtube,
                        il funzionamento del suo algoritmo e le regole SEO,elabora un titolo e una descrizione per ogni elemento seguendo anche le istruzioni seguenti. 
                    Il titolo deve:
                    – Riflettere chiaramente l'argomento del video
                    – Non superare i 100 caratteri.
                    – Includere parole chiave rilevanti per il pubblico di riferimento
                    – Incoraggiare il clic (essere coinvolgente ma non clickbait)
                    La descrizione deve:
                    – Spiegare chiaramente di cosa parla il video, la tipologia del video è di intrattenimento ed anche la descrizione deve tenere toni leggeri
                    - non utilizzare costrutti simili a 'questo video', 'questo video', 'nel video', che saranno dati come sottintesi
                    – Includere parole chiave rilevanti per il pubblico e l'algoritmo di ricerca, evidenziarle utilizzando hashtag
                    – Includere se possibile un breve riassunto dei punti principali trattati
                    - Se alcune parole chiave non vengono incluse nel contesto della descrizione, aggiungile alla fine del testo senza scrivere altro.
                    – Avere una lunghezza compresa tra 200 e 800 caratteri.
                    Traduci anche in Inglese.Non tradurre: No Shoot, A Zone, Double Alpha, Charlie, Double Charlie.
                    Dizionario per termini specifici: Hit factor > Fattore, match > gara, Failure to engage > Mancato ingaggio, Topolino > Mickey mouse, Consizione 1/2/3 > Condition 1/2/3,mano forte/debole > strong/weak hand
                    Utilizzando la traduzione inglese, crea le traduzioni anche per queste lingue: {string.Join(", ", reviewRequest.Languages)}.
                    Elabora le informazioni e dammi un risultato seguento il JSON schema fornito.";

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
      var executionSettings = new AzureOpenAIPromptExecutionSettings()
      {
        ResponseFormat = typeof(Review)
      };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

      var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

      var review = JsonSerializer.Deserialize<Review>(result.ToString());
      return Ok(review);
    }
  }
}

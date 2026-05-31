Quando scrivi codice, attieniti alle seguenti linee guida per garantire chiarezza e concisione.

## Generalità:

- Scrivi codice leggibile, con nomi di variabili, metodi e classi descrittivi.
- Evita logica superflua, commenta solo quando il codice non è autoesplicativo.
- Segui le convenzioni di stile del linguaggio e del framework utilizzato.

## C# – WebAPI e WPF:
- Per WebAPI, usa controller RESTful puliti, con metodi separati per responsabilità (SRP).
- Utilizza DTO per l’input/output, evitando di esporre entità direttamente.
- Per WPF, mantieni l’architettura MVVM, con ViewModel snelli e separazione netta da UI logic.
- Evita binding complessi e prediligi proprietà reattive e INotifyPropertyChanged.

## OutputCache & cache tag conventions:
- Tutti i tag di OutputCache (sia in `[OutputCache(Tags = ...)]` sia in chiamate a `EvictByTagAsync`) **devono essere in lowercase invariant**. Usa le costanti centralizzate in `MorWalPizVideo.Models/Constraints/CacheKeys.cs` e `ApiTagCacheKeys.cs`.
- Quando si riceve un tag dall'esterno (es. query string del `CacheController`), normalizzalo sempre con `ToLowerInvariant()` prima dell'eviction.
- Non costruire `HttpClient` con `new HttpClient(...)` né con `using var c = factory.CreateClient(...)`: gli `HttpClient` ottenuti da `IHttpClientFactory` non vanno disposti dal caller (rompe il pooling delle connessioni e causa `SocketException`).

## React con TypeScript:
- Scrivi componenti funzionali, ben separati e riutilizzabili.
- Usa tipi e interfacce TypeScript per ogni props/state.
- Evita la logica nel render, spostala in hook o funzioni helper.
- Mantieni la struttura a directory modulare, seguendo la logica feature-based se possibile.
- L’obiettivo è scrivere codice moderno, manutenibile e chiaro al primo colpo d’occhio. Evita overengineering: semplicità prima di tutto.

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan:
[specs/002-pepperbox-clone/plan.md](../specs/002-pepperbox-clone/plan.md)
<!-- SPECKIT END -->

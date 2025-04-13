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
## React con TypeScript:
- Scrivi componenti funzionali, ben separati e riutilizzabili.
- Usa tipi e interfacce TypeScript per ogni props/state.
- Evita la logica nel render, spostala in hook o funzioni helper.
- Mantieni la struttura a directory modulare, seguendo la logica feature-based se possibile.
- L’obiettivo è scrivere codice moderno, manutenibile e chiaro al primo colpo d’occhio. Evita overengineering: semplicità prima di tutto.
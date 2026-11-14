import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

export const supportedCultures = ["it-IT", "en-GB"] as const;
export type SupportedCulture = (typeof supportedCultures)[number];

const storageKey = "shooter-workshop.culture";
const italian: Record<string, string> = {
  "Workshop navigation": "Navigazione Workshop",
  "Dashboard": "Dashboard",
  "Firearms": "Armi",
  "Components": "Componenti",
  "Ammunition": "Munizioni",
  "Recipes": "Ricette",
  "Production": "Produzione",
  "Shooting sessions": "Sessioni di tiro",
  "History": "Cronologia",
  "Account": "Account",
  "Sign out": "Esci",
  "Restoring your account session…": "Ripristino della sessione dell'account…",
  "Redirecting to account login…": "Reindirizzamento all'accesso dell'account…",
  "Loading workshop data…": "Caricamento dei dati del Workshop…",
  "Could not load this workspace.": "Impossibile caricare questo spazio di lavoro.",
  "The request could not be completed.": "Impossibile completare la richiesta.",
  "Try again": "Riprova",
  "Conflict detected": "Conflitto rilevato",
  "Another operation changed this record. Reload before saving again.": "Un'altra operazione ha modificato questo record. Ricarica prima di salvare di nuovo.",
  "Reload": "Ricarica",
  "No data returned": "Nessun dato restituito",
  "The API returned an empty response.": "L'API ha restituito una risposta vuota.",
  "Saving…": "Salvataggio…",
  "Please review the form.": "Controlla il modulo.",
  "Language": "Lingua",
  "Italian": "Italiano",
  "English": "Inglese",
  "Private workspace": "Spazio di lavoro privato",
  "Sign in to Workshop": "Accedi al Workshop",
  "Use your existing MorWalPiz account. Workshop requests stay on the account cookie transport.": "Usa il tuo account MorWalPiz esistente. Le richieste Workshop restano sul trasporto tramite cookie dell'account.",
  "Username or email": "Nome utente o email",
  "Password": "Password",
  "Sign in": "Accedi",
  "Sign-in was not accepted.": "Accesso non accettato.",
  "No activity yet": "Nessuna attività",
  "Your first operation will appear here.": "La tua prima operazione apparirà qui.",
  "rounds": "colpi",
  "active": "attiva",
  "archived": "archiviata",
  "Inventory": "Magazzino",
  "Installed": "Installato",
  "Maintenance": "Manutenzione",
  "Retired": "Dismesso",
  "Current": "Attuale",
  "Pending": "In attesa",
  "Inconsistent": "Incoerente",
  "Counter": "Contatore",
  "Interval": "Intervallo",
  "Manual": "Manuale",
  "Inspection": "Ispezione",
  "Cleaning": "Pulizia",
  "ComponentMaintenance": "Manutenzione componente",
  "Replacement": "Sostituzione",
  "General": "Generale",
  "No manufacturer": "Nessun produttore",
  "not set": "non impostato",
  "manual": "manuale",
  "Report this problem": "Segnala questo problema",
  "Workshop dashboard": "Dashboard Workshop",
  "Account-owned equipment, stock and operation history.": "Attrezzatura, scorte e cronologia delle operazioni dell'account.",
  "Operational overview": "Panoramica operativa",
  "Every operation remains attributable.": "Ogni operazione resta tracciabile.",
  "Firearms, physical components, batches and shooting facts are kept independent by the API.": "Armi, componenti fisici, lotti e dati di tiro restano indipendenti nell'API.",
  "Record a shooting session": "Registra una sessione di tiro",
  "Active firearms": "Armi attive",
  "Firearm rounds": "Colpi delle armi",
  "Physical components": "Componenti fisici",
  "Available stock": "Scorte disponibili",
  "Quick actions": "Azioni rapide",
  "Register firearm": "Registra arma",
  "Keep equipment counters independent.": "Mantieni indipendenti i contatori dell'attrezzatura.",
  "Create recipe": "Crea ricetta",
  "Capture the immutable recipe snapshot.": "Registra l'istantanea immutabile della ricetta.",
  "Record production": "Registra produzione",
  "Create stock from a recipe.": "Crea scorte da una ricetta.",
  "Check integrity": "Controlla integrità",
  "Review reconciliation status.": "Controlla lo stato della riconciliazione.",
  "Recent activity": "Attività recente",
  "View history": "Vedi cronologia",
  "My firearms": "Le mie armi",
  "{count} account-owned records": "{count} record dell'account",
  "No firearms registered": "Nessuna arma registrata",
  "Add a firearm to connect sessions, components and allocations.": "Aggiungi un'arma per collegare sessioni, componenti e assegnazioni.",
  "Save firearm": "Salva arma",
  "manufacturer": "produttore",
  "model": "modello",
  "caliber": "calibro",
  "nickname": "nome",
  "category": "categoria",
  "Notes": "Note",
  "Firearm not found": "Arma non trovata",
  "The record may be archived or belong to another account.": "Il record potrebbe essere archiviato o appartenere a un altro account.",
  "Total rounds": "Colpi totali",
  "Revision": "Revisione",
  "Account record": "Record account",
  "Manage firearm": "Gestisci arma",
  "Cancel": "Annulla",
  "Edit details": "Modifica dettagli",
  "Archive firearm": "Archivia arma",
  "Related workflows": "Flussi correlati",
  "Record a session against this firearm or correct a previously recorded operation.": "Registra una sessione per quest'arma o correggi un'operazione precedente.",
  "Record session": "Registra sessione",
  "Edit firearm": "Modifica arma",
  "Save changes": "Salva modifiche",
  "Catalogue definitions and independent physical inventory.": "Definizioni di catalogo e inventario fisico indipendente.",
  "Catalogue": "Catalogo",
  "Definition": "Definizione",
  "New component definition": "Nuova definizione componente",
  "Save definition": "Salva definizione",
  "Save": "Salva",
  "Edit": "Modifica",
  "Archive": "Archivia",
  "Delete": "Elimina",
  "Name": "Nome",
  "interval {value}": "intervallo {value}",
  "Catalogue is empty": "Il catalogo è vuoto",
  "My inventory": "Il mio inventario",
  "Add physical component": "Aggiungi componente fisico",
  "Add component": "Aggiungi componente",
  "Select definition": "Seleziona definizione",
  "Identification note": "Nota identificativa",
  "Manufacturer": "Produttore",
  "Model": "Modello",
  "Compatibility tags": "Tag di compatibilità",
  "Comma-separated tags": "Tag separati da virgole",
  "Tracking mode": "Modalità di tracciamento",
  "Maximum rounds": "Colpi massimi",
  "Firearm (optional)": "Arma (facoltativa)",
  "Leave empty to add a spare part": "Lascia vuoto per aggiungere un ricambio",
  "Edit physical component": "Modifica componente fisico",
  "No compatibility tags": "Nessun tag di compatibilità",
  "No description": "Nessuna descrizione",
  "Clear": "Cancella",
  "No components match this tag": "Nessun componente corrisponde a questo tag",
  "No identification note": "Nessuna nota identificativa",
  "Component projections and service intervals.": "Proiezioni dei componenti e intervalli di manutenzione.",
  "Record maintenance": "Registra manutenzione",
  "Save event": "Salva evento",
  "Component": "Componente",
  "Select component": "Seleziona componente",
  "Event type": "Tipo evento",
  "Component maintenance": "Manutenzione componente",
  "Date": "Data",
  "Round count": "Conteggio colpi",
  "Description": "Descrizione",
  "Produced batches and current on-hand stock.": "Lotti prodotti e scorte attualmente disponibili.",
  "Batch": "Lotto",
  "Caliber": "Calibro",
  "Produced": "Prodotti",
  "On hand": "Disponibili",
  "Ammunition recipes": "Ricette munizioni",
  "Recipe fields match the server snapshot contract.": "I campi della ricetta corrispondono al contratto snapshot del server.",
  "New recipe": "Nuova ricetta",
  "Powder not set": "Polvere non impostata",
  "Save recipe": "Salva ricetta",
  "Powder": "Polvere",
  "Powder weight": "Peso polvere",
  "Production sessions": "Sessioni di produzione",
  "POST /api/v1/workshop/ammunition/production creates a batch.": "POST /api/v1/workshop/ammunition/production crea un lotto.",
  "Create batch": "Crea lotto",
  "Recipe": "Ricetta",
  "Select recipe": "Seleziona ricetta",
  "Quantity": "Quantità",
  "Record new sessions or correct an operation without rewriting history.": "Registra nuove sessioni o correggi un'operazione senza riscrivere la cronologia.",
  "Record shooting": "Registra tiro",
  "Firearm": "Arma",
  "Select firearm": "Seleziona arma",
  "Untracked ammunition": "Munizioni non tracciate",
  "Consumption records JSON": "Record di consumo JSON",
  "Usage segments JSON": "Segmenti di utilizzo JSON",
  "Correct shooting operation": "Correggi operazione di tiro",
  "Record correction": "Registra correzione",
  "Original operation ID": "ID operazione originale",
  "Correct firearm": "Arma corretta",
  "Correct total rounds": "Correggi colpi totali",
  "A correction appends a compensating operation; the original remains in history.": "Una correzione aggiunge un'operazione compensativa; l'originale resta nella cronologia.",
  "Ammunition allocation": "Assegnazione munizioni",
  "Allocate, release, or move stock between firearms.": "Assegna, rilascia o sposta scorte tra armi.",
  "Allocate stock": "Assegna scorte",
  "Allocate": "Assegna",
  "Select batch": "Seleziona lotto",
  "{count} on hand": "{count} disponibili",
  "Release allocation": "Rilascia assegnazione",
  "Release stock": "Rilascia scorte",
  "Allocation ID": "ID assegnazione",
  "Current revision": "Revisione attuale",
  "Reallocate stock": "Riassegna scorte",
  "Move stock": "Sposta scorte",
  "Source allocation ID": "ID assegnazione origine",
  "Destination firearm": "Arma destinazione",
  "Account-filtered chronological activity from the controller.": "Attività cronologica filtrata per account dal controller.",
  "Integrity and reconciliation": "Integrità e riconciliazione",
  "Projection state is returned by the canonical reconciliation endpoint.": "Lo stato della proiezione è restituito dall'endpoint canonico di riconciliazione.",
  "{count} mismatches detected.": "Rilevate {count} incoerenze.",
  "No mismatches reported.": "Nessuna incoerenza rilevata.",
  "Last run: {date}": "Ultima esecuzione: {date}",
  "Rebuild projections": "Ricostruisci proiezioni",
  "Shared MorWalPiz account identity": "Identità account MorWalPiz condivisa",
  "Username": "Nome utente",
  "Email": "Email",
  "Display name": "Nome visualizzato",
  "Operation {id}": "Operazione {id}",
  "Report an issue": "Segnala un problema",
  "Close": "Chiudi",
  "Describe what happened. The current page and basic browser context are attached automatically. Do not include passwords or private data.": "Descrivi cosa è successo. La pagina corrente e il contesto di base del browser vengono allegati automaticamente. Non includere password o dati privati.",
  "Page:": "Pagina:",
  "Sending…": "Invio…",
  "Send feedback": "Invia segnalazione",
  "Unable to start a feedback report. Please try again.": "Impossibile avviare la segnalazione. Riprova.",
  "Your report is too large. Shorten the description and try again.": "La segnalazione è troppo lunga. Accorcia la descrizione e riprova.",
  "Feedback sent. Thank you.": "Segnalazione inviata. Grazie.",
  "Feedback sent anonymously. Thank you.": "Segnalazione anonima inviata. Grazie.",
  "You appear to be offline. Reconnect and try again.": "Sembra che tu sia offline. Riconnettiti e riprova.",
  "Too many reports. Please wait a moment and try again.": "Troppe segnalazioni. Attendi un momento e riprova.",
  "Your session expired. Refresh the page and try again.": "La sessione è scaduta. Aggiorna la pagina e riprova.",
  "Unable to send feedback. Check your connection and try again.": "Impossibile inviare la segnalazione. Controlla la connessione e riprova.",
  "The dashboard response was empty.": "La risposta della dashboard era vuota.",
};

const messages: Record<string, string> = {
  "No physical components": "Nessun componente fisico",
  "No tracked components": "Nessun componente tracciato",
  "No recipes yet": "Nessuna ricetta",
  "No ammunition batches": "Nessun lotto di munizioni",
  "No history yet": "Nessuna cronologia",
  "Unavailable route": "Percorso non disponibile",
  "Use the Components workspace for catalogue and inventory.": "Usa lo spazio Componenti per catalogo e magazzino.",
};
Object.assign(italian, messages);

function isSupportedCulture(value: string | null): value is SupportedCulture {
  return value === "it-IT" || value === "en-GB";
}

function matchBrowserCulture(languages: readonly string[]): SupportedCulture | null {
  for (const language of languages) {
    const primary = language.toLowerCase().split("-")[0];
    if (primary === "it") return "it-IT";
    if (primary === "en") return "en-GB";
  }
  return null;
}

export function resolveInitialCulture(
  storage: Pick<Storage, "getItem"> | null = typeof window === "undefined" ? null : window.localStorage,
  languages: readonly string[] = typeof navigator === "undefined" ? [] : [...navigator.languages, navigator.language],
): SupportedCulture {
  try {
    const stored = storage?.getItem(storageKey) ?? null;
    if (isSupportedCulture(stored)) return stored;
  } catch {
    // Browser storage can be disabled; detection still provides a usable culture.
  }
  return matchBrowserCulture(languages) ?? "it-IT";
}

function persistCulture(culture: SupportedCulture) {
  try {
    window.localStorage.setItem(storageKey, culture);
  } catch {
    // The current selection remains active even when persistence is unavailable.
  }
}

function updateDocument(culture: SupportedCulture) {
  if (typeof document === "undefined") return;
  document.documentElement.lang = culture;
  document.title = culture === "it-IT" ? "Shooter Workshop" : "Shooter Workshop";
}

interface I18nContextValue {
  culture: SupportedCulture;
  setCulture: (culture: SupportedCulture) => void;
  t: (key: string, values?: Record<string, string | number>) => string;
}

const I18nContext = createContext<I18nContextValue | null>(null);

export function I18nProvider({ children }: { children: ReactNode }) {
  const [culture, setCultureState] = useState<SupportedCulture>(() => resolveInitialCulture());
  const setCulture = (next: SupportedCulture) => {
    setCultureState(next);
    persistCulture(next);
    updateDocument(next);
  };
  const value = useMemo<I18nContextValue>(() => ({
    culture,
    setCulture,
    t: (key, values) => {
      const message = culture === "it-IT" ? (italian[key] ?? key) : key;
      return values ? message.replace(/\{(\w+)\}/g, (_, name: string) => String(values[name] ?? `{${name}}`)) : message;
    },
  }), [culture]);
  useEffect(() => updateDocument(culture), [culture]);
  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  const context = useContext(I18nContext);
  if (!context) throw new Error("useI18n must be used within I18nProvider");
  return context;
}

export function getStorageKey() {
  return storageKey;
}

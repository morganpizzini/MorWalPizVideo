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

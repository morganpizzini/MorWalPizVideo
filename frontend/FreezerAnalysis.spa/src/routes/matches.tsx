import {
  useState,
  type FormEvent
} from "react";
import {
  Link,
  useLoaderData,
  useRevalidator
} from "react-router-dom";


import {
  freezerApi,
  type ImportPreview,
  type MatchSummary,
  type PdfPairPreview
} from "../freezerApi";


import {
  ErrorMessage,
  errorMessage,
  formatDate
} from "./shared";

export function MatchesPage() {
  const matches = useLoaderData() as MatchSummary[];
  const revalidator = useRevalidator();
  const [error, setError] = useState("");
  const [preview, setPreview] = useState<ImportPreview | null>(null);
  const [sourceFile, setSourceFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [pdfPreview, setPdfPreview] = useState<PdfPairPreview | null>(null);
  const [pdfError, setPdfError] = useState("");
  const [pdfBusy, setPdfBusy] = useState(false);
  const [classificationPdfName, setClassificationPdfName] = useState("");
  const [verificationPdfName, setVerificationPdfName] = useState("");

  async function upload(event: FormEvent<HTMLFormElement>, publish: boolean) {
    event.preventDefault();
    setError("");
    setBusy(true);
    const form = new FormData(event.currentTarget);
    try {
      if (publish) {
        if (!preview || !sourceFile)
          throw new Error("Analizza nuovamente il CSV prima di pubblicarlo.");
        const publishForm = new FormData();
        publishForm.set("file", sourceFile);
        publishForm.set("matchName", preview.matchName);
        publishForm.set("matchDate", preview.localMatchDate);
        const result = await freezerApi.publishScores(
          publishForm,
          preview.expectedRevision,
        );
        setPreview(result);
        setSourceFile(null);
        revalidator.revalidate();
      } else {
        const file = form.get("file");
        if (!(file instanceof File) || file.size === 0)
          throw new Error("Seleziona un file CSV valido.");
        const result = await freezerApi.previewScores(form);
        setSourceFile(file);
        setPreview(result);
      }
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function previewPdfPair(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPdfError("");
    setPdfPreview(null);
    setPdfBusy(true);
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    try {
      const classificationFile = (
        formElement.elements.namedItem(
          "classificationFile",
        ) as HTMLInputElement | null
      )?.files?.[0];
      const verificationFile = (
        formElement.elements.namedItem(
          "verificationFile",
        ) as HTMLInputElement | null
      )?.files?.[0];
      if (
        !classificationFile ||
        classificationFile.size === 0 ||
        !verificationFile ||
        verificationFile.size === 0
      ) {
        throw new Error(
          "Seleziona entrambi i PDF: classifica e verifica punteggi.",
        );
      }
      if (
        classificationFile.size > 20 * 1024 * 1024 ||
        verificationFile.size > 20 * 1024 * 1024
      ) {
        throw new Error("Ogni PDF deve avere dimensione massima di 20 MiB.");
      }
      if (
        !classificationFile.name.toLowerCase().endsWith(".pdf") ||
        !verificationFile.name.toLowerCase().endsWith(".pdf")
      ) {
        throw new Error("Seleziona due file con estensione .pdf.");
      }
      setPdfPreview(await freezerApi.previewPdfPair(form));
    } catch (cause) {
      setPdfError(errorMessage(cause));
    } finally {
      setPdfBusy(false);
    }
  }

  return (
    <div className="content-grid">
      <section className="section-heading">
        <div>
          <h1>Gare</h1>
          <p className="lede">
            Importa i risultati e costruisci una preparazione verificabile.
          </p>
        </div>
        <span className="count-chip">
          {matches.length} {matches.length === 1 ? "gara" : "gare"}
        </span>
      </section>
      <section className="match-list" aria-label="Gare importate">
        {matches.length === 0 ? (
          <div className="empty-state">
            <h2>La prima gara inizia qui.</h2>
            <p>Importa un CSV dei risultati per avviare l'analisi.</p>
          </div>
        ) : (
          matches.map((match) => (
            <Link
              className="match-row"
              to={`/matches/${encodeURIComponent(match.id)}`}
              key={match.id}
            >
              <span className="match-date">
                {formatDate(match.localMatchDate)}
              </span>
              <span className="match-main">
                <strong>{match.name}</strong>
                <small>{match.location || "Località non indicata"}</small>
              </span>
              <span className="match-meta">
                {match.stageCount} stage · {match.competitorCount} atleti
              </span>
              <span
                className={match.isFinal ? "status status-final" : "status"}
              >
                {match.isFinal ? "Finale" : "Provvisoria"}
              </span>
            </Link>
          ))
        )}
      </section>
      <section className="panel import-panel">
        <div className="panel-heading">
          <div>
            <h2>Importa una classifica</h2>
          </div>
        </div>
        <form
          className="import-form"
          onSubmit={(event) => void upload(event, false)}
        >
          <div className="form-columns">
            <label>
              Nome gara
              <input name="matchName" required maxLength={160} />
            </label>
            <label>
              Data locale
              <input name="matchDate" type="date" required />
            </label>
          </div>
          <label className="file-picker">
            File CSV
            <input name="file" type="file" accept=".csv,text/csv" required />
            <span>Seleziona un export CSV</span>
          </label>
          {error && <ErrorMessage>{error}</ErrorMessage>}
          <button className="button button-secondary" disabled={busy}>
            {busy ? "Analisi…" : "Analizza prima di importare"}
          </button>
        </form>
        {preview && (
          <div className="preview-result" aria-live="polite">
            <div className="preview-top">
              <div>
                <h3>{preview.matchName}</h3>
                <p className="quiet-copy">Anteprima · {preview.format}</p>
              </div>
              <span className="status">{preview.completeness}</span>
            </div>
            <div className="preview-stats">
              <span>
                <strong>{preview.stageCount}</strong> stage
              </span>
              <span>
                <strong>{preview.competitorCount}</strong> atleti
              </span>
              <span>
                <strong>{preview.unresolvedIdentityCount}</strong> identità da
                associare
              </span>
            </div>
            {preview.diagnostics.length > 0 && (
              <ul className="diagnostic-list">
                {preview.diagnostics.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            )}
            <p className="quiet-copy">
              Revisione attesa: {preview.expectedRevision}. La pubblicazione
              registra una nuova revisione e conserva l'origine.
            </p>
            <form onSubmit={(event) => void upload(event, true)}>
              <button
                type="submit"
                className="button button-primary"
                disabled={busy || !preview.canPublish || !sourceFile}
              >
                {busy ? "Pubblicazione…" : "Pubblica revisione"}
              </button>
              <p className="quiet-copy">
                Il file analizzato verrà pubblicato come revisione{" "}
                {preview.expectedRevision + 1}.
              </p>
            </form>
          </div>
        )}
        <section className="pdf-pair-import" aria-labelledby="pdf-pair-heading">
          <div className="panel-heading">
            <div>
              <h2 id="pdf-pair-heading">Anteprima da PDF</h2>
              <p className="quiet-copy">
                Carica insieme la classifica Overall e il report Verify. Questa
                anteprima non importa né pubblica risultati.
              </p>
            </div>
          </div>
          <form
            className="import-form"
            onSubmit={(event) => void previewPdfPair(event)}
          >
            <div className="form-columns">
              <label className="file-picker">
                Classifica Overall
                <input
                  name="classificationFile"
                  type="file"
                  accept=".pdf,application/pdf"
                  required
                  onChange={(event) =>
                    setClassificationPdfName(
                      event.currentTarget.files?.[0]?.name ?? "",
                    )
                  }
                />
                <span>
                  {classificationPdfName || "Seleziona il PDF della classifica"}
                </span>
              </label>
              <label className="file-picker">
                Verifica punteggi
                <input
                  name="verificationFile"
                  type="file"
                  accept=".pdf,application/pdf"
                  required
                  onChange={(event) =>
                    setVerificationPdfName(
                      event.currentTarget.files?.[0]?.name ?? "",
                    )
                  }
                />
                <span>{verificationPdfName || "Seleziona il PDF Verify"}</span>
              </label>
            </div>
            {pdfError && <ErrorMessage>{pdfError}</ErrorMessage>}
            <button
              type="submit"
              className="button button-secondary"
              disabled={pdfBusy}
            >
              {pdfBusy ? "Lettura dei report…" : "Analizza coppia PDF"}
            </button>
          </form>
          {pdfPreview && <PdfPairPreviewResult preview={pdfPreview} />}
        </section>
      </section>
      <p className="data-note">
        I risultati pubblicati sono condivisibili tra account; profilo, piano e
        note restano personali.
      </p>
    </div>
  );
}

function PdfPairPreviewResult({ preview }: { preview: PdfPairPreview }) {
  const numberFormat = new Intl.NumberFormat("it-IT", {
    maximumFractionDigits: 4,
  });
  const disqualifiedEntries = preview.disqualifiedEntries ?? [];
  const mappedDisqualifiedCount =
    preview.mappedDisqualifiedCompetitorCount ?? 0;
  const unmatchedDisqualifiedCount =
    preview.unmatchedDisqualifiedCompetitorCount ??
    preview.disqualifiedCompetitorCount;
  const pairingLabel = (status: string) =>
    ({
      matched: "Abbinato",
      ambiguous: "Ambiguo",
      classification_unmatched: "Solo in classifica",
      verification_unmatched: "Solo in Verify",
      unmatched: "Non abbinato",
    })[status] ?? "Da verificare";
  const disqualificationStatusLabel = (status: boolean | null | undefined) =>
    status === true
      ? "DQ indicata e abbinata"
      : status === false
        ? "Non presente nell’appendice"
        : "Stato DQ non verificabile";
  const disqualificationPairingLabel = (status: string) =>
    ({
      matched: "Abbinata a classifica e Verify",
      ambiguous: "Abbinamento ambiguo",
      unmatched: "Non abbinata",
    })[status] ?? "Da verificare";

  return (
    <div className="pdf-pair-result" aria-live="polite">
      <div className="pdf-pair-notice" role="status">
        <strong>Anteprima non pubblicabile</strong>
        <span>
          I dati estratti richiedono verifica e riconciliazione ufficiale prima
          di qualsiasi importazione.
        </span>
      </div>
      <dl className="pdf-pair-stats">
        <div>
          <dt>Pagine classifica</dt>
          <dd>{preview.classificationPageCount}</dd>
        </div>
        <div>
          <dt>Righe classifica lette</dt>
          <dd>
            {preview.classificationEntries.length} /{" "}
            {preview.classificationCompetitorCount}
          </dd>
        </div>
        <div>
          <dt>DQ contati</dt>
          <dd>{preview.disqualifiedCompetitorCount}</dd>
        </div>
        <div>
          <dt>Voci DQ abbinate</dt>
          <dd>
            {mappedDisqualifiedCount} / {preview.disqualifiedCompetitorCount}
          </dd>
        </div>
        <div>
          <dt>Voci DQ non verificate</dt>
          <dd>{unmatchedDisqualifiedCount}</dd>
        </div>
        <div>
          <dt>Pagine Verify</dt>
          <dd>{preview.verificationPageCount}</dd>
        </div>
        <div>
          <dt>Sezioni concorrente</dt>
          <dd>
            {preview.verificationEntries.length} /{" "}
            {preview.verificationCompetitorCount}
          </dd>
        </div>
        <div>
          <dt>Righe stage lette</dt>
          <dd>
            {preview.parsedVerificationStageResultCount} /{" "}
            {preview.verificationStageResultCount}
          </dd>
        </div>
        <div>
          <dt>Abbinamenti univoci</dt>
          <dd>{preview.matchedCompetitorCount}</dd>
        </div>
        <div>
          <dt>Classifiche senza Verify</dt>
          <dd>{preview.unmatchedClassificationCount}</dd>
        </div>
        <div>
          <dt>Verify senza classifica</dt>
          <dd>{preview.unmatchedVerificationCount}</dd>
        </div>
        <div>
          <dt>Gruppi ambigui</dt>
          <dd>{preview.ambiguousNameDivisionGroupCount}</dd>
        </div>
        <div>
          <dt>HF / punti / tempo da controllare</dt>
          <dd>{preview.hitFactorTimeMismatchCount}</dd>
        </div>
      </dl>
      {preview.diagnostics.length > 0 && (
        <ul className="diagnostic-list">
          {preview.diagnostics.map((item, index) => (
            <li key={`${index}-${item}`}>{item}</li>
          ))}
        </ul>
      )}
      {(preview.disqualifiedCompetitorCount > 0 ||
        disqualifiedEntries.length > 0) && (
        <details className="pdf-preview-details">
          <summary>
            Voci dell’appendice DQ ({disqualifiedEntries.length})
          </summary>
          {disqualifiedEntries.length > 0 ? (
            <div className="pdf-table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Atleta indicato</th>
                    <th>Divisione</th>
                    <th>Abbinamento</th>
                    <th>Pag.</th>
                  </tr>
                </thead>
                <tbody>
                  {disqualifiedEntries.map((entry, index) => (
                    <tr
                      key={`${entry.pageNumber}-${entry.displayName}-${index}`}
                    >
                      <td>{entry.displayName}</td>
                      <td>{entry.division}</td>
                      <td>
                        {disqualificationPairingLabel(entry.pairingStatus)}
                      </td>
                      <td>{entry.pageNumber}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="quiet-copy">
              Il report conta voci DQ, ma non ha fornito righe dell’appendice
              verificabili.
            </p>
          )}
        </details>
      )}
      <details className="pdf-preview-details">
        <summary>
          Righe della classifica ({preview.classificationEntries.length})
        </summary>
        <div className="pdf-table-scroll">
          <table>
            <thead>
              <tr>
                <th>Pos.</th>
                <th>Atleta</th>
                <th>Divisione</th>
                <th>%</th>
                <th>Punti</th>
                <th>Abbinamento</th>
                <th>Stato DQ</th>
                <th>Pag.</th>
              </tr>
            </thead>
            <tbody>
              {preview.classificationEntries.map((entry, index) => (
                <tr key={`${entry.pageNumber}-${entry.rank}-${index}`}>
                  <td>{entry.rank}</td>
                  <td>{entry.displayName}</td>
                  <td>{entry.division}</td>
                  <td>{numberFormat.format(entry.percent)}</td>
                  <td>{numberFormat.format(entry.points)}</td>
                  <td>{pairingLabel(entry.pairingStatus)}</td>
                  <td>{disqualificationStatusLabel(entry.isDisqualified)}</td>
                  <td>{entry.pageNumber}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </details>
      <details className="pdf-preview-details">
        <summary>
          Sezioni Verify e dettagli stage ({preview.verificationEntries.length})
        </summary>
        <ol className="pdf-verification-list">
          {preview.verificationEntries.map((entry, index) => (
            <li key={`${entry.competitorNumber}-${entry.pageNumber}-${index}`}>
              <details>
                <summary>
                  <span>
                    {entry.displayName} · {entry.division}
                    {entry.powerFactor ? ` · ${entry.powerFactor}` : ""}
                  </span>
                  <span>
                    N° {entry.competitorNumber} ·{" "}
                    {pairingLabel(entry.pairingStatus)} ·{" "}
                    {disqualificationStatusLabel(entry.isDisqualified)} ·{" "}
                    {entry.stages.length} stage · pag. {entry.pageNumber}
                  </span>
                </summary>
                <div className="pdf-table-scroll">
                  <table>
                    <thead>
                      <tr>
                        <th>Stage</th>
                        <th>HF</th>
                        <th>Punti</th>
                        <th>A</th>
                        <th>C</th>
                        <th>D</th>
                        <th>Ded.</th>
                        <th>MI</th>
                        <th>NS</th>
                        <th>PE</th>
                        <th>Tempo</th>
                        <th>OT</th>
                        <th>HF / punti / tempo</th>
                        <th>Pag.</th>
                      </tr>
                    </thead>
                    <tbody>
                      {entry.stages.map((stage) => (
                        <tr
                          key={`${entry.competitorNumber}-${stage.stageNumber}`}
                        >
                          <td>{stage.stageNumber}</td>
                          <td>
                            {stage.hitFactor === null
                              ? "—"
                              : numberFormat.format(stage.hitFactor)}
                          </td>
                          <td>{stage.reportedPoints ?? "—"}</td>
                          <td>{stage.alpha ?? "—"}</td>
                          <td>{stage.charlie ?? "—"}</td>
                          <td>{stage.delta ?? "—"}</td>
                          <td>{stage.deduction ?? "—"}</td>
                          <td>{stage.misses ?? "—"}</td>
                          <td>{stage.noShoots ?? "—"}</td>
                          <td>{stage.procedurals ?? "—"}</td>
                          <td>
                            {stage.timeSeconds === null
                              ? "—"
                              : numberFormat.format(stage.timeSeconds)}
                          </td>
                          <td>{stage.overtimeValue ?? "—"}</td>
                          <td>
                            {stage.hitFactorMatchesReportedPointsAndTime ===
                            null
                              ? "Non verificabile"
                              : stage.hitFactorMatchesReportedPointsAndTime
                                ? "Nessuna segnalazione"
                                : "Da controllare"}
                          </td>
                          <td>{stage.pageNumber}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </details>
            </li>
          ))}
        </ol>
      </details>
    </div>
  );
}


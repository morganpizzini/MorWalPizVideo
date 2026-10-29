import { useState, type FormEvent } from 'react';
import {
  Link,
  NavLink,
  Outlet,
  useLocation,
  useLoaderData,
  useNavigate,
  useRevalidator,
  useRouteLoaderData,
  useParams,
  useRouteError,
  isRouteErrorResponse,
  useMatches
} from 'react-router-dom';
import { useEffect } from 'react';
import {
  clearRememberedAccountId,
  freezerApi,
  FreezerApiError,
  rememberedAccountId,
  type AccountSession,
  type Draft,
  type DraftUpdate,
  type Estimate,
  type FieldEvent,
  type FieldEventInput,
  type IdentityReviewEntry,
  type ImportPreview,
  type MatchSummary,
  type ParameterAdoption,
  type ParameterBacktestReport,
  type ParameterValues,
  type PdfPairPreview,
  type Profile,
  type ShooterIdentity,
  type Snapshot,
  type SnapshotSelection,
  type SnapshotReplay,
  type VerificationReport
} from './freezerApi';
import {
  clearAccountOfflineData,
  enqueueFieldEvent,
  getSnapshot,
  queuedFieldEvents,
  removeQueuedFieldEvent,
  saveSnapshot
} from './offlineStore';

export interface RootData {
  session: AccountSession;
  offline: boolean;
}

function optionalPositiveInteger(value: FormDataEntryValue | null): number | undefined {
  const text = String(value ?? '').trim();
  if (!text) return undefined;
  const number = Number(text);
  if (!Number.isInteger(number) || number < 1) {
    throw new Error('I dati del turno devono essere numeri interi positivi.');
  }
  return number;
}

function localDateTime(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0');
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`;
}

export function RouteErrorPage() {
  const error = useRouteError();
  const message = isRouteErrorResponse(error)
    ? `${error.status}: ${error.statusText}`
    : error instanceof Error ? error.message : 'La pagina non è disponibile.';
  return (
    <main className="page-frame">
      <section className="auth-card" role="alert">
        <h1>Questa pagina non è pronta.</h1>
        <p className="lede">{message}</p>
        <Link className="button button-primary" to="/">Torna alle gare</Link>
      </section>
    </main>
  );
}

function rootData(): RootData {
  return useRouteLoaderData('root') as RootData;
}

function parseEventKind(value: FormDataEntryValue | null): FieldEventInput['kind'] {
  if (value === 'turn' || value === 'reconnaissance' || value === 'midday' || value === 'correction') return value;
  throw new Error('Seleziona un tipo di nota valido.');
}

function ErrorMessage({ children }: { children: string }) {
  return <p className="notice notice-error" role="alert">{children}</p>;
}

export function RootLayout() {
  const { session, offline } = rootData();
  const location = useLocation();
  const activeSnapshotId = useMatches()
    .map(match => match.params.snapshotId)
    .find((value): value is string => Boolean(value));
  const revalidator = useRevalidator();
  const [logoutError, setLogoutError] = useState('');
  const account = session.account;

  async function logout() {
    setLogoutError('');
    try {
      if (account) await clearAccountOfflineData(account.id);
      await freezerApi.logout();
      clearRememberedAccountId();
      revalidator.revalidate();
    } catch (error) {
      setLogoutError(errorMessage(error));
    }
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <Link className="brand" to="/" aria-label="FreezerAnalysis — gare">
          <span className="brand-mark" aria-hidden="true">F</span>
          <span>Freezer<span className="brand-light">Analysis</span></span>
        </Link>
        <nav className="main-nav" aria-label="Navigazione principale">
          <NavLink to="/" end>Gare</NavLink>
          {account && <NavLink to="/profile">Profilo</NavLink>}
        </nav>
        <div className="account-area">
          {offline && <span className="offline-pill">Offline</span>}
          {account ? (
            <>
              <span className="account-name">{account.displayName || account.username}</span>
              {!offline && <button className="button button-quiet" onClick={() => void logout()}>Esci</button>}
            </>
          ) : <Link className="button button-quiet" to="/login">Accedi</Link>}
        </div>
      </header>
      {logoutError && <div className="shell-message"><ErrorMessage>{logoutError}</ErrorMessage></div>}
      <main className="page-frame">
        {!account && !offline && location.pathname !== '/login' ? <LoginPrompt /> : <Outlet />}
      </main>
      {account && <nav className="mobile-nav" aria-label="Navigazione mobile">
        <NavLink to="/" end>Gare</NavLink>
        <NavLink to="/profile">Profilo</NavLink>
        {activeSnapshotId && <NavLink to={`/field/${encodeURIComponent(activeSnapshotId)}`}>Campo</NavLink>}
      </nav>}
      <footer className="site-footer">
        <span>FreezerAnalysis</span>
        <span>Preparazione ragionata. Decisioni sempre tue.</span>
      </footer>
    </div>
  );
}

function LoginPrompt() {
  return (
    <section className="welcome-panel">
      <h1>La gara, letta con lucidità.</h1>
      <p>Accedi per importare risultati, preparare il piano e portare le note con te sul campo.</p>
      <Link to="/login" className="button button-primary">Accedi o crea un account</Link>
    </section>
  );
}

export function LoginPage() {
  const navigate = useNavigate();
  const revalidator = useRevalidator();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setBusy(true);
    const form = new FormData(event.currentTarget);
    try {
      const previousAccountId = rememberedAccountId();
      let session: AccountSession;
      if (mode === 'login') {
        session = await freezerApi.login(String(form.get('identifier') ?? ''), String(form.get('password') ?? ''));
      } else {
        session = await freezerApi.register({
          username: String(form.get('username') ?? ''),
          email: String(form.get('email') ?? ''),
          displayName: String(form.get('displayName') ?? ''),
          password: String(form.get('password') ?? ''),
          termsAccepted: form.get('termsAccepted') === 'on',
          privacyNoticeAcknowledged: form.get('privacyNoticeAcknowledged') === 'on'
        });
      }
      if (previousAccountId && session.account && previousAccountId !== session.account.id)
        await clearAccountOfflineData(previousAccountId);
      revalidator.revalidate();
      navigate('/');
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="auth-card">
      <h1>{mode === 'login' ? 'Bentornato.' : 'Crea il tuo account.'}</h1>
      <div className="segmented-control" role="group" aria-label="Tipo di accesso">
        <button className={mode === 'login' ? 'selected' : ''} onClick={() => setMode('login')}>Accedi</button>
        <button className={mode === 'register' ? 'selected' : ''} onClick={() => setMode('register')}>Registrati</button>
      </div>
      <form className="form-stack" onSubmit={event => void submit(event)}>
        {mode === 'register' && <>
          <label>Nome utente<input name="username" required minLength={3} maxLength={40} autoComplete="username" /></label>
          <label>Nome visualizzato<input name="displayName" maxLength={120} autoComplete="name" /></label>
          <label>Email<input name="email" type="email" required autoComplete="email" /></label>
        </>}
        {mode === 'login' && <label>Email o nome utente<input name="identifier" required autoComplete="username" /></label>}
        <label>Password<input name="password" type="password" required minLength={8} autoComplete={mode === 'login' ? 'current-password' : 'new-password'} /></label>
        {mode === 'register' && <div className="checkbox-stack">
          <label className="checkbox-label"><input type="checkbox" name="termsAccepted" required /> Accetto i termini del servizio.</label>
          <label className="checkbox-label"><input type="checkbox" name="privacyNoticeAcknowledged" required /> Ho letto l'informativa privacy.</label>
        </div>}
        {error && <ErrorMessage>{error}</ErrorMessage>}
        <button className="button button-primary button-wide" disabled={busy}>
          {busy ? 'Attendi…' : mode === 'login' ? 'Accedi' : 'Crea account'}
        </button>
      </form>
    </section>
  );
}

export function MatchesPage() {
  const matches = useLoaderData() as MatchSummary[];
  const revalidator = useRevalidator();
  const [error, setError] = useState('');
  const [preview, setPreview] = useState<ImportPreview | null>(null);
  const [sourceFile, setSourceFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [pdfPreview, setPdfPreview] = useState<PdfPairPreview | null>(null);
  const [pdfError, setPdfError] = useState('');
  const [pdfBusy, setPdfBusy] = useState(false);
  const [classificationPdfName, setClassificationPdfName] = useState('');
  const [verificationPdfName, setVerificationPdfName] = useState('');

  async function upload(event: FormEvent<HTMLFormElement>, publish: boolean) {
    event.preventDefault();
    setError('');
    setBusy(true);
    const form = new FormData(event.currentTarget);
    try {
      if (publish) {
        if (!preview || !sourceFile) throw new Error('Analizza nuovamente il CSV prima di pubblicarlo.');
        const publishForm = new FormData();
        publishForm.set('file', sourceFile);
        publishForm.set('matchName', preview.matchName);
        publishForm.set('matchDate', preview.localMatchDate);
        const result = await freezerApi.publishScores(publishForm, preview.expectedRevision);
        setPreview(result);
        setSourceFile(null);
        revalidator.revalidate();
      } else {
        const file = form.get('file');
        if (!(file instanceof File) || file.size === 0) throw new Error('Seleziona un file CSV valido.');
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
    setPdfError('');
    setPdfPreview(null);
    setPdfBusy(true);
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    try {
      const classificationFile = (formElement.elements.namedItem('classificationFile') as HTMLInputElement | null)?.files?.[0];
      const verificationFile = (formElement.elements.namedItem('verificationFile') as HTMLInputElement | null)?.files?.[0];
      if (!classificationFile || classificationFile.size === 0 ||
          !verificationFile || verificationFile.size === 0) {
        throw new Error('Seleziona entrambi i PDF: classifica e verifica punteggi.');
      }
      if (classificationFile.size > 20 * 1024 * 1024 || verificationFile.size > 20 * 1024 * 1024) {
        throw new Error('Ogni PDF deve avere dimensione massima di 20 MiB.');
      }
      if (!classificationFile.name.toLowerCase().endsWith('.pdf') ||
          !verificationFile.name.toLowerCase().endsWith('.pdf')) {
        throw new Error('Seleziona due file con estensione .pdf.');
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
          <p className="lede">Importa i risultati e costruisci una preparazione verificabile.</p>
        </div>
        <span className="count-chip">{matches.length} {matches.length === 1 ? 'gara' : 'gare'}</span>
      </section>
      <section className="match-list" aria-label="Gare importate">
        {matches.length === 0 ? (
          <div className="empty-state"><h2>La prima gara inizia qui.</h2><p>Importa un CSV dei risultati per avviare l'analisi.</p></div>
        ) : matches.map(match => (
          <Link className="match-row" to={`/matches/${encodeURIComponent(match.id)}`} key={match.id}>
            <span className="match-date">{formatDate(match.localMatchDate)}</span>
            <span className="match-main"><strong>{match.name}</strong><small>{match.location || 'Località non indicata'}</small></span>
            <span className="match-meta">{match.stageCount} stage · {match.competitorCount} atleti</span>
            <span className={match.isFinal ? 'status status-final' : 'status'}>{match.isFinal ? 'Finale' : 'Provvisoria'}</span>
          </Link>
        ))}
      </section>
      <section className="panel import-panel">
        <div className="panel-heading">
          <div><h2>Importa una classifica</h2></div>
        </div>
        <form className="import-form" onSubmit={event => void upload(event, false)}>
          <div className="form-columns">
            <label>Nome gara<input name="matchName" required maxLength={160} /></label>
            <label>Data locale<input name="matchDate" type="date" required /></label>
          </div>
          <label className="file-picker">File CSV<input name="file" type="file" accept=".csv,text/csv" required /><span>Seleziona un export CSV</span></label>
          {error && <ErrorMessage>{error}</ErrorMessage>}
          <button className="button button-secondary" disabled={busy}>{busy ? 'Analisi…' : 'Analizza prima di importare'}</button>
        </form>
        {preview && <div className="preview-result" aria-live="polite">
          <div className="preview-top"><div><h3>{preview.matchName}</h3><p className="quiet-copy">Anteprima · {preview.format}</p></div><span className="status">{preview.completeness}</span></div>
          <div className="preview-stats">
            <span><strong>{preview.stageCount}</strong> stage</span>
            <span><strong>{preview.competitorCount}</strong> atleti</span>
            <span><strong>{preview.unresolvedIdentityCount}</strong> identità da associare</span>
          </div>
          {preview.diagnostics.length > 0 && <ul className="diagnostic-list">{preview.diagnostics.map(item => <li key={item}>{item}</li>)}</ul>}
          <p className="quiet-copy">Revisione attesa: {preview.expectedRevision}. La pubblicazione registra una nuova revisione e conserva l'origine.</p>
          <form onSubmit={event => void upload(event, true)}>
            <button type="submit" className="button button-primary" disabled={busy || !preview.canPublish || !sourceFile}>{busy ? 'Pubblicazione…' : 'Pubblica revisione'}</button>
            <p className="quiet-copy">Il file analizzato verrà pubblicato come revisione {preview.expectedRevision + 1}.</p>
          </form>
        </div>}
        <section className="pdf-pair-import" aria-labelledby="pdf-pair-heading">
          <div className="panel-heading">
            <div>
              <h2 id="pdf-pair-heading">Anteprima da PDF</h2>
              <p className="quiet-copy">Carica insieme la classifica Overall e il report Verify. Questa anteprima non importa né pubblica risultati.</p>
            </div>
          </div>
          <form className="import-form" onSubmit={event => void previewPdfPair(event)}>
            <div className="form-columns">
              <label className="file-picker">Classifica Overall<input name="classificationFile" type="file" accept=".pdf,application/pdf" required onChange={event => setClassificationPdfName(event.currentTarget.files?.[0]?.name ?? '')} /><span>{classificationPdfName || 'Seleziona il PDF della classifica'}</span></label>
              <label className="file-picker">Verifica punteggi<input name="verificationFile" type="file" accept=".pdf,application/pdf" required onChange={event => setVerificationPdfName(event.currentTarget.files?.[0]?.name ?? '')} /><span>{verificationPdfName || 'Seleziona il PDF Verify'}</span></label>
            </div>
            {pdfError && <ErrorMessage>{pdfError}</ErrorMessage>}
            <button type="submit" className="button button-secondary" disabled={pdfBusy}>
              {pdfBusy ? 'Lettura dei report…' : 'Analizza coppia PDF'}
            </button>
          </form>
          {pdfPreview && <PdfPairPreviewResult preview={pdfPreview} />}
        </section>
      </section>
      <p className="data-note">I risultati pubblicati sono condivisibili tra account; profilo, piano e note restano personali.</p>
    </div>
  );
}

function PdfPairPreviewResult({ preview }: { preview: PdfPairPreview }) {
  const numberFormat = new Intl.NumberFormat('it-IT', { maximumFractionDigits: 4 });
  const disqualifiedEntries = preview.disqualifiedEntries ?? [];
  const mappedDisqualifiedCount = preview.mappedDisqualifiedCompetitorCount ?? 0;
  const unmatchedDisqualifiedCount =
    preview.unmatchedDisqualifiedCompetitorCount ?? preview.disqualifiedCompetitorCount;
  const pairingLabel = (status: string) => ({
    matched: 'Abbinato',
    ambiguous: 'Ambiguo',
    classification_unmatched: 'Solo in classifica',
    verification_unmatched: 'Solo in Verify',
    unmatched: 'Non abbinato'
  }[status] ?? 'Da verificare');
  const disqualificationStatusLabel = (status: boolean | null | undefined) =>
    status === true ? 'DQ indicata e abbinata' :
      status === false ? 'Non presente nell’appendice' : 'Stato DQ non verificabile';
  const disqualificationPairingLabel = (status: string) => ({
    matched: 'Abbinata a classifica e Verify',
    ambiguous: 'Abbinamento ambiguo',
    unmatched: 'Non abbinata'
  }[status] ?? 'Da verificare');

  return (
    <div className="pdf-pair-result" aria-live="polite">
      <div className="pdf-pair-notice" role="status">
        <strong>Anteprima non pubblicabile</strong>
        <span>I dati estratti richiedono verifica e riconciliazione ufficiale prima di qualsiasi importazione.</span>
      </div>
      <dl className="pdf-pair-stats">
        <div><dt>Pagine classifica</dt><dd>{preview.classificationPageCount}</dd></div>
        <div><dt>Righe classifica lette</dt><dd>{preview.classificationEntries.length} / {preview.classificationCompetitorCount}</dd></div>
        <div><dt>DQ contati</dt><dd>{preview.disqualifiedCompetitorCount}</dd></div>
        <div><dt>Voci DQ abbinate</dt><dd>{mappedDisqualifiedCount} / {preview.disqualifiedCompetitorCount}</dd></div>
        <div><dt>Voci DQ non verificate</dt><dd>{unmatchedDisqualifiedCount}</dd></div>
        <div><dt>Pagine Verify</dt><dd>{preview.verificationPageCount}</dd></div>
        <div><dt>Sezioni concorrente</dt><dd>{preview.verificationEntries.length} / {preview.verificationCompetitorCount}</dd></div>
        <div><dt>Righe stage lette</dt><dd>{preview.parsedVerificationStageResultCount} / {preview.verificationStageResultCount}</dd></div>
        <div><dt>Abbinamenti univoci</dt><dd>{preview.matchedCompetitorCount}</dd></div>
        <div><dt>Classifiche senza Verify</dt><dd>{preview.unmatchedClassificationCount}</dd></div>
        <div><dt>Verify senza classifica</dt><dd>{preview.unmatchedVerificationCount}</dd></div>
        <div><dt>Gruppi ambigui</dt><dd>{preview.ambiguousNameDivisionGroupCount}</dd></div>
        <div><dt>HF / punti / tempo da controllare</dt><dd>{preview.hitFactorTimeMismatchCount}</dd></div>
      </dl>
      {preview.diagnostics.length > 0 && (
        <ul className="diagnostic-list">{preview.diagnostics.map((item, index) => <li key={`${index}-${item}`}>{item}</li>)}</ul>
      )}
      {(preview.disqualifiedCompetitorCount > 0 || disqualifiedEntries.length > 0) && (
        <details className="pdf-preview-details">
          <summary>Voci dell’appendice DQ ({disqualifiedEntries.length})</summary>
          {disqualifiedEntries.length > 0 ? (
            <div className="pdf-table-scroll">
              <table>
                <thead><tr><th>Atleta indicato</th><th>Divisione</th><th>Abbinamento</th><th>Pag.</th></tr></thead>
                <tbody>{disqualifiedEntries.map((entry, index) => (
                  <tr key={`${entry.pageNumber}-${entry.displayName}-${index}`}>
                    <td>{entry.displayName}</td><td>{entry.division}</td>
                    <td>{disqualificationPairingLabel(entry.pairingStatus)}</td><td>{entry.pageNumber}</td>
                  </tr>
                ))}</tbody>
              </table>
            </div>
          ) : (
            <p className="quiet-copy">Il report conta voci DQ, ma non ha fornito righe dell’appendice verificabili.</p>
          )}
        </details>
      )}
      <details className="pdf-preview-details">
        <summary>Righe della classifica ({preview.classificationEntries.length})</summary>
        <div className="pdf-table-scroll">
          <table>
            <thead><tr><th>Pos.</th><th>Atleta</th><th>Divisione</th><th>%</th><th>Punti</th><th>Abbinamento</th><th>Stato DQ</th><th>Pag.</th></tr></thead>
            <tbody>{preview.classificationEntries.map((entry, index) => (
              <tr key={`${entry.pageNumber}-${entry.rank}-${index}`}>
                <td>{entry.rank}</td><td>{entry.displayName}</td><td>{entry.division}</td>
                <td>{numberFormat.format(entry.percent)}</td><td>{numberFormat.format(entry.points)}</td>
                <td>{pairingLabel(entry.pairingStatus)}</td>
                <td>{disqualificationStatusLabel(entry.isDisqualified)}</td><td>{entry.pageNumber}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      </details>
      <details className="pdf-preview-details">
        <summary>Sezioni Verify e dettagli stage ({preview.verificationEntries.length})</summary>
        <ol className="pdf-verification-list">
          {preview.verificationEntries.map((entry, index) => (
            <li key={`${entry.competitorNumber}-${entry.pageNumber}-${index}`}>
              <details>
                <summary>
                  <span>{entry.displayName} · {entry.division}{entry.powerFactor ? ` · ${entry.powerFactor}` : ''}</span>
                      <span>N° {entry.competitorNumber} · {pairingLabel(entry.pairingStatus)} · {disqualificationStatusLabel(entry.isDisqualified)} · {entry.stages.length} stage · pag. {entry.pageNumber}</span>
                </summary>
                <div className="pdf-table-scroll">
                  <table>
                        <thead><tr><th>Stage</th><th>HF</th><th>Punti</th><th>A</th><th>C</th><th>D</th><th>Ded.</th><th>MI</th><th>NS</th><th>PE</th><th>Tempo</th><th>OT</th><th>HF / punti / tempo</th><th>Pag.</th></tr></thead>
                    <tbody>{entry.stages.map(stage => (
                      <tr key={`${entry.competitorNumber}-${stage.stageNumber}`}>
                        <td>{stage.stageNumber}</td>
                        <td>{stage.hitFactor === null ? '—' : numberFormat.format(stage.hitFactor)}</td>
                        <td>{stage.reportedPoints ?? '—'}</td><td>{stage.alpha ?? '—'}</td><td>{stage.charlie ?? '—'}</td>
                            <td>{stage.delta ?? '—'}</td><td>{stage.deduction ?? '—'}</td><td>{stage.misses ?? '—'}</td>
                            <td>{stage.noShoots ?? '—'}</td><td>{stage.procedurals ?? '—'}</td>
                            <td>{stage.timeSeconds === null ? '—' : numberFormat.format(stage.timeSeconds)}</td>
                            <td>{stage.overtimeValue ?? '—'}</td>
                            <td>{stage.hitFactorMatchesReportedPointsAndTime === null ? 'Non verificabile' : stage.hitFactorMatchesReportedPointsAndTime ? 'Nessuna segnalazione' : 'Da controllare'}</td>
                            <td>{stage.pageNumber}</td>
                      </tr>
                    ))}</tbody>
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

export function ProfilePage() {
  const data = useLoaderData() as { profile: Profile; identities: ShooterIdentity[] } | null;
  const profile = data?.profile;
  const revalidator = useRevalidator();
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [creatingIdentity, setCreatingIdentity] = useState(false);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!profile) {
      setError('Accedi per modificare il profilo.');
      return;
    }
    setBusy(true);
    setError('');
    setMessage('');
    const form = new FormData(event.currentTarget);
    try {
      await freezerApi.saveProfile({
        identityId: String(form.get('identityId') ?? '') || null,
        division: String(form.get('division') ?? '') || null,
        powerFactor: String(form.get('powerFactor') ?? '') || null,
        performance: Number(form.get('performance'))
      });
      setMessage('Profilo aggiornato.');
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function createIdentity(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    setError('');
    setMessage('');
    setCreatingIdentity(true);
    const form = new FormData(formElement);
    const aliases = String(form.get('aliases') ?? '')
      .split(',')
      .map(value => value.trim())
      .filter(Boolean);
    try {
      const identity = await freezerApi.createIdentity(String(form.get('displayName') ?? ''), aliases);
      setMessage(`Identità creata: ${identity.aliases[0] ?? identity.normalizedName}. Associala ai risultati dopo averli verificati.`);
      formElement.reset();
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setCreatingIdentity(false);
    }
  }

  return (
    <section className="form-panel">
      <h1>Profilo tiratore</h1>
      <p className="lede">Queste impostazioni sono private e non modificano le classifiche condivise.</p>
      <form className="form-stack" onSubmit={event => void save(event)}>
        {!profile && <ErrorMessage>Accedi per visualizzare e modificare il profilo.</ErrorMessage>}
        <label>Identità agonistica
          <select name="identityId" defaultValue={profile?.identityId ?? ''} disabled={!profile}>
            <option value="">Non associata</option>
            {(data?.identities ?? []).map(identity => (
              <option key={identity.id} value={identity.id}>{identity.aliases[0] ?? identity.normalizedName}</option>
            ))}
          </select>
        </label>
        <p className="quiet-copy">Le identità si creano e si associano dalla revisione della gara; nessuna corrispondenza viene applicata automaticamente.</p>
        <div className="form-columns">
          <label>Divisione<input name="division" defaultValue={profile?.division ?? ''} maxLength={32} /></label>
          <label>Power factor<select name="powerFactor" defaultValue={profile?.powerFactor ?? ''}><option value="">Non impostato</option><option>Major</option><option>Minor</option></select></label>
        </div>
        <label>Performance personale<input name="performance" type="number" min="0.01" max="1" step="0.001" defaultValue={profile?.performance ?? 0.935} /></label>
        {error && <ErrorMessage>{error}</ErrorMessage>}
        {message && <p className="notice notice-success" role="status">{message}</p>}
        <button className="button button-primary" disabled={busy}>{busy ? 'Salvataggio…' : 'Salva profilo'}</button>
      </form>
      <form className="form-stack identity-create-form" onSubmit={event => void createIdentity(event)}>
        <h2>Crea un’identità agonistica</h2>
        <p className="quiet-copy">Gli alias vengono aggiunti solo su tua conferma; non associano automaticamente i risultati.</p>
        <label>Nome principale<input name="displayName" required maxLength={100} disabled={creatingIdentity} /></label>
        <label>Altri nomi confermati
          <input name="aliases" maxLength={2000} disabled={creatingIdentity} placeholder="Un alias, un alias alternativo" />
          <span className="field-hint">Separali con una virgola. Questo flusso non acquisisce licenze: inserisci solo nomi alternativi che puoi confermare.</span>
        </label>
        <button className="button button-secondary" disabled={creatingIdentity || !profile}>
          {creatingIdentity ? 'Creazione…' : 'Crea identità'}
        </button>
      </form>
    </section>
  );
}

interface IdentityReviewLoaderData {
  entries: IdentityReviewEntry[] | null;
  offline: boolean;
}

export function IdentityReviewPage() {
  const { matchId } = useParams();
  const data = useLoaderData() as IdentityReviewLoaderData | null;
  const revalidator = useRevalidator();
  const [busyResultId, setBusyResultId] = useState('');
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');

  async function assign(event: FormEvent<HTMLFormElement>, entry: IdentityReviewEntry) {
    event.preventDefault();
    if (!matchId) return;
    const form = new FormData(event.currentTarget);
    const identityId = String(form.get('identityId') ?? '');
    if (!identityId) {
      setError('Seleziona un’identità prima di confermare l’associazione.');
      return;
    }

    setBusyResultId(entry.resultId);
    setError('');
    setStatus('');
    try {
      await freezerApi.resolveIdentity(matchId, entry.resultId, identityId, entry.resolutionRevision);
      setStatus(`Associazione aggiornata per ${entry.displayName}.`);
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
      if (cause instanceof FreezerApiError && cause.status === 409) revalidator.revalidate();
    } finally {
      setBusyResultId('');
    }
  }

  async function createAndAssign(entry: IdentityReviewEntry) {
    if (!matchId) return;
    setBusyResultId(entry.resultId);
    setError('');
    setStatus('');
    try {
      const identity = await freezerApi.createIdentity(entry.displayName);
      await freezerApi.resolveIdentity(matchId, entry.resultId, identity.id, entry.resolutionRevision);
      setStatus(`Creata e associata una nuova identità per ${entry.displayName}.`);
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusyResultId('');
    }
  }

  async function remove(entry: IdentityReviewEntry) {
    if (!matchId) return;
    setBusyResultId(entry.resultId);
    setError('');
    setStatus('');
    try {
      await freezerApi.removeIdentityResolution(matchId, entry.resultId, entry.resolutionRevision);
      setStatus(`Associazione rimossa per ${entry.displayName}.`);
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
      if (cause instanceof FreezerApiError && cause.status === 409) revalidator.revalidate();
    } finally {
      setBusyResultId('');
    }
  }

  return (
    <div className="content-grid">
      <Link to={matchId ? `/matches/${encodeURIComponent(matchId)}` : '/'} className="back-link">Torna alla preparazione</Link>
      <section className="section-heading">
        <div>
          <h1>Revisione identità</h1>
          <p className="lede">Associa i risultati a identità stabili solo dopo aver verificato nome e divisione. Le associazioni restano private al tuo account.</p>
        </div>
      </section>
      {data?.offline && <div className="notice notice-warning" role="status">La revisione identità richiede una connessione attiva.</div>}
      {error && <ErrorMessage>{error}</ErrorMessage>}
      {status && <p className="notice notice-success" role="status">{status}</p>}
      {data?.entries && data.entries.length === 0 && (
        <div className="empty-state"><h2>Nessun risultato da associare.</h2><p>Pubblica una revisione della gara prima di rivedere le identità.</p></div>
      )}
      {data?.entries && data.entries.length > 0 && (
        <ol className="identity-list" aria-label="Risultati della gara da verificare">
          {data.entries.map(entry => {
            const busy = busyResultId === entry.resultId;
            return (
              <li className="identity-row" key={entry.resultId}>
                <div className="identity-result">
                  <h2>{entry.displayName}</h2>
                  <p>{entry.division} · ID locale {entry.competitorId}</p>
                  <span className={entry.identityId ? 'status status-final' : 'status status-warning'}>
                    {entry.identityId ? 'Associata' : 'Da associare'}
                  </span>
                  {entry.identityId && <small className="identity-reference">Identità {entry.identityId.slice(-8)} · revisione {entry.resolutionRevision}</small>}
                </div>
                <div className="identity-actions">
                  <form className="identity-controls" onSubmit={event => void assign(event, entry)}>
                    <label>Identità confermata
                      <select name="identityId" defaultValue={entry.identityId ?? ''} disabled={busy} required>
                        <option value="">Scegli una corrispondenza esatta</option>
                        {entry.candidates.map(identity => (
                          <option key={identity.id} value={identity.id}>
                            {identity.aliases[0] ?? identity.normalizedName} · {identity.id.slice(-8)}
                          </option>
                        ))}
                      </select>
                    </label>
                    <button className="button button-secondary" disabled={busy || entry.candidates.length === 0}>
                      {busy ? 'Salvataggio…' : 'Conferma associazione'}
                    </button>
                  </form>
                  <div className="identity-secondary-actions">
                    <button className="button button-secondary" disabled={busy} onClick={() => void createAndAssign(entry)}>
                      Crea identità distinta
                    </button>
                    {entry.identityId && <button className="button button-quiet" disabled={busy} onClick={() => void remove(entry)}>
                      Rimuovi associazione
                    </button>}
                  </div>
                </div>
              </li>
            );
          })}
        </ol>
      )}
    </div>
  );
}

interface MatchLoaderData {
  draft: Draft;
  estimate: Estimate;
}

export function MatchPage() {
  const { matchId } = useParams();
  const data = useLoaderData() as MatchLoaderData | null;
  const draft = data?.draft;
  const estimate = data?.estimate;
  const profile = rootData().session.account;
  const navigate = useNavigate();
  const revalidator = useRevalidator();
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [busy, setBusy] = useState(false);

  async function saveAndFreeze(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!matchId || !draft) {
      setError('Accedi per modificare il piano.');
      return;
    }
    setBusy(true);
    setError('');
    const form = new FormData(event.currentTarget);
    const strategies = { ...draft.strategyByStage };
    for (const stage of estimate?.stages ?? []) {
      strategies[stage.stageId] = String(form.get(`strategy:${stage.stageId}`) ?? '');
    }
    const request: DraftUpdate = {
      expectedRevision: draft.revision,
      importManifestId: draft.importManifestId,
      identityId: String(form.get('identityId') ?? '') || null,
      division: String(form.get('division') ?? '') || null,
      beta: Number(form.get('beta')),
      driftPercent: Number(form.get('driftPercent')),
      ownVariability: Number(form.get('ownVariability')),
      distancePenalty: Number(form.get('distancePenalty')),
      performance: Number(form.get('performance')),
      applyAfternoon: form.get('applyAfternoon') === 'on',
      applyOneHand: form.get('applyOneHand') === 'on',
      excludedStageIds: (estimate?.stages ?? [])
        .filter(stage => form.get(`exclude:${stage.stageId}`) === 'on')
        .map(stage => stage.stageId),
      rivalIdentityIds: draft.rivalIdentityIds,
      strategyByStage: strategies
    };
    let draftSaved = false;
    try {
      const updated = await freezerApi.saveDraft(matchId, request);
      draftSaved = true;
      if (form.get('intent') === 'save') {
        setStatus(`Bozza salvata alla revisione ${updated.revision}.`);
        revalidator.revalidate();
        return;
      }
      const snapshot = await freezerApi.freeze(matchId, updated.revision);
      if (!profile?.id) throw new Error('Account non disponibile per salvare la copia offline.');
      try {
        await saveSnapshot(profile.id, snapshot);
      } catch (storageError) {
        setError(`Piano congelato sul server, ma la copia offline non è stata salvata: ${errorMessage(storageError)}`);
        revalidator.revalidate();
        return;
      }
      navigate(`/field/${encodeURIComponent(snapshot.id)}`);
    } catch (cause) {
      setError(errorMessage(cause));
      if (draftSaved) revalidator.revalidate();
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="content-grid">
      <Link to="/" className="back-link">Tutte le gare</Link>
      {(!draft || !estimate) && <ErrorMessage>Accedi per visualizzare e preparare questa gara.</ErrorMessage>}
      {draft && estimate && <>
      <section className="section-heading">
        <div><h1>Preparazione gara</h1><p className="quiet-copy">Revisione del piano {draft.revision}</p></div>
        <span className={estimate.isAvailable ? 'status status-final' : 'status status-warning'}>{estimate.isAvailable ? 'Stima disponibile' : 'Stima incompleta'}</span>
      </section>
      <Link className="button button-secondary identity-review-link" to={`/matches/${encodeURIComponent(matchId ?? '')}/identities`}>
        Rivedi identità dei risultati
      </Link>
      {!estimate.isAvailable && <div className="notice notice-warning">
        <strong>Stima non ancora verificabile.</strong>
        <ul>{estimate.diagnostics.map(item => <li key={item}>{item}</li>)}</ul>
        Il congelamento salva una copia immutabile del piano corrente; non trasforma una stima incompleta in un risultato.
      </div>}
      <div className="plan-layout">
        <section className="panel">
          <div className="panel-heading"><div><h2>Definisci il piano</h2></div></div>
          <form className="form-stack" onSubmit={event => void saveAndFreeze(event)}>
            <div className="form-columns">
              <label>Identità<input name="identityId" defaultValue={draft.identityId ?? ''} maxLength={100} /></label>
              <label>Divisione<input name="division" defaultValue={draft.division ?? ''} maxLength={32} /></label>
            </div>
            <div className="form-columns">
              <label>Beta<input name="beta" type="number" min="0.1" max="3" step="0.01" defaultValue={draft.beta} /></label>
              <label>Deriva %<input name="driftPercent" type="number" min="0" max="0.5" step="0.005" defaultValue={draft.driftPercent} /></label>
              <label>Variabilità<input name="ownVariability" type="number" min="0" max="0.5" step="0.005" defaultValue={draft.ownVariability} /></label>
              <label>Penalità distanza<input name="distancePenalty" type="number" min="0" max="2" step="0.05" defaultValue={draft.distancePenalty} /></label>
            </div>
            <label>Performance personale<input name="performance" type="number" min="0.01" max="1" step="0.001" defaultValue={draft.performance} /></label>
            <label className="checkbox-label"><input name="applyAfternoon" type="checkbox" defaultChecked={draft.applyAfternoon} /> Considera l'effetto pomeridiano</label>
            <label className="checkbox-label"><input name="applyOneHand" type="checkbox" defaultChecked={draft.applyOneHand} /> Considera gli stage a una mano</label>
            <fieldset className="stage-options">
              <legend>Decisioni per stage</legend>
              {estimate.stages.map(stage => (
                <div className="stage-option" key={stage.stageId}>
                  <label className="checkbox-label">
                    <input name={`exclude:${stage.stageId}`} type="checkbox" defaultChecked={draft.excludedStageIds.includes(stage.stageId)} />
                    Escludi stage {stage.stageNumber} dai calcoli
                  </label>
                  <label>Strategia / nota<textarea name={`strategy:${stage.stageId}`} rows={2} maxLength={1000} defaultValue={draft.strategyByStage[stage.stageId] ?? ''} /></label>
                </div>
              ))}
            </fieldset>
            {error && <ErrorMessage>{error}</ErrorMessage>}
            {status && <p className="notice notice-success" role="status">{status}</p>}
            <div className="action-row">
              <button className="button button-secondary" name="intent" value="save" disabled={busy}>{busy ? 'Salvataggio…' : 'Salva bozza'}</button>
              <button className="button button-primary" name="intent" value="freeze" disabled={busy}>{busy ? 'Congelamento…' : 'Salva e congela il piano'}</button>
            </div>
          </form>
        </section>
        <aside className="estimate-panel">
          <h2>Stima per stage</h2>
          <p className="quiet-copy">Modello {estimate.engineVersion}</p>
          <p className="quiet-copy">Dispersione: {estimate.dispersionConvention}</p>
          {estimate.stages.length === 0 ? <p className="muted-copy">Nessun dato di stage disponibile per questa stima.</p> : estimate.stages.map(stage => (
            <article className="stage-estimate" key={stage.stageId}>
              <div><span>STAGE {String(stage.stageNumber).padStart(2, '0')}</span><strong>{stage.center === null ? '—' : stage.center.toFixed(2)}</strong></div>
              <p>{stage.minimumRounds} colpi minimi{stage.rhythmSymbol ? ` · ritmo ${stage.rhythmSymbol}` : ''}</p>
              <small>Intervallo {stage.lower?.toFixed(2) ?? '—'}–{stage.upper?.toFixed(2) ?? '—'}</small>
            </article>
          ))}
        </aside>
      </div>
      </>}
    </div>
  );
}

interface FieldLoaderData {
  snapshot: Snapshot;
  events: FieldEvent[];
  offline: boolean;
}

interface VerificationLoaderData {
  snapshot: Snapshot;
  match: MatchSummary | null;
}

export function VerificationPage() {
  const data = useLoaderData() as VerificationLoaderData | null;
  const offline = (useRouteLoaderData('root') as RootData | undefined)?.offline ?? false;
  const { snapshotId } = useParams();
  const [report, setReport] = useState<VerificationReport | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [backtestError, setBacktestError] = useState('');
  const [backtestBusy, setBacktestBusy] = useState(false);
  const [backtest, setBacktest] = useState<ParameterBacktestReport | null>(null);
  const [snapshots, setSnapshots] = useState<SnapshotSelection[]>([]);
  const [matches, setMatches] = useState<MatchSummary[]>([]);
  const [selectionLoading, setSelectionLoading] = useState(true);
  const [selectionError, setSelectionError] = useState('');
  const [selectedSnapshotIds, setSelectedSnapshotIds] = useState<string[]>([]);
  const [parameters, setParameters] = useState<ParameterValues>({
    beta: 1.2,
    driftPercent: 0.04,
    ownVariability: 0.105,
    distancePenalty: 0.25
  });
  const [adoptionRevision, setAdoptionRevision] = useState(0);
  const [adoptionConfirmed, setAdoptionConfirmed] = useState(false);
  const [adoptionBusy, setAdoptionBusy] = useState(false);
  const [adoptionMessage, setAdoptionMessage] = useState('');

  function updateParameters(update: Partial<ParameterValues>) {
    setParameters(current => ({ ...current, ...update }));
    setBacktestError('');
    setBacktest(null);
    setAdoptionConfirmed(false);
    setAdoptionMessage('');
  }

  useEffect(() => {
    let active = true;
    if (offline) {
      setSelectionLoading(false);
      return () => { active = false; };
    }
    async function loadBacktestOptions() {
      try {
        const [listedSnapshots, listedMatches, adoption] = await Promise.all([
          freezerApi.getSnapshots(),
          freezerApi.getMatches(),
          freezerApi.getLatestParameterAdoption().catch(cause => {
            if (cause instanceof FreezerApiError && cause.status === 404) return null;
            throw cause;
          })
        ]);
        if (!active) return;
        const availableSnapshots = listedSnapshots;
        setSnapshots(availableSnapshots);
        setMatches(listedMatches);
        if (adoption) {
          setParameters(adoption.parameters);
          setAdoptionRevision(adoption.revision);
        }
        const matchById = new Map(listedMatches.map(item => [item.id, item]));
        if (data?.match) matchById.set(data.match.id, data.match);
        setSelectedSnapshotIds(availableSnapshots
          .filter(item => {
            const match = matchById.get(item.matchId);
            return match?.isFinal && match.canVerify && match.activeRevisionId !== item.matchRevisionId;
          })
          .filter(item => item.id === data?.snapshot.id)
          .map(item => item.id));
      } catch (cause) {
        if (active) setSelectionError(errorMessage(cause));
      } finally {
        if (active) setSelectionLoading(false);
      }
    }
    void loadBacktestOptions();
    return () => { active = false; };
  }, [data?.snapshot.id, data?.match, offline]);

  async function verify() {
    if (offline || !data?.match?.isFinal || !data.match.canVerify) return;
    setError('');
    setBusy(true);
    try {
      setReport(await freezerApi.verifySnapshot(data.snapshot.id, data.match.activeRevisionId));
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function runBacktest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (offline || !selectedEvaluations.length) return;
    setBacktestError('');
    setAdoptionMessage('');
    setBacktestBusy(true);
    setBacktest(null);
    setBacktestError('');
    setAdoptionConfirmed(false);
    try {
      setBacktest(await freezerApi.backtestParameters(parameters, selectedEvaluations));
      setAdoptionConfirmed(false);
    } catch (cause) {
      setBacktestError(errorMessage(cause));
    } finally {
      setBacktestBusy(false);
    }
  }

  async function adoptBacktest() {
    if (offline || !backtest || !canAdoptBacktest || !adoptionConfirmed) return;
    setBacktestError('');
    setAdoptionMessage('');
    setAdoptionBusy(true);
    try {
      const adoption: ParameterAdoption = await freezerApi.adoptParameters(
        backtest.id,
        adoptionRevision,
        adoptionConfirmed
      );
      setAdoptionRevision(adoption.revision);
      setParameters(adoption.parameters);
      setAdoptionMessage(`Parametri adottati alla revisione ${adoption.revision}. Si applicheranno alle nuove bozze; i piani già creati non cambiano.`);
    } catch (cause) {
      setBacktestError(errorMessage(cause));
    } finally {
      setAdoptionBusy(false);
    }
  }

  if (!data) {
    return <section className="form-panel"><h1>Verifica non disponibile.</h1><p className="lede">Accedi per confrontare un piano congelato con i risultati finali.</p></section>;
  }

  const match = data.match;
  const matchById = new Map(matches.map(item => [item.id, item]));
  if (match) matchById.set(match.id, match);
  const eligibleSnapshots = snapshots.filter(item => {
    const candidateMatch = matchById.get(item.matchId);
    return candidateMatch?.isFinal &&
      candidateMatch.canVerify &&
      candidateMatch.activeRevisionId !== item.matchRevisionId;
  });
  const selectedEvaluations = eligibleSnapshots
    .filter(item => selectedSnapshotIds.includes(item.id))
    .map(item => ({
      snapshotId: item.id,
      finalRevisionId: matchById.get(item.matchId)!.activeRevisionId
    }));
  const canAdoptBacktest = Boolean(backtest &&
    backtest.engineVersion &&
    backtest.evaluations.length > 0 &&
    backtest.evaluations.every(item => item.status === 'evaluated' && item.verifiableStageCount > 0) &&
    backtest.verifiableStageCount > 0);
  const numberFormat = new Intl.NumberFormat('it-IT', { maximumFractionDigits: 4 });
  const percent = (value: number | null) => value === null ? '—' : `${numberFormat.format(value * 100)}%`;
  const intervalStatus = (value: boolean | null) =>
    value === null ? 'Non verificabile' : value ? 'Dentro l’intervallo' : 'Fuori intervallo';
  const stageGroupName = (value: string) =>
    value === 'short' ? 'Brevi' : value === 'medium' ? 'Medie' : value === 'long' ? 'Lunghe' : value;
  const stageGroupingConvention = report?.stageGroupingConvention === 'minimum rounds: short=1-12, medium=13-24, long=25+'
    ? 'round minimi: brevi 1–12, medie 13–24, lunghe 25 o più'
    : report?.stageGroupingConvention || 'non disponibile';

  return (
    <div className="content-grid verification-page">
      <Link to={`/field/${encodeURIComponent(snapshotId ?? data.snapshot.id)}`} className="back-link">Piano congelato</Link>
      <section className="section-heading">
        <div><h1>Verifica finale</h1><p className="lede">Il confronto è separato dal piano congelato e non modifica stime o note.</p></div>
      </section>
      <section className="panel verification-panel">
        <h2>{match?.name ?? 'Gara non disponibile'}</h2>
        <p className="quiet-copy">Piano congelato il {formatDate(data.snapshot.createdAtUtc)} · {data.snapshot.stages.length} stage</p>
        {!match && <p className="notice notice-warning">Non è disponibile una revisione gara associata a questo piano.</p>}
        {match && !match.isFinal && <p className="notice notice-warning">La revisione corrente non è finale. Pubblica i risultati ufficiali finali prima della verifica.</p>}
        {match?.isFinal && !match.canVerify && <p className="notice notice-warning">La revisione finale è condivisa, ma non risulta pubblicata dal tuo account. La verifica è consentita solo sulla revisione finale di cui possiedi l’import.</p>}
        {match?.isFinal && match.canVerify && !report && (
          <>
            <p className="quiet-copy">Revisione ufficiale finale {match.revisionNumber} pronta per il confronto.</p>
            <button type="button" className="button button-primary" disabled={busy || offline} onClick={() => void verify()}>
              {busy ? 'Confronto…' : 'Calcola verifica'}
            </button>
          </>
        )}
        {error && <ErrorMessage>{error}</ErrorMessage>}
      </section>
      {report && <section className="panel verification-report" aria-live="polite">
        <div className="panel-heading"><div><h2>Risultato della verifica</h2><p className="quiet-copy">Report {report.id} · revisione finale associata al piano.</p></div><span className="status status-final">Salvato</span></div>
        <dl className="verification-stats">
          <div><dt>Stage verificabili</dt><dd>{report.verifiableStageCount}</dd></div>
          <div><dt>Stage mancanti</dt><dd>{report.missingStageCount}</dd></div>
          <div><dt>Stage DQ</dt><dd>{report.disqualifiedStageCount}</dd></div>
          <div><dt>Stage azzerati</dt><dd>{report.zeroedStageCount}</dd></div>
          <div><dt>Errore relativo medio</dt><dd>{percent(report.meanRelativeError)}</dd></div>
          <div><dt>Dispersione errore</dt><dd>{percent(report.errorSampleStandardDeviation)}</dd></div>
          <div><dt>Copertura intervallo</dt><dd>{report.intervalCoverageCount} / {report.verifiableStageCount}</dd></div>
          <div><dt>Beta osservato</dt><dd>{report.observedBeta === null ? '—' : numberFormat.format(report.observedBeta)}</dd></div>
          <div><dt>Confronti informatore-stage</dt><dd>{report.informantDriftComparisonCount}</dd></div>
          <div><dt>Confronti informatore-stage mancanti</dt><dd>{report.informantDriftMissingStageCount}</dd></div>
          <div><dt>Confronti informatore-stage DQ</dt><dd>{report.informantDriftDisqualifiedStageCount}</dd></div>
          <div><dt>Confronti informatore-stage azzerati</dt><dd>{report.informantDriftZeroedStageCount}</dd></div>
          <div><dt>Confronti informatore-stage non verificabili</dt><dd>{report.informantDriftUnverifiableStageCount}</dd></div>
          <div><dt>Errore medio degli informatori</dt><dd>{percent(report.informantDriftMeanRelativeError)}</dd></div>
          <div><dt>Dispersione errore informatori</dt><dd>{percent(report.informantDriftErrorSampleStandardDeviation)}</dd></div>
        </dl>
        {report.informantDriftComparisonCount === 0 && <p className="quiet-copy">Confronto con gli informatori non disponibile per questa verifica.</p>}
        {report.diagnostics.length > 0 && <ul className="diagnostic-list">{report.diagnostics.map((item, index) => <li key={`${index}-${item}`}>{item}</li>)}</ul>}
        <section className="verification-groups" aria-labelledby="verification-groups-heading">
          <div>
            <h3 id="verification-groups-heading">Risultati per durata stage</h3>
            <p className="quiet-copy">Criterio: {stageGroupingConvention}.</p>
          </div>
          {report.groups.length === 0
            ? <p className="quiet-copy">Non ci sono stage raggruppabili in questa verifica.</p>
            : <div className="pdf-table-scroll">
              <table>
                <thead><tr><th>Gruppo</th><th>Stage</th><th>Verificabili</th><th>Mancanti</th><th>DQ</th><th>Azzerati</th><th>Non verificabili</th><th>Errore medio</th><th>Dispersione</th><th>Copertura intervallo</th></tr></thead>
                <tbody>{report.groups.map(group => (
                  <tr key={group.stageGroup}>
                    <th scope="row">{stageGroupName(group.stageGroup)}</th>
                    <td>{group.stageCount}</td>
                    <td>{group.verifiableStageCount}</td>
                    <td>{group.missingStageCount}</td>
                    <td>{group.disqualifiedStageCount}</td>
                    <td>{group.zeroedStageCount}</td>
                    <td>{group.unverifiableStageCount}</td>
                    <td>{percent(group.meanRelativeError)}</td>
                    <td>{percent(group.errorSampleStandardDeviation)}</td>
                    <td>{group.intervalCoverageCount} / {group.verifiableStageCount} ({percent(group.intervalCoverageRate)})</td>
                  </tr>
                ))}</tbody>
              </table>
            </div>}
        </section>
        <div className="pdf-table-scroll">
          <table>
            <thead><tr><th>Stage</th><th>Gruppo</th><th>Esito</th><th>HF reale</th><th>Stima</th><th>Intervallo</th><th>Errore relativo</th><th>Copertura</th></tr></thead>
            <tbody>{report.stages.map(stage => (
              <tr key={stage.stageId}>
                <td>{stage.stageNumber}</td><td>{stage.stageGroup ? stageGroupName(stage.stageGroup) : 'Non raggruppato'}</td><td>{stage.status}</td>
                <td>{stage.actualHitFactor === null ? '—' : numberFormat.format(stage.actualHitFactor)}</td>
                <td>{stage.forecastCenter === null ? '—' : numberFormat.format(stage.forecastCenter)}</td>
                <td>{stage.forecastLower === null || stage.forecastUpper === null ? '—' : `${numberFormat.format(stage.forecastLower)}–${numberFormat.format(stage.forecastUpper)}`}</td>
                <td>{percent(stage.relativeError)}</td><td>{intervalStatus(stage.isWithinPredictionInterval)}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      </section>}
      <section className="panel parameter-backtest">
        <div className="panel-heading">
          <div><h2>Confronta parametri</h2><p className="quiet-copy">Ogni stima usa solo gli input congelati di quel piano e la revisione finale selezionata.</p></div>
          <span className="status">{selectedEvaluations.length} gare</span>
        </div>
        {offline && <p className="notice notice-warning">Verifica, backtest e adozione richiedono una connessione attiva. Le stime e i piani congelati restano invariati.</p>}
        {selectionLoading && <p className="quiet-copy" role="status">Caricamento dei piani finali disponibili…</p>}
        {selectionError && <ErrorMessage>{selectionError}</ErrorMessage>}
        {!offline && !selectionLoading && !selectionError && eligibleSnapshots.length === 0 &&
          <p className="notice notice-info">Non ci sono piani con risultati finali pubblicati dal tuo account e distinti dalla revisione congelata.</p>}
        {!offline && !selectionLoading && eligibleSnapshots.length > 0 && (
          <form className="form-stack" onSubmit={event => void runBacktest(event)}>
            <fieldset className="backtest-selection">
              <legend>Piani da confrontare</legend>
              {eligibleSnapshots.map(item => {
                const candidateMatch = matchById.get(item.matchId)!;
                return (
                  <label className="checkbox-label backtest-option" key={item.id}>
                    <input
                      type="checkbox"
                      checked={selectedSnapshotIds.includes(item.id)}
                      onChange={event => {
                        const checked = event.currentTarget.checked;
                        setSelectedSnapshotIds(current => checked
                          ? [...current, item.id]
                          : current.filter(id => id !== item.id));
                        setBacktest(null);
                        setAdoptionConfirmed(false);
                        setAdoptionMessage('');
                      }}
                    />
                    <span><strong>{candidateMatch.name}</strong><small>Piano {formatDate(item.createdAtUtc)} · revisione finale {candidateMatch.revisionNumber}</small></span>
                  </label>
                );
              })}
            </fieldset>
            <div className="form-columns parameter-fields">
              <label>Beta<input type="number" min="0.1" max="5" step="0.01" value={parameters.beta} onChange={event => updateParameters({ beta: Number(event.currentTarget.value) })} required /></label>
              <label>Deriva (0,04 = 4%)<input type="number" min="0" max="0.5" step="0.005" value={parameters.driftPercent} onChange={event => updateParameters({ driftPercent: Number(event.currentTarget.value) })} required /></label>
              <label>Variabilità personale<input type="number" min="0.001" max="1" step="0.001" value={parameters.ownVariability} onChange={event => updateParameters({ ownVariability: Number(event.currentTarget.value) })} required /></label>
              <label>Penalità distanza<input type="number" min="0" max="2" step="0.01" value={parameters.distancePenalty} onChange={event => updateParameters({ distancePenalty: Number(event.currentTarget.value) })} required /></label>
            </div>
            <button type="submit" className="button button-primary" disabled={backtestBusy || selectedEvaluations.length === 0}>
              {backtestBusy ? 'Confronto…' : 'Esegui backtest'}
            </button>
          </form>
        )}
        {backtestError && <ErrorMessage>{backtestError}</ErrorMessage>}
        {adoptionMessage && <p className="notice notice-success" role="status">{adoptionMessage}</p>}
        {backtest && <div className="parameter-backtest-report" aria-live="polite">
          <h3>Esito del backtest</h3>
          <p className="quiet-copy">Parametri testati: beta {numberFormat.format(backtest.parameters.beta)} · deriva {percent(backtest.parameters.driftPercent)} · variabilità {numberFormat.format(backtest.parameters.ownVariability)} · distanza {numberFormat.format(backtest.parameters.distancePenalty)} · versione {backtest.parameterVersion}</p>
          <dl className="verification-stats">
            <div><dt>Stage verificabili</dt><dd>{backtest.verifiableStageCount}</dd></div>
            <div><dt>Errore relativo medio</dt><dd>{percent(backtest.meanRelativeError)}</dd></div>
            <div><dt>Dispersione errore</dt><dd>{percent(backtest.errorSampleStandardDeviation)}</dd></div>
            <div><dt>Copertura intervallo</dt><dd>{backtest.intervalCoverageCount} / {backtest.verifiableStageCount}</dd></div>
          </dl>
          {backtest.diagnostics.length > 0 && <ul className="diagnostic-list">{backtest.diagnostics.map((item, index) => <li key={`${index}-${item}`}>{item}</li>)}</ul>}
          {backtest.evaluations.map(evaluation => (
            <details className="backtest-evaluation" key={`${evaluation.snapshotId}-${evaluation.finalRevisionId}`}>
              <summary>{matchById.get(evaluation.matchId)?.name ?? evaluation.matchId} · {evaluation.verifiableStageCount} stage verificabili · {evaluation.status}</summary>
              {evaluation.diagnostics.length > 0 && <ul className="diagnostic-list">{evaluation.diagnostics.map((item, index) => <li key={`${index}-${item}`}>{item}</li>)}</ul>}
              <div className="pdf-table-scroll">
                <table><thead><tr><th>Stage</th><th>Esito</th><th>HF reale</th><th>Stima candidata</th><th>Errore relativo</th></tr></thead>
                  <tbody>{evaluation.stages.map(stage => <tr key={stage.stageId}>
                    <td>{stage.stageNumber}</td><td>{stage.status}</td>
                    <td>{stage.actualHitFactor === null ? '—' : numberFormat.format(stage.actualHitFactor)}</td>
                    <td>{stage.forecastCenter === null ? '—' : numberFormat.format(stage.forecastCenter)}</td>
                    <td>{percent(stage.relativeError)}</td>
                  </tr>)}</tbody>
                </table>
              </div>
            </details>
          ))}
          <div className="adoption-confirmation">
            <label className="checkbox-label"><input type="checkbox" checked={adoptionConfirmed} onChange={event => setAdoptionConfirmed(event.currentTarget.checked)} /> Confermo di voler usare questi parametri nelle nuove bozze del mio account.</label>
            <button type="button" className="button button-secondary" disabled={!canAdoptBacktest || !adoptionConfirmed || adoptionBusy} onClick={() => void adoptBacktest()}>
              {adoptionBusy ? 'Salvataggio…' : 'Adotta per le nuove bozze'}
            </button>
            {!canAdoptBacktest && <p className="quiet-copy">L’adozione richiede risultati verificabili per tutti i piani selezionati.</p>}
          </div>
        </div>}
      </section>
    </div>
  );
}

export function FieldPage() {
  const { snapshot, events: initialEvents, offline: loaderOffline } = useLoaderData() as FieldLoaderData;
  const { snapshotId } = useParams();
  const { session, offline: accountOffline } = rootData();
  const accountId = session.account?.id;
  const [events, setEvents] = useState(initialEvents);
  const [pendingCount, setPendingCount] = useState(0);
  const [isOffline, setIsOffline] = useState(loaderOffline || accountOffline);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [eventKind, setEventKind] = useState<FieldEventInput['kind']>('reconnaissance');
  const [downloading, setDownloading] = useState(false);
  const [replaying, setReplaying] = useState(false);
  const [replay, setReplay] = useState<SnapshotReplay | null>(null);

  useEffect(() => {
    void refreshPending()
      .then(async () => {
        if (!accountId || !snapshotId) return;
        const queued = await queuedFieldEvents(accountId);
        const localEvents: FieldEvent[] = queued
          .filter(item => item.snapshotId === snapshotId)
          .map(item => ({
            id: `queued:${item.event.clientOperationId}`,
            snapshotId,
            sequence: 0,
            kind: item.event.kind,
            content: item.event.content,
            serverReceivedAtUtc: '',
            observedAtLocal: item.event.observedAtLocal,
            observedOffsetMinutes: item.event.observedOffsetMinutes,
            startStage: item.event.startStage ?? null,
            startTurn: item.event.startTurn ?? null,
            squadSize: item.event.squadSize ?? null,
            displayOrder: item.event.displayOrder ?? null
          }));
        setEvents(previous => [
          ...previous.filter(item => !item.id.startsWith('queued:')),
          ...localEvents
        ]);
        if (!loaderOffline && !accountOffline && window.navigator.onLine && queued.length > 0)
          void synchronize();
      })
      .catch(cause => setError(errorMessage(cause)));
    const handleOnline = () => {
      setIsOffline(false);
      void synchronize();
    };
    window.addEventListener('online', handleOnline);
    return () => window.removeEventListener('online', handleOnline);
  }, [accountId, snapshotId, loaderOffline, accountOffline]);

  async function refreshPending() {
    if (!accountId) return;
    setPendingCount((await queuedFieldEvents(accountId)).length);
  }

  async function submitEvent(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    if (!accountId || !snapshotId) {
      setError('Account o piano non disponibile.');
      return;
    }
    setError('');
    setStatus('');
    let payload: FieldEventInput | null = null;
    try {
      const form = new FormData(formElement);
      const now = new Date();
      const startStage = optionalPositiveInteger(form.get('startStage'));
      const startTurn = optionalPositiveInteger(form.get('startTurn'));
      const squadSize = optionalPositiveInteger(form.get('squadSize'));
      const displayOrderInput = String(form.get('displayOrder') ?? '')
        .split(',')
        .map(value => value.trim())
        .filter(Boolean);
      const displayOrder = displayOrderInput.map(value => {
        const stageNumber = Number(value);
        const stage = snapshot.stages.find(item => item.stageNumber === stageNumber);
        if (!Number.isInteger(stageNumber) || !stage) {
          throw new Error('L’ordine deve contenere numeri di stage presenti nel piano.');
        }
        return stage.stageId;
      });
      payload = {
        clientOperationId: newOperationId(),
        kind: parseEventKind(form.get('kind')),
        content: String(form.get('content') ?? ''),
        observedAtLocal: localDateTime(now),
        observedOffsetMinutes: -now.getTimezoneOffset(),
        ...(form.get('kind') === 'turn' ? { startStage, startTurn, squadSize, displayOrder } : {})
      };
      const saved = await freezerApi.appendFieldEvent(snapshotId, payload);
      setEvents(previous => [...previous, saved]);
      setStatus('Nota sincronizzata.');
      formElement.reset();
    } catch (cause) {
      if (!(cause instanceof TypeError) || !payload) {
        setError(errorMessage(cause));
        return;
      }
      try {
        await enqueueFieldEvent(accountId, snapshotId, payload);
        setPendingCount(count => count + 1);
        setIsOffline(true);
        setStatus('Nota salvata sul dispositivo; verrà sincronizzata al ritorno della connessione.');
        formElement.reset();
      } catch (storageError) {
        setError(errorMessage(storageError));
      }
    }
  }

  async function synchronize() {
    if (!accountId) return;
    setError('');
    let queued: Awaited<ReturnType<typeof queuedFieldEvents>>;
    try {
      const session = await freezerApi.getSession();
      if (!session.authenticated || !session.account) {
        setError('Accedi di nuovo con l’account che ha salvato le note; la coda locale è conservata.');
        return;
      }
      if (session.account.id !== accountId) {
        await clearAccountOfflineData(accountId);
        setPendingCount(0);
        setError('La sessione attiva non corrisponde all’account che ha salvato le note. Accedi con l’account corretto prima di sincronizzare.');
        return;
      }
      queued = await queuedFieldEvents(accountId);
    } catch (cause) {
      setError(errorMessage(cause));
      return;
    }
    const verifiedSnapshots = new Set<string>();
    for (const item of queued) {
      try {
        if (!verifiedSnapshots.has(item.snapshotId)) {
          await freezerApi.getSnapshot(item.snapshotId);
          verifiedSnapshots.add(item.snapshotId);
        }
        const saved = await freezerApi.appendFieldEvent(item.snapshotId, item.event);
        await removeQueuedFieldEvent(accountId, item.event.clientOperationId);
        if (item.snapshotId === snapshotId) {
          setEvents(previous => [
            ...previous.filter(existing => existing.id !== `queued:${item.event.clientOperationId}`),
            ...(previous.some(existing => existing.id === saved.id) ? [] : [saved])
          ]);
        }
      } catch (cause) {
        setError(errorMessage(cause));
        return;
      }
    }
    setPendingCount(0);
    setIsOffline(false);
    setStatus('Tutte le note in coda sono sincronizzate.');
  }

  async function refreshOfflineSnapshot() {
    if (!accountId || !snapshotId) return;
    const cached = await getSnapshot(accountId, snapshotId);
    if (!cached) {
      setError('Non è disponibile una copia locale di questo piano.');
      return;
    }
    setStatus('Piano locale verificato.');
  }

  async function downloadDocuments() {
    if (!snapshotId) return;
    setDownloading(true);
    setError('');
    try {
      const archive = await freezerApi.createArtifactPackage(snapshotId);
      const objectUrl = URL.createObjectURL(archive);
      const anchor = document.createElement('a');
      anchor.href = objectUrl;
      anchor.download = `freezer-card-${snapshotId}.zip`;
      anchor.click();
      URL.revokeObjectURL(objectUrl);
      setStatus('Pacchetto dei documenti scaricato.');
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setDownloading(false);
    }
  }

  async function replaySnapshot() {
    if (!snapshotId) return;
    setError('');
    setReplay(null);
    setReplaying(true);
    try {
      setReplay(await freezerApi.replaySnapshot(snapshotId));
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setReplaying(false);
    }
  }

  return (
    <div className="content-grid field-page">
      <Link to="/" className="back-link">Gare</Link>
      <section className="section-heading">
        <div><h1>Il tuo piano</h1><p className="quiet-copy">Congelato il {formatDate(snapshot.createdAtUtc)}</p></div>
        <span className={isOffline ? 'status status-warning' : 'status status-final'}>{isOffline ? 'Disponibile offline' : 'Piano immutabile'}</span>
      </section>
      <div className="notice notice-info">
        <strong>Revisione {snapshot.parameterVersion} · modello {snapshot.engineVersion}</strong>
        <p>Questo piano è una fotografia immutabile. Le note di campo vengono aggiunte come eventi separati e ordinati.</p>
        {pendingCount > 0 && <p>{pendingCount} {pendingCount === 1 ? 'nota in attesa' : 'note in attesa'} di sincronizzazione.</p>}
        {!isOffline && <Link className="button button-secondary" to={`/field/${encodeURIComponent(snapshot.id)}/verification`}>Verifica risultati finali</Link>}
        {!isOffline && <button className="button button-secondary" disabled={replaying} onClick={() => void replaySnapshot()}>{replaying ? 'Verifica replay…' : 'Verifica riproducibilità'}</button>}
        {isOffline && <button className="button button-secondary" onClick={() => void refreshOfflineSnapshot()}>Verifica copia locale</button>}
        {!isOffline && pendingCount > 0 && <button className="button button-secondary" onClick={() => void synchronize()}>Sincronizza note</button>}
        {!isOffline && <button className="button button-secondary" disabled={downloading} onClick={() => void downloadDocuments()}>{downloading ? 'Preparazione…' : 'Scarica documenti gara'}</button>}
      </div>
      {isOffline && <p className="notice notice-warning" role="status">Le copie e le note locali non sono cifrate come protezione per un dispositivo condiviso. La disconnessione o il logout remoto non possono cancellare questo dispositivo; quando la connessione torna, accedi e sincronizza, quindi esci per rimuovere i dati locali.</p>}
      {error && <ErrorMessage>{error}</ErrorMessage>}
      {status && <p className="notice notice-success" role="status">{status}</p>}
      <section className="field-stage-list" aria-label="Stime congelate">
        {snapshot.stages.map(stage => (
          <article className="field-stage" key={stage.stageId}>
            <div className="field-stage-index">{String(stage.stageNumber).padStart(2, '0')}</div>
            <div><span className="stage-meta">Stage · {stage.minimumRounds} colpi minimi</span><h2>{stage.center === null ? 'Stima non disponibile' : stage.center.toFixed(2)}</h2>
              <p>Intervallo {stage.lower?.toFixed(2) ?? '—'}–{stage.upper?.toFixed(2) ?? '—'}{stage.rhythmSymbol ? ` · ritmo ${stage.rhythmSymbol}` : ''}</p>
            </div>
          </article>
        ))}
      </section>
      {replay && <section className="panel replay-report" aria-live="polite">
        <div className="panel-heading">
          <div>
            <h2>Riproducibilità del piano</h2>
            <p className="quiet-copy">Modello {replay.engineVersion} · il replay usa solo gli input congelati.</p>
          </div>
          <span className={replay.matchesFrozenForecast ? 'status status-final' : 'status status-warning'}>
            {!replay.isReplayAvailable
              ? 'Replay non disponibile'
              : replay.matchesFrozenForecast ? 'Forecast coincidente' : 'Differenza rilevata'}
          </span>
        </div>
        {replay.diagnostics.length > 0 && <ul className="diagnostic-list">{replay.diagnostics.map((item, index) => <li key={`${index}-${item}`}>{item}</li>)}</ul>}
        {replay.stages.length > 0 && <div className="pdf-table-scroll">
          <table>
            <thead><tr><th>Stage</th><th>Stima congelata</th><th>Stima replay</th><th>Intervallo congelato</th><th>Intervallo replay</th><th>Confronto</th></tr></thead>
            <tbody>{replay.stages.map(stage => {
              const format = (value: number | null) => value === null ? '—' : value.toLocaleString('it-IT', { maximumFractionDigits: 4 });
              return <tr key={stage.stageId}>
                <td>{stage.stageNumber}</td>
                <td>{format(stage.frozenCenter)}</td>
                <td>{format(stage.replayedCenter)}</td>
                <td>{format(stage.frozenLower)}–{format(stage.frozenUpper)}</td>
                <td>{format(stage.replayedLower)}–{format(stage.replayedUpper)}</td>
                <td>{stage.matchesFrozenForecast ? 'Coincide' : 'Differente'}</td>
              </tr>;
            })}</tbody>
          </table>
        </div>}
      </section>}
      <section className="panel field-notes">
        <div className="panel-heading"><div><h2>Aggiungi una nota di gara</h2></div></div>
        <form className="form-stack" onSubmit={event => void submitEvent(event)}>
          <label>Tipo di nota<select name="kind" value={eventKind} onChange={event => setEventKind(parseEventKind(event.currentTarget.value))}><option value="reconnaissance">Ricognizione</option><option value="turn">Turno / ordine</option><option value="midday">Aggiornamento di metà giornata</option><option value="correction">Correzione</option></select></label>
          {eventKind === 'turn' && <fieldset className="turn-fields">
            <legend>Contesto di partenza</legend>
            <div className="form-columns">
              <label>Stage di partenza<input name="startStage" type="number" min="1" step="1" /></label>
              <label>Numero turno<input name="startTurn" type="number" min="1" step="1" /></label>
              <label>Dimensione squadra<input name="squadSize" type="number" min="1" step="1" /></label>
            </div>
            <label>Ordine stage<input name="displayOrder" placeholder="Numeri stage, separati da virgola" /><span className="field-hint">Indica ogni stage una sola volta, nell’ordine desiderato.</span></label>
          </fieldset>}
          <label>Dettagli<textarea name="content" rows={4} maxLength={2000} required placeholder="Usa la dettatura della tastiera per aggiungere una nota a voce." /></label>
          <button className="button button-primary">Salva nota</button>
        </form>
        <ol className="event-list">
          {events.map(item => <li key={item.id}>
            <span>{item.kind} · {item.sequence > 0 ? `#${item.sequence}` : 'in attesa'}</span>
            {(item.startStage || item.startTurn || item.squadSize || item.displayOrder?.length) && <small className="event-context">
              {item.startStage && `Partenza stage ${item.startStage}`}
              {item.startTurn && ` · turno ${item.startTurn}`}
              {item.squadSize && ` · squadra ${item.squadSize}`}
              {item.displayOrder?.length && ` · ordine ${item.displayOrder.map(stageId => snapshot.stages.find(stage => stage.stageId === stageId)?.stageNumber ?? stageId).join(', ')}`}
            </small>}
            <p>{item.content}</p>
          </li>)}
        </ol>
      </section>
    </div>
  );
}

function errorMessage(error: unknown): string {
  if (error instanceof FreezerApiError) return error.message;
  if (error instanceof Error) return error.message;
  return 'Si è verificato un errore inatteso. Riprova.';
}

function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat('it-IT', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

function newOperationId(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID();
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, value => value.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

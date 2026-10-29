import 'fake-indexeddb/auto';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router-dom';
import {
  FreezerApiError,
  freezerApi,
  type FieldEvent,
  type FieldEventInput,
  type IdentityReviewEntry,
  type MatchSummary,
  type ParameterBacktestReport,
  type PdfPairPreview,
  type Snapshot,
  type SnapshotSelection,
  type SnapshotReplay,
  type VerificationReport
} from './freezerApi';
import { FieldPage, IdentityReviewPage, LoginPage, MatchesPage, RootLayout, VerificationPage } from './routes';
import { enqueueFieldEvent, getSnapshot, queuedFieldEvents, saveSnapshot } from './offlineStore';

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

function renderLoginPage() {
  const router = createMemoryRouter([
    {
      id: 'root',
      path: '/',
      loader: () => ({
        session: { authenticated: false, account: null },
        offline: false
      }),
      element: <RootLayout />,
      children: [{ path: 'login', element: <LoginPage /> }]
    }
  ], { initialEntries: ['/login'] });
  render(<RouterProvider router={router} />);
}

describe('account offline-data lifecycle', () => {
  it('clears the signed-in account cache before completing logout', async () => {
    const accountId = 'logout-account';
    const snapshot: Snapshot = {
      id: 'logout-snapshot',
      matchId: 'match-1',
      engineVersion: 'engine-v1',
      parameterVersion: 'parameters-v1',
      createdAtUtc: '2026-10-29T10:00:00Z',
      stages: []
    };
    await saveSnapshot(accountId, snapshot);
    const logout = vi.spyOn(freezerApi, 'logout').mockResolvedValue();
    const router = createMemoryRouter([{
      id: 'root',
      path: '/',
      loader: () => ({
        session: {
          authenticated: true,
          account: { id: accountId, username: 'runner', email: 'runner@example.test', displayName: 'Runner' }
        },
        offline: false
      }),
      element: <RootLayout />,
      children: [{ index: true, element: <div /> }]
    }]);
    render(<RouterProvider router={router} />);

    fireEvent.click(await screen.findByRole('button', { name: 'Esci' }));

    await waitFor(() => expect(logout).toHaveBeenCalledOnce());
    expect(await getSnapshot(accountId, snapshot.id)).toBeNull();
  });

  it('clears the previous account cache after switching accounts', async () => {
    const previousAccountId = 'previous-account';
    const nextAccountId = 'next-account';
    const snapshot: Snapshot = {
      id: 'account-switch-snapshot',
      matchId: 'match-1',
      engineVersion: 'engine-v1',
      parameterVersion: 'parameters-v1',
      createdAtUtc: '2026-10-29T10:00:00Z',
      stages: []
    };
    await saveSnapshot(previousAccountId, snapshot);
    await saveSnapshot(nextAccountId, { ...snapshot, id: 'next-account-snapshot' });
    sessionStorage.setItem('freezer-analysis.account-id', previousAccountId);
    vi.spyOn(freezerApi, 'login').mockResolvedValue({
      authenticated: true,
      account: {
        id: nextAccountId,
        username: 'next-runner',
        email: 'next@example.test',
        displayName: 'Next Runner'
      }
    });
    renderLoginPage();
    fireEvent.change(await screen.findByLabelText('Email o nome utente'), { target: { value: 'next-runner' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'password-valida' } });
    fireEvent.submit(screen.getByLabelText('Password').closest('form')!);

    await waitFor(async () => expect(await getSnapshot(previousAccountId, snapshot.id)).toBeNull());
    expect(await getSnapshot(nextAccountId, 'next-account-snapshot')).not.toBeNull();
  });
});

function renderIdentityReview(entry: IdentityReviewEntry) {
  const router = createMemoryRouter([{
    path: 'matches/:matchId/identities',
    loader: () => ({ entries: [entry], offline: false }),
    element: <IdentityReviewPage />
  }], { initialEntries: ['/matches/match-1/identities'] });
  render(<RouterProvider router={router} />);
}

function renderMatchesPage() {
  const router = createMemoryRouter([{
    path: '/matches',
    loader: () => [],
    element: <MatchesPage />
  }], { initialEntries: ['/matches'] });
  render(<RouterProvider router={router} />);
}

function renderVerificationPage(snapshot: Snapshot, match: MatchSummary | null) {
  const router = createMemoryRouter([{
    path: '/field/:snapshotId/verification',
    loader: () => ({ snapshot, match }),
    element: <VerificationPage />
  }], { initialEntries: [`/field/${snapshot.id}/verification`] });
  render(<RouterProvider router={router} />);
}

function renderFieldPage(snapshot: Snapshot, accountId?: string, offline = false) {
  const router = createMemoryRouter([{
    id: 'root',
    path: '/',
    loader: () => ({
      session: {
        authenticated: true,
        account: accountId
          ? { id: accountId, username: 'runner', email: 'runner@example.test', displayName: 'Runner' }
          : null
      },
      offline
    }),
    element: <Outlet />,
    children: [{
      path: 'field/:snapshotId',
      loader: () => ({ snapshot, events: [], offline }),
      element: <FieldPage />
    }]
  }], { initialEntries: [`/field/${snapshot.id}`] });
  render(<RouterProvider router={router} />);
}

describe('offline field reconciliation', () => {
  it('reauthenticates and verifies snapshot ownership before replaying queued events', async () => {
    const accountId = 'field-account';
    const snapshot: Snapshot = {
      id: 'field-snapshot',
      matchId: 'match-1',
      engineVersion: 'engine-v1',
      parameterVersion: 'parameters-v1',
      createdAtUtc: '2026-10-29T10:00:00Z',
      stages: []
    };
    const queuedEvent: FieldEventInput = {
      clientOperationId: 'queued-operation',
      kind: 'reconnaissance',
      content: 'Promemoria offline',
      observedAtLocal: '2026-10-29T12:00:00',
      observedOffsetMinutes: 120
    };
    const savedEvent: FieldEvent = {
      id: 'saved-event',
      snapshotId: snapshot.id,
      sequence: 1,
      kind: queuedEvent.kind,
      content: queuedEvent.content,
      serverReceivedAtUtc: '2026-10-29T10:01:00Z',
      observedAtLocal: queuedEvent.observedAtLocal,
      observedOffsetMinutes: queuedEvent.observedOffsetMinutes,
      startStage: null,
      startTurn: null,
      squadSize: null,
      displayOrder: null
    };
    await enqueueFieldEvent(accountId, snapshot.id, queuedEvent);
    const getSession = vi.spyOn(freezerApi, 'getSession').mockResolvedValue({
      authenticated: true,
      account: { id: accountId, username: 'runner', email: 'runner@example.test', displayName: 'Runner' }
    });
    const verifySnapshot = vi.spyOn(freezerApi, 'getSnapshot').mockResolvedValue(snapshot);
    const appendFieldEvent = vi.spyOn(freezerApi, 'appendFieldEvent').mockResolvedValue(savedEvent);
    renderFieldPage(snapshot, accountId, true);

    expect(await screen.findByText(/1 nota in attesa/)).toBeInTheDocument();
    fireEvent(window, new Event('online'));

    expect(await screen.findByText('Tutte le note in coda sono sincronizzate.')).toBeInTheDocument();
    expect(getSession).toHaveBeenCalledOnce();
    expect(verifySnapshot).toHaveBeenCalledWith(snapshot.id);
    expect(appendFieldEvent).toHaveBeenCalledWith(snapshot.id, queuedEvent);
    expect(await queuedFieldEvents(accountId)).toEqual([]);
  });
});

describe('account screen', () => {
  it('switches to registration and requires explicit terms and privacy acknowledgement', async () => {
    renderLoginPage();
    fireEvent.click(await screen.findByRole('button', { name: 'Registrati' }));

    expect(await screen.findByLabelText('Nome utente')).toBeRequired();
    expect(screen.getByLabelText('Email')).toBeRequired();
    expect(screen.getByLabelText(/Accetto i termini/)).toBeRequired();
    expect(screen.getByLabelText(/Ho letto l'informativa/)).toBeRequired();
  });

  it('surfaces the account request error without navigating away', async () => {
    vi.spyOn(freezerApi, 'login').mockRejectedValue(new Error('Credenziali non valide.'));
    renderLoginPage();
    fireEvent.change(await screen.findByLabelText('Email o nome utente'), { target: { value: 'tiratore' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'password-valida' } });
    fireEvent.submit(screen.getByLabelText('Password').closest('form')!);

    expect(await screen.findByRole('alert')).toHaveTextContent('Credenziali non valide.');
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Bentornato.' })).toBeInTheDocument());
  });
});

describe('PDF pair preview', () => {
  const preview: PdfPairPreview = {
    classificationPageCount: 1,
    classificationCompetitorCount: 1,
    disqualifiedCompetitorCount: 1,
    verificationPageCount: 1,
    verificationCompetitorCount: 1,
    verificationStageResultCount: 1,
    matchedCompetitorCount: 1,
    unmatchedClassificationCount: 0,
    unmatchedVerificationCount: 0,
    ambiguousNameDivisionGroupCount: 0,
    mappedDisqualifiedCompetitorCount: 0,
    unmatchedDisqualifiedCompetitorCount: 1,
    canPublish: false,
    diagnostics: [],
    classificationEntries: [{
      displayName: 'Atleta di prova',
      division: 'OPEN',
      rank: 1,
      percent: 100,
      points: 400,
      pageNumber: 1,
      pairingStatus: 'matched',
      isDisqualified: null
    }],
    verificationEntries: [{
      competitorNumber: '1',
      displayName: 'Atleta di prova',
      division: 'OPEN',
      powerFactor: 'MAJOR',
      pageNumber: 1,
      pairingStatus: 'matched',
      isDisqualified: null,
      stages: [{
        stageNumber: 1,
        hitFactor: 4.25,
        reportedPoints: 100,
        alpha: 20,
        charlie: 1,
        delta: 0,
        deduction: 0,
        misses: 0,
        noShoots: 0,
        procedurals: 0,
        overtimeValue: null,
        timeSeconds: 23.53,
        pageNumber: 1,
        hitFactorMatchesReportedPointsAndTime: true,
        diagnostics: []
      }]
    }],
    parsedVerificationStageResultCount: 1,
    hitFactorTimeMismatchCount: 0,
    disqualifiedEntries: [{
      displayName: 'Atleta non abbinato',
      division: 'OPEN',
      pageNumber: 2,
      pairingStatus: 'unmatched'
    }]
  };

  it('previews both PDF files and clearly keeps the result unpublished', async () => {
    const previewPdfPair = vi.spyOn(freezerApi, 'previewPdfPair').mockResolvedValue(preview);
    const publishScores = vi.spyOn(freezerApi, 'publishScores');
    renderMatchesPage();
    fireEvent.change(await screen.findByLabelText(/Classifica Overall/), {
      target: { files: [new File(['%PDF-test'], 'classifica.pdf', { type: 'application/pdf' })] }
    });
    fireEvent.change(screen.getByLabelText(/Verifica punteggi/), {
      target: { files: [new File(['%PDF-test'], 'verify.pdf', { type: 'application/pdf' })] }
    });
    expect(await screen.findByText('classifica.pdf')).toBeInTheDocument();
    expect(screen.getByText('verify.pdf')).toBeInTheDocument();
    fireEvent.submit(screen.getByRole('button', { name: 'Analizza coppia PDF' }).closest('form')!);

    expect(await screen.findByText('Anteprima non pubblicabile')).toBeInTheDocument();
    expect(await screen.findByText('Righe della classifica (1)')).toBeInTheDocument();
    expect(await screen.findByText('Voci dell’appendice DQ (1)')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Atleta non abbinato' })).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Non abbinata' })).toBeInTheDocument();
    expect(screen.getAllByText('Stato DQ non verificabile')).toHaveLength(1);
    fireEvent.click(screen.getByText('Sezioni Verify e dettagli stage (1)'));
    fireEvent.click(screen.getByText(/N° 1 · Abbinato/));
    expect(screen.getByText(/N° 1 · Abbinato · Stato DQ non verificabile/)).toBeInTheDocument();
    expect(screen.getByText('Voci DQ abbinate').parentElement).toHaveTextContent('0 / 1');
    expect(screen.getByText('Voci DQ non verificate').parentElement).toHaveTextContent('1');
    expect(previewPdfPair).toHaveBeenCalledOnce();
    const form = previewPdfPair.mock.calls[0][0];
    expect(form.has('classificationFile')).toBe(true);
    expect(form.has('verificationFile')).toBe(true);
    expect(publishScores).not.toHaveBeenCalled();
  });

  it('rejects non-PDF files before making a request', async () => {
    const previewPdfPair = vi.spyOn(freezerApi, 'previewPdfPair');
    renderMatchesPage();
    fireEvent.change(await screen.findByLabelText(/Classifica Overall/), {
      target: { files: [new File(['data'], 'classifica.csv', { type: 'text/csv' })] }
    });
    fireEvent.change(screen.getByLabelText(/Verifica punteggi/), {
      target: { files: [new File(['%PDF-test'], 'verify.pdf', { type: 'application/pdf' })] }
    });
    fireEvent.submit(screen.getByRole('button', { name: 'Analizza coppia PDF' }).closest('form')!);

    expect(await screen.findByRole('alert')).toHaveTextContent('estensione .pdf');
    expect(previewPdfPair).not.toHaveBeenCalled();
  });
});

describe('final verification screen', () => {
  const snapshot: Snapshot = {
    id: 'snapshot-1',
    matchId: 'match-1',
    engineVersion: 'engine-v1',
    parameterVersion: 'parameters-v1',
    createdAtUtc: '2026-10-28T10:00:00Z',
    stages: [{
      stageId: 'stage-1',
      stageNumber: 1,
      minimumRounds: 10,
      rhythmSymbol: null,
      center: 4.2,
      lower: 3.8,
      upper: 4.6,
      missCostSeconds: null,
      charlieCostSeconds: null,
      diagnostics: []
    }]
  };
  const match: MatchSummary = {
    id: 'match-1',
    activeRevisionId: 'final-revision-2',
    canVerify: true,
    name: 'Gara di prova',
    location: 'Campo',
    localMatchDate: '2026-10-29',
    timeZoneId: 'Europe/Rome',
    revisionNumber: 2,
    isFinal: true,
    stageCount: 1,
    competitorCount: 1
  };
  const report: VerificationReport = {
    id: 'verification-1',
    snapshotId: snapshot.id,
    finalRevisionId: match.activeRevisionId,
    verifiableStageCount: 1,
    missingStageCount: 0,
    disqualifiedStageCount: 0,
    zeroedStageCount: 0,
    meanRelativeError: 0.1,
    errorSampleStandardDeviation: 0,
    intervalCoverageCount: 1,
    observedBeta: 1.1,
    stageGroupingConvention: 'minimum rounds: short=1-12, medium=13-24, long=25+',
    informantDriftComparisonCount: 0,
    informantDriftMissingStageCount: 0,
    informantDriftDisqualifiedStageCount: 0,
    informantDriftZeroedStageCount: 0,
    informantDriftUnverifiableStageCount: 0,
    informantDriftMeanRelativeError: null,
    informantDriftErrorSampleStandardDeviation: null,
    groups: [{
      stageGroup: 'short',
      stageCount: 1,
      verifiableStageCount: 1,
      missingStageCount: 0,
      disqualifiedStageCount: 0,
      zeroedStageCount: 0,
      unverifiableStageCount: 0,
      meanRelativeError: 0.1,
      errorSampleStandardDeviation: null,
      intervalCoverageCount: 0,
      intervalCoverageRate: 0
    }],
    stages: [{
      stageId: 'stage-1',
      stageNumber: 1,
      stageGroup: 'short',
      status: 'scored',
      actualHitFactor: 4.62,
      forecastCenter: 4.2,
      forecastLower: 3.8,
      forecastUpper: 4.6,
      relativeError: 0.1,
      isWithinPredictionInterval: false,
      diagnostics: []
    }],
    diagnostics: []
  };

  const snapshots: SnapshotSelection[] = [
    {
      id: snapshot.id,
      matchId: snapshot.matchId,
      matchRevisionId: 'prestart-revision-1',
      engineVersion: snapshot.engineVersion,
      parameterVersion: snapshot.parameterVersion,
      createdAtUtc: snapshot.createdAtUtc
    },
    {
      id: 'snapshot-older',
      matchId: 'match-older',
      matchRevisionId: 'prestart-revision-older',
      engineVersion: snapshot.engineVersion,
      parameterVersion: snapshot.parameterVersion,
      createdAtUtc: '2026-09-10T10:00:00Z'
    }
  ];
  const olderMatch: MatchSummary = {
    ...match,
    id: 'match-older',
    activeRevisionId: 'final-revision-older',
    name: 'Gara precedente',
    localMatchDate: '2026-09-11'
  };

  beforeEach(() => {
    vi.spyOn(freezerApi, 'getSnapshots').mockResolvedValue(snapshots);
    vi.spyOn(freezerApi, 'getMatches').mockResolvedValue([match, olderMatch]);
    vi.spyOn(freezerApi, 'getLatestParameterAdoption').mockRejectedValue(new FreezerApiError('Nessuna adozione', 404));
  });

  it('runs verification only for an owned final revision and renders the persisted report', async () => {
    const verify = vi.spyOn(freezerApi, 'verifySnapshot').mockResolvedValue(report);
    renderVerificationPage(snapshot, match);
    fireEvent.click(await screen.findByRole('button', { name: 'Calcola verifica' }));

    expect(await screen.findByRole('heading', { name: 'Risultato della verifica' })).toBeInTheDocument();
    expect(verify).toHaveBeenCalledWith(snapshot.id, match.activeRevisionId);
    expect(screen.getByText('1 / 1')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Risultati per durata stage' })).toBeInTheDocument();
    expect(screen.getByText('Criterio: round minimi: brevi 1–12, medie 13–24, lunghe 25 o più.')).toBeInTheDocument();
    expect(screen.getByRole('rowheader', { name: 'Brevi' })).toBeInTheDocument();
    expect(screen.getByText('Confronti informatore-stage')).toBeInTheDocument();
    expect(screen.getByText('Confronti informatore-stage mancanti')).toBeInTheDocument();
    expect(screen.getByText('Confronto con gli informatori non disponibile per questa verifica.')).toBeInTheDocument();
    expect(screen.getByText('0 / 1 (0%)')).toBeInTheDocument();
    expect(screen.getAllByText('—').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: 'Pubblica revisione' })).not.toBeInTheDocument();
  });

  it('backtests selected frozen races and requires explicit confirmation before adoption', async () => {
    const backtest: ParameterBacktestReport = {
      id: 'backtest-1',
      engineVersion: 'engine-v1',
      parameterVersion: 'candidate-abc123',
      parameters: { beta: 1.2, driftPercent: 0.04, ownVariability: 0.105, distancePenalty: 0.25 },
      evaluationCount: 2,
      verifiableStageCount: 2,
      meanRelativeError: 0.03,
      errorSampleStandardDeviation: 0.01,
      intervalCoverageCount: 2,
      evaluations: [
        {
          snapshotId: snapshot.id,
          matchId: match.id,
          finalRevisionId: match.activeRevisionId,
          status: 'evaluated',
          verifiableStageCount: 1,
          missingStageCount: 0,
          disqualifiedStageCount: 0,
          zeroedStageCount: 0,
          meanRelativeError: 0.03,
          errorSampleStandardDeviation: 0,
          intervalCoverageCount: 1,
          stages: [{
            stageId: 'stage-1',
            stageNumber: 1,
            status: 'scored',
            actualHitFactor: 4.3,
            forecastCenter: 4.2,
            forecastLower: 3.8,
            forecastUpper: 4.6,
            relativeError: 0.03,
            isWithinPredictionInterval: true
          }],
          diagnostics: []
        },
        {
          snapshotId: 'snapshot-older',
          matchId: olderMatch.id,
          finalRevisionId: olderMatch.activeRevisionId,
          status: 'evaluated',
          verifiableStageCount: 1,
          missingStageCount: 0,
          disqualifiedStageCount: 0,
          zeroedStageCount: 0,
          meanRelativeError: 0.03,
          errorSampleStandardDeviation: 0,
          intervalCoverageCount: 1,
          stages: [],
          diagnostics: []
        }
      ],
      diagnostics: [],
      createdAtUtc: '2026-10-30T10:00:00Z'
    };
    const backtestParameters = vi.spyOn(freezerApi, 'backtestParameters').mockResolvedValue(backtest);
    const adoptParameters = vi.spyOn(freezerApi, 'adoptParameters').mockResolvedValue({
      revision: 1,
      backtestId: backtest.id,
      parameterVersion: backtest.parameterVersion,
      parameters: backtest.parameters,
      createdAtUtc: '2026-10-30T10:01:00Z'
    });
    renderVerificationPage(snapshot, match);

    const olderSnapshotCheckbox = await screen.findByRole('checkbox', { name: /Gara precedente/ });
    fireEvent.click(olderSnapshotCheckbox);
    await waitFor(() => expect(olderSnapshotCheckbox).toBeChecked());
    fireEvent.click(screen.getByRole('button', { name: 'Esegui backtest' }));

    expect(await screen.findByRole('heading', { name: 'Esito del backtest' })).toBeInTheDocument();
    expect(backtestParameters).toHaveBeenCalledWith(
      { beta: 1.2, driftPercent: 0.04, ownVariability: 0.105, distancePenalty: 0.25 },
      [
        { snapshotId: snapshot.id, finalRevisionId: match.activeRevisionId },
        { snapshotId: 'snapshot-older', finalRevisionId: olderMatch.activeRevisionId }
      ]
    );
    const adoptButton = screen.getByRole('button', { name: 'Adotta per le nuove bozze' });
    expect(adoptButton).toBeDisabled();
    fireEvent.click(screen.getByLabelText(/Confermo di voler usare questi parametri/));
    fireEvent.click(adoptButton);
    expect(await screen.findByText(/Si applicheranno alle nuove bozze/)).toBeInTheDocument();
    expect(adoptParameters).toHaveBeenCalledWith(backtest.id, 0, true);
  });

  describe('frozen snapshot replay', () => {
    it('shows the deterministic replay report separately from the frozen field plan', async () => {
      const snapshot: Snapshot = {
        id: 'snapshot-replay',
        matchId: 'match-1',
        engineVersion: 'engine-v1',
        parameterVersion: 'parameters-v1',
        createdAtUtc: '2026-10-29T10:00:00Z',
        stages: [{
          stageId: 'stage-1',
          stageNumber: 1,
          minimumRounds: 12,
          rhythmSymbol: null,
          center: 4.25,
          lower: 3.8,
          upper: 4.7,
          missCostSeconds: null,
          charlieCostSeconds: null,
          diagnostics: []
        }]
      };
      const replay: SnapshotReplay = {
        snapshotId: snapshot.id,
        engineVersion: snapshot.engineVersion,
        isReplayAvailable: true,
        matchesFrozenForecast: true,
        stages: [{
          stageId: 'stage-1',
          stageNumber: 1,
          frozenCenter: 4.25,
          replayedCenter: 4.25,
          frozenLower: 3.8,
          replayedLower: 3.8,
          frozenUpper: 4.7,
          replayedUpper: 4.7,
          matchesFrozenForecast: true
        }],
        diagnostics: []
      };
      const replaySnapshot = vi.spyOn(freezerApi, 'replaySnapshot').mockResolvedValue(replay);
      renderFieldPage(snapshot);

      fireEvent.click(await screen.findByRole('button', { name: 'Verifica riproducibilità' }));

      expect(await screen.findByText('Forecast coincidente')).toBeInTheDocument();
      expect(screen.getByText(/il replay usa solo gli input congelati/)).toBeInTheDocument();
      expect(screen.getByText('Coincide')).toBeInTheDocument();
      expect(replaySnapshot).toHaveBeenCalledWith(snapshot.id);
    });
  });

  it('withholds verification when the final revision is not owned by the account', async () => {
    const verify = vi.spyOn(freezerApi, 'verifySnapshot');
    renderVerificationPage(snapshot, { ...match, canVerify: false });

    expect(await screen.findByText(/non risulta pubblicata dal tuo account/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Calcola verifica' })).not.toBeInTheDocument();
    expect(verify).not.toHaveBeenCalled();
  });
});

describe('identity review screen', () => {
  const entry: IdentityReviewEntry = {
    resultId: 'result-1',
    competitorId: 'competitor-1',
    displayName: 'Runner Uno',
    division: 'PRODUCTION',
    identityId: null,
    resolutionRevision: 0,
    candidates: [{
      id: 'identity-1',
      normalizedName: 'RUNNERUNO',
      aliases: ['Runner Uno']
    }]
  };

  it('requires an explicit selection and reports successful mapping', async () => {
    const resolve = vi.spyOn(freezerApi, 'resolveIdentity').mockResolvedValue({
      resultId: entry.resultId,
      identityId: 'identity-1',
      revision: 1,
      resolvedAtUtc: '2026-10-29T10:00:00Z'
    });
    renderIdentityReview(entry);
    fireEvent.change(await screen.findByLabelText('Identità confermata'), { target: { value: 'identity-1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Conferma associazione' }));

    await waitFor(() => expect(resolve).toHaveBeenCalledWith('match-1', 'result-1', 'identity-1', 0));
    expect(await screen.findByRole('status')).toHaveTextContent('Associazione aggiornata per Runner Uno.');
  });

  it('creates a distinct identity and reports revision conflicts', async () => {
    vi.spyOn(freezerApi, 'createIdentity').mockResolvedValue({
      id: 'identity-new',
      normalizedName: 'RUNNERUNO',
      aliases: ['Runner Uno'],
      revision: 1
    });
    vi.spyOn(freezerApi, 'resolveIdentity').mockRejectedValue(new FreezerApiError('La revisione è cambiata.', 409));
    renderIdentityReview({ ...entry, candidates: [] });
    fireEvent.click(await screen.findByRole('button', { name: 'Crea identità distinta' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('La revisione è cambiata.');
    expect(freezerApi.resolveIdentity).toHaveBeenCalledWith('match-1', 'result-1', 'identity-new', 0);
  });
});

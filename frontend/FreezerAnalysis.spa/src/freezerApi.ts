import { createApiClient } from '@morwalpizvideo/services';

export interface AccountSummary {
  id: string;
  username: string;
  email: string;
  displayName: string;
}

export interface AccountSession {
  authenticated: boolean;
  account: AccountSummary | null;
}

export interface Profile {
  accountId: string;
  identityId: string | null;
  division: string | null;
  powerFactor: string | null;
  performance: number;
  updatedAtUtc: string;
}

export interface ShooterIdentity {
  id: string;
  normalizedName: string;
  aliases: string[];
  revision: number;
}

export interface IdentityCandidate {
  id: string;
  normalizedName: string;
  aliases: string[];
}

export interface IdentityReviewEntry {
  resultId: string;
  competitorId: string;
  displayName: string;
  division: string;
  identityId: string | null;
  resolutionRevision: number;
  candidates: IdentityCandidate[];
}

export interface IdentityResolution {
  resultId: string;
  identityId: string;
  revision: number;
  resolvedAtUtc: string;
}

export interface MatchSummary {
  id: string;
  activeRevisionId: string;
  canVerify: boolean;
  name: string;
  location: string | null;
  localMatchDate: string;
  timeZoneId: string;
  revisionNumber: number;
  isFinal: boolean;
  stageCount: number;
  competitorCount: number;
}

export interface ImportPreview {
  importId: string;
  matchName: string;
  localMatchDate: string;
  timeZoneId: string;
  format: string;
  completeness: string;
  stageCount: number;
  competitorCount: number;
  unresolvedIdentityCount: number;
  matchedResultCount: number;
  diagnostics: string[];
  canPublish: boolean;
  expectedRevision: number;
}

export interface PdfPairPreview {
  classificationPageCount: number;
  classificationCompetitorCount: number;
  disqualifiedCompetitorCount: number;
  verificationPageCount: number;
  verificationCompetitorCount: number;
  verificationStageResultCount: number;
  matchedCompetitorCount: number;
  unmatchedClassificationCount: number;
  unmatchedVerificationCount: number;
  ambiguousNameDivisionGroupCount: number;
  mappedDisqualifiedCompetitorCount?: number;
  unmatchedDisqualifiedCompetitorCount?: number;
  canPublish: boolean;
  diagnostics: string[];
  classificationEntries: PdfClassificationPreview[];
  verificationEntries: PdfVerificationPreview[];
  parsedVerificationStageResultCount: number;
  hitFactorTimeMismatchCount: number;
  disqualifiedEntries?: PdfDisqualificationPreview[] | null;
}

export interface PdfDisqualificationPreview {
  displayName: string;
  division: string;
  pageNumber: number;
  pairingStatus: string;
}

export interface PdfClassificationPreview {
  displayName: string;
  division: string;
  rank: number;
  percent: number;
  points: number;
  pageNumber: number;
  pairingStatus: string;
  isDisqualified?: boolean | null;
}

export interface PdfVerificationPreview {
  competitorNumber: string;
  displayName: string;
  division: string;
  powerFactor: string | null;
  pageNumber: number;
  pairingStatus: string;
  isDisqualified?: boolean | null;
  stages: PdfStagePreview[];
}

export interface PdfStagePreview {
  stageNumber: number;
  hitFactor: number | null;
  reportedPoints: number | null;
  alpha: number | null;
  charlie: number | null;
  delta: number | null;
  deduction: number | null;
  misses: number | null;
  noShoots: number | null;
  procedurals: number | null;
  overtimeValue: string | null;
  timeSeconds: number | null;
  pageNumber: number;
  hitFactorMatchesReportedPointsAndTime: boolean | null;
  diagnostics: string[];
}

export interface VerificationStage {
  stageId: string;
  stageNumber: number;
  stageGroup: string | null;
  status: string;
  actualHitFactor: number | null;
  forecastCenter: number | null;
  forecastLower: number | null;
  forecastUpper: number | null;
  relativeError: number | null;
  isWithinPredictionInterval: boolean | null;
  diagnostics: string[];
}

export interface VerificationGroup {
  stageGroup: string;
  stageCount: number;
  verifiableStageCount: number;
  missingStageCount: number;
  disqualifiedStageCount: number;
  zeroedStageCount: number;
  unverifiableStageCount: number;
  meanRelativeError: number | null;
  errorSampleStandardDeviation: number | null;
  intervalCoverageCount: number;
  intervalCoverageRate: number | null;
}

export interface VerificationReport {
  id: string;
  snapshotId: string;
  finalRevisionId: string;
  verifiableStageCount: number;
  missingStageCount: number;
  disqualifiedStageCount: number;
  zeroedStageCount: number;
  meanRelativeError: number | null;
  errorSampleStandardDeviation: number | null;
  intervalCoverageCount: number;
  observedBeta: number | null;
  stageGroupingConvention: string;
  informantDriftComparisonCount: number;
  informantDriftMissingStageCount: number;
  informantDriftDisqualifiedStageCount: number;
  informantDriftZeroedStageCount: number;
  informantDriftUnverifiableStageCount: number;
  informantDriftMeanRelativeError: number | null;
  informantDriftErrorSampleStandardDeviation: number | null;
  groups: VerificationGroup[];
  stages: VerificationStage[];
  diagnostics: string[];
}

export interface SnapshotSelection {
  id: string;
  matchId: string;
  matchRevisionId: string;
  engineVersion: string;
  parameterVersion: string;
  createdAtUtc: string;
}

export interface ParameterValues {
  beta: number;
  driftPercent: number;
  ownVariability: number;
  distancePenalty: number;
}

export interface ParameterBacktestEvaluation {
  snapshotId: string;
  finalRevisionId: string;
}

export interface ParameterBacktestStage {
  stageId: string;
  stageNumber: number;
  status: string;
  actualHitFactor: number | null;
  forecastCenter: number | null;
  forecastLower: number | null;
  forecastUpper: number | null;
  relativeError: number | null;
  isWithinPredictionInterval: boolean | null;
}

export interface ParameterBacktestMatchResult {
  snapshotId: string;
  matchId: string;
  finalRevisionId: string;
  status: string;
  verifiableStageCount: number;
  missingStageCount: number;
  disqualifiedStageCount: number;
  zeroedStageCount: number;
  meanRelativeError: number | null;
  errorSampleStandardDeviation: number | null;
  intervalCoverageCount: number;
  stages: ParameterBacktestStage[];
  diagnostics: string[];
}

export interface ParameterBacktestReport {
  id: string;
  engineVersion: string;
  parameterVersion: string;
  parameters: ParameterValues;
  evaluationCount: number;
  verifiableStageCount: number;
  meanRelativeError: number | null;
  errorSampleStandardDeviation: number | null;
  intervalCoverageCount: number;
  evaluations: ParameterBacktestMatchResult[];
  diagnostics: string[];
  createdAtUtc: string;
}

export interface ParameterAdoption {
  revision: number;
  backtestId: string;
  parameterVersion: string;
  parameters: ParameterValues;
  createdAtUtc: string;
}

export interface Draft {
  id: string;
  matchId: string;
  revision: number;
  importManifestId: string | null;
  identityId: string | null;
  division: string | null;
  parameterVersion: string;
  beta: number;
  driftPercent: number;
  ownVariability: number;
  distancePenalty: number;
  distancePenaltyConfirmed: boolean;
  performance: number;
  applyAfternoon: boolean;
  applyOneHand: boolean;
  excludedStageIds: string[];
  rivalIdentityIds: string[];
  strategyByStage: Record<string, string>;
  isStarted: boolean;
  frozenSnapshotId: string | null;
}

export interface DraftUpdate {
  expectedRevision: number;
  importManifestId: string | null;
  identityId: string | null;
  division: string | null;
  beta: number;
  driftPercent: number;
  ownVariability: number;
  distancePenalty: number;
  performance: number;
  applyAfternoon: boolean;
  applyOneHand: boolean;
  excludedStageIds: string[];
  rivalIdentityIds: string[];
  strategyByStage: Record<string, string>;
}

export interface StageEstimate {
  stageId: string;
  stageNumber: number;
  minimumRounds: number;
  rhythmSymbol: string | null;
  center: number | null;
  lower: number | null;
  upper: number | null;
  missCostSeconds: number | null;
  charlieCostSeconds: number | null;
  diagnostics: string[];
}

export interface Estimate {
  engineVersion: string;
  dispersionConvention: string;
  isAvailable: boolean;
  stages: StageEstimate[];
  diagnostics: string[];
}

export interface Snapshot {
  id: string;
  matchId: string;
  engineVersion: string;
  parameterVersion: string;
  createdAtUtc: string;
  stages: StageEstimate[];
}

export interface SnapshotReplayStage {
  stageId: string;
  stageNumber: number;
  frozenCenter: number | null;
  replayedCenter: number | null;
  frozenLower: number | null;
  replayedLower: number | null;
  frozenUpper: number | null;
  replayedUpper: number | null;
  matchesFrozenForecast: boolean;
}

export interface SnapshotReplay {
  snapshotId: string;
  engineVersion: string;
  isReplayAvailable: boolean;
  matchesFrozenForecast: boolean;
  stages: SnapshotReplayStage[];
  diagnostics: string[];
}

export interface FieldEventInput {
  clientOperationId: string;
  kind: 'turn' | 'reconnaissance' | 'midday' | 'correction';
  content: string;
  observedAtLocal: string;
  observedOffsetMinutes: number;
  startStage?: number;
  startTurn?: number;
  squadSize?: number;
  displayOrder?: string[];
}

export interface FieldEvent {
  id: string;
  snapshotId: string;
  sequence: number;
  kind: string;
  content: string;
  serverReceivedAtUtc: string;
  observedAtLocal: string | null;
  observedOffsetMinutes: number | null;
  startStage: number | null;
  startTurn: number | null;
  squadSize: number | null;
  displayOrder: string[] | null;
}

export class FreezerApiError extends Error {
  constructor(
    message: string,
    readonly status: number
  ) {
    super(message);
    this.name = 'FreezerApiError';
  }
}

const apiBaseUrl = import.meta.env.VITE_FREEZER_API_BASE_URL ?? '';
const accountApi = createApiClient({ mode: 'account', baseUrl: apiBaseUrl });

async function checked<T>(response: Promise<unknown>): Promise<T> {
  const result = await response;
  if (typeof result === 'object' && result !== null) {
    const record = result as Record<string, unknown>;
    const hasErrors = Array.isArray(record.errors) && record.errors.length > 0;
    const hasProblemDetails = typeof record.title === 'string' || typeof record.detail === 'string';
    const status = typeof record.status === 'number' ? record.status : hasErrors || hasProblemDetails ? 500 : 200;
    if (status >= 400 || hasErrors || hasProblemDetails) {
      const message = [record.detail, record.title]
        .find((value): value is string => typeof value === 'string' && value.length > 0)
        ?? (Array.isArray(record.errors) ? record.errors.map(String).join('; ') : '');
      throw new FreezerApiError(message || `Richiesta non riuscita (${status}).`, status);
    }
  }
  return result as T;
}

async function csrfToken(): Promise<string> {
  const response = await checked<{ token?: string }>(
    accountApi.get('/api/v1/account/csrf', undefined, '', true)
  );
  if (!response.token) throw new Error('Il server non ha restituito un token CSRF valido.');
  return response.token;
}

export const freezerApi = {
  getSession: () => checked<AccountSession>(
    accountApi.get('/api/v1/account/session', undefined, '', true)
  ),
  login: async (identifier: string, password: string) => {
    const token = await csrfToken();
    const session = await checked<AccountSession>(accountApi.post(
      '/api/v1/account/sessions',
      { identifier, password },
      '',
      { 'X-CSRF-TOKEN': token }
    ));
    if (session.account) rememberAccountId(session.account.id);
    return session;
  },
  register: async (input: {
    username: string;
    email: string;
    displayName: string;
    password: string;
    termsAccepted: boolean;
    privacyNoticeAcknowledged: boolean;
  }) => {
    const token = await csrfToken();
    const session = await checked<AccountSession>(accountApi.post(
      '/api/v1/accounts',
      input,
      '',
      { 'X-CSRF-TOKEN': token }
    ));
    if (session.account) rememberAccountId(session.account.id);
    return session;
  },
  logout: async () => {
    const token = await csrfToken();
    await checked<unknown>(accountApi.call(
      '/api/v1/account/sessions/current',
      'DELETE',
      {},
      '',
      undefined,
      false,
      false,
      undefined,
      { 'X-CSRF-TOKEN': token }
    ));
    sessionStorage.removeItem('freezer-analysis.account-id');
  },
  getMatches: () => checked<MatchSummary[]>(
    accountApi.get('/api/v1/matches', { limit: '100' }, '', true)
  ),
  getMatch: (matchId: string) => checked<MatchSummary>(
    accountApi.get(`/api/v1/matches/${encodeURIComponent(matchId)}`, undefined, '', true)
  ),
  getProfile: () => checked<Profile>(accountApi.get('/api/v1/profile', undefined, '', true)),
  saveProfile: (profile: Pick<Profile, 'identityId' | 'division' | 'powerFactor' | 'performance'>) =>
    checked<Profile>(accountApi.put('/api/v1/profile', profile)),
  getIdentities: () => checked<ShooterIdentity[]>(accountApi.get('/api/v1/identities', undefined, '', true)),
  createIdentity: (displayName: string, aliases: string[] = []) =>
    checked<ShooterIdentity>(accountApi.post('/api/v1/identities', { displayName, aliases })),
  getIdentityReview: (matchId: string) =>
    checked<IdentityReviewEntry[]>(
      accountApi.get(`/api/v1/matches/${encodeURIComponent(matchId)}/identities/review`, undefined, '', true)
    ),
  resolveIdentity: (matchId: string, resultId: string, identityId: string, expectedRevision: number) =>
    checked<IdentityResolution>(accountApi.put(
      `/api/v1/matches/${encodeURIComponent(matchId)}/identities/${encodeURIComponent(resultId)}`,
      { identityId, expectedRevision }
    )),
  removeIdentityResolution: (matchId: string, resultId: string, expectedRevision: number) =>
    checked<unknown>(accountApi.call(
      `/api/v1/matches/${encodeURIComponent(matchId)}/identities/${encodeURIComponent(resultId)}?expectedRevision=${expectedRevision}`,
      'DELETE',
      {}
    )),
  previewScores: (form: FormData) =>
    checked<ImportPreview>(accountApi.postFormData('/api/v1/imports/scores/preview', form)),
  previewPdfPair: (form: FormData) =>
    checked<PdfPairPreview>(accountApi.postFormData('/api/v1/imports/pdfs/preview', form)),
  publishScores: (form: FormData, expectedRevision: number) => {
    form.set('expectedRevision', String(expectedRevision));
    return checked<ImportPreview>(accountApi.postFormData('/api/v1/imports/scores/publish', form));
  },
  getDraft: (matchId: string) =>
    checked<Draft>(accountApi.get(`/api/v1/matches/${encodeURIComponent(matchId)}/draft`, undefined, '', true)),
  saveDraft: (matchId: string, draft: DraftUpdate) =>
    checked<Draft>(accountApi.put(`/api/v1/matches/${encodeURIComponent(matchId)}/draft`, draft)),
  getEstimate: (matchId: string) =>
    checked<Estimate>(accountApi.get(`/api/v1/matches/${encodeURIComponent(matchId)}/estimate`, undefined, '', true)),
  freeze: (matchId: string, expectedRevision: number) =>
    checked<Snapshot>(accountApi.post(`/api/v1/matches/${encodeURIComponent(matchId)}/freeze`, { expectedRevision })),
  getSnapshot: (snapshotId: string) =>
    checked<Snapshot>(accountApi.get(`/api/v1/snapshots/${encodeURIComponent(snapshotId)}`, undefined, '', true)),
  getSnapshots: () =>
    checked<SnapshotSelection[]>(accountApi.get('/api/v1/snapshots', { limit: '100' }, '', true)),
  replaySnapshot: (snapshotId: string) =>
    checked<SnapshotReplay>(accountApi.get(
      `/api/v1/snapshots/${encodeURIComponent(snapshotId)}/replay`,
      undefined,
      '',
      true
    )),
  verifySnapshot: (snapshotId: string, finalRevisionId: string) =>
    checked<VerificationReport>(accountApi.post(
      `/api/v1/snapshots/${encodeURIComponent(snapshotId)}/verification`,
      { finalRevisionId }
    )),
  getLatestParameterAdoption: () =>
    checked<ParameterAdoption>(accountApi.get('/api/v1/parameters/adoption', undefined, '', true)),
  backtestParameters: (parameters: ParameterValues, evaluations: ParameterBacktestEvaluation[]) =>
    checked<ParameterBacktestReport>(accountApi.post('/api/v1/parameter-backtests', { parameters, evaluations })),
  adoptParameters: (backtestId: string, expectedRevision: number, confirm: boolean) =>
    checked<ParameterAdoption>(accountApi.post('/api/v1/parameters/adoption', {
      backtestId,
      expectedRevision,
      confirm
    })),
  getFieldEvents: (snapshotId: string) =>
    checked<FieldEvent[]>(accountApi.get(`/api/v1/snapshots/${encodeURIComponent(snapshotId)}/field-events`, undefined, '', true)),
  appendFieldEvent: (snapshotId: string, event: FieldEventInput) =>
    checked<FieldEvent>(accountApi.post(`/api/v1/snapshots/${encodeURIComponent(snapshotId)}/field-events`, event)),
  createArtifactPackage: async (snapshotId: string) => {
    return checked<Blob>(accountApi.call(
      `/api/v1/snapshots/${encodeURIComponent(snapshotId)}/artifacts`,
      'POST',
      {},
      '',
      undefined,
      true
    ));
  }
};

export function rememberAccountId(accountId: string): void {
  sessionStorage.setItem('freezer-analysis.account-id', accountId);
}

export function rememberedAccountId(): string | null {
  return sessionStorage.getItem('freezer-analysis.account-id');
}

export function clearRememberedAccountId(): void {
  sessionStorage.removeItem('freezer-analysis.account-id');
}

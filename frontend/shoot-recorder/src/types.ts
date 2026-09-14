export interface OverlayElement {
  x: number
  y: number
  width: number
  height: number
  color: string
  strokeColor: string
  strokeWidth: number
  fontSize: number
}

export interface OverlayLayout {
  shot: OverlayElement
  social: OverlayElement
}

export interface ShootPreferences {
  overlay: OverlayLayout
  socialHandler: string
  peakMultiplier: number
}

export type ScoringMode = 'IPSC' | 'IDPA'

export interface TrimRange {
  startSeconds: number
  endSeconds: number
}

export interface ShotScore {
  points: number
  penalties: number
  misses: number
}

export interface ShotCandidate {
  id: string
  timeSeconds: number
  confidence: number
  selected: boolean
  source: 'analysis' | 'manual'
}

export interface AnalysisResult {
  durationSeconds: number
  candidates: ShotCandidate[]
  sampledFrames: number
  trimRange: TrimRange
  timerOriginSeconds: number
}

export interface OverlayCompositionData {
  currentSourceSeconds: number
  compositionSeconds: number
  timerSeconds: number
  shotTimings: Array<{
    candidate: ShotCandidate
    relativeSeconds: number
    splitSeconds: number
    shotNumber: number
    score: ShotScore
  }>
  scoringMode: ScoringMode
  totalScore: number
  hitFactor: number | null
  thermometerValue: number | null
  idpaPointsDown: number | null
  idpaPenaltyCount: number | null
  socialHandler: string
}

export const DEFAULT_SHOT_OVERLAY: OverlayElement = {
  x: 4,
  y: 4,
  width: 30,
  height: 42,
  color: '#ffffff',
  strokeColor: '#0b1220',
  strokeWidth: 2,
  fontSize: 45
}

export const DEFAULT_SOCIAL_OVERLAY: OverlayElement = {
  x: 4,
  y: 82,
  width: 28,
  height: 12,
  color: '#f6b73c',
  strokeColor: '#0b1220',
  strokeWidth: 2,
  fontSize: 45
}

export const DEFAULT_OVERLAY: OverlayLayout = {
  shot: DEFAULT_SHOT_OVERLAY,
  social: DEFAULT_SOCIAL_OVERLAY
}
export const PREFERENCES_KEY = 'shoot-recorder.preferences'
export const DEFAULT_PEAK_MULTIPLIER = 1.15

import type { ScoringMode, ShotScore } from './types'

export const IPSC_MISS_OR_PENALTY_VALUE = 10
export const IDPA_MISS_POINTS_DOWN = 5
export const IDPA_PROCEDURAL_PENALTY_SECONDS = 3
export const IPSC_THERMOMETER_MAX = 11

export const DEFAULT_SHOT_SCORE: ShotScore = { points: 0, penalties: 0, misses: 0 }

const safeScore = (score: ShotScore): ShotScore => ({
  points: Number.isFinite(score.points) ? Math.max(0, score.points) : 0,
  penalties: Number.isFinite(score.penalties) ? Math.max(0, score.penalties) : 0,
  misses: Number.isFinite(score.misses) ? Math.max(0, score.misses) : 0
})

export const calculateIpscScore = (score: ShotScore): number => {
  const safe = safeScore(score)
  return safe.points - (safe.penalties + safe.misses) * IPSC_MISS_OR_PENALTY_VALUE
}

export const calculateIdpaScore = (score: ShotScore): { pointsDown: number; penaltySeconds: number } => {
  const safe = safeScore(score)
  return {
    pointsDown: safe.points + safe.misses * IDPA_MISS_POINTS_DOWN,
    penaltySeconds: safe.penalties * IDPA_PROCEDURAL_PENALTY_SECONDS
  }
}

export const calculateShotScore = (mode: ScoringMode, score: ShotScore): number =>
  mode === 'IPSC' ? calculateIpscScore(score) : calculateIdpaScore(score).pointsDown

export const calculateHitFactor = (totalScore: number, elapsedSeconds: number): number =>
  elapsedSeconds > 0 ? totalScore / elapsedSeconds : 0

export const clampThermometerValue = (hitFactor: number): number =>
  Math.min(IPSC_THERMOMETER_MAX, Math.max(0, Number.isFinite(hitFactor) ? hitFactor : 0))

export const getScoreSummary = (
  mode: ScoringMode,
  scores: ShotScore[],
  elapsedSeconds: number
) => {
  if (mode === 'IPSC') {
    const totalScore = scores.reduce((total, score) => total + calculateIpscScore(score), 0)
    const hitFactor = calculateHitFactor(totalScore, elapsedSeconds)
    return { totalScore, hitFactor, thermometerValue: clampThermometerValue(hitFactor), idpaPointsDown: null, idpaPenaltyCount: null }
  }
  const idpa = scores.reduce(
    (summary, score) => {
      const current = calculateIdpaScore(score)
      return { pointsDown: summary.pointsDown + current.pointsDown, penaltySeconds: summary.penaltySeconds + current.penaltySeconds, penaltyCount: summary.penaltyCount + score.penalties }
    },
    { pointsDown: 0, penaltySeconds: 0, penaltyCount: 0 }
  )
  return { totalScore: idpa.pointsDown, hitFactor: null, thermometerValue: null, idpaPointsDown: idpa.pointsDown, idpaPenaltyCount: idpa.penaltyCount }
}

export const preserveShotScores = (
  previous: Record<string, ShotScore>,
  previousCandidates: Array<{ id: string; timeSeconds: number }>,
  nextCandidates: Array<{ id: string; timeSeconds: number }>
): Record<string, ShotScore> => {
  const next: Record<string, ShotScore> = {}
  nextCandidates.forEach((candidate) => {
    const direct = previous[candidate.id]
    if (direct) {
      next[candidate.id] = direct
      return
    }
    const match = previousCandidates.find((previousCandidate) => Math.abs(previousCandidate.timeSeconds - candidate.timeSeconds) < 0.08)
    next[candidate.id] = match ? previous[match.id] ?? { ...DEFAULT_SHOT_SCORE } : { ...DEFAULT_SHOT_SCORE }
  })
  return next
}
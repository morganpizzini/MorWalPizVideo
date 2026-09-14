import { getScoreSummary } from './scoring'
import { mapSourceToCompositionTime } from './trim'
import { getSelectedShotTimings } from './timeline'
import type { AnalysisResult, OverlayCompositionData, ScoringMode, ShotScore } from './types'

export const getOverlayCompositionData = (
  analysis: AnalysisResult,
  currentSourceSeconds: number,
  scoringMode: ScoringMode,
  scores: Record<string, ShotScore>,
  socialHandler: string
): OverlayCompositionData => {
  const safeCurrentSourceSeconds = Math.min(analysis.trimRange.endSeconds, Math.max(analysis.trimRange.startSeconds, currentSourceSeconds))
  const shotTimings = getSelectedShotTimings(analysis, safeCurrentSourceSeconds).map((timing) => ({
    ...timing,
    score: scores[timing.candidate.id] ?? { points: 0, penalties: 0, misses: 0 }
  }))
  const allTimings = getSelectedShotTimings(analysis, safeCurrentSourceSeconds)
  const summary = getScoreSummary(
    scoringMode,
    allTimings.map((timing) => scores[timing.candidate.id] ?? { points: 0, penalties: 0, misses: 0 }),
    Math.max(0, safeCurrentSourceSeconds - analysis.timerOriginSeconds)
  )
  return {
    currentSourceSeconds: safeCurrentSourceSeconds,
    compositionSeconds: mapSourceToCompositionTime(safeCurrentSourceSeconds, analysis.trimRange),
    timerSeconds: Math.max(0, safeCurrentSourceSeconds - analysis.timerOriginSeconds),
    shotTimings,
    scoringMode,
    totalScore: summary.totalScore,
    hitFactor: summary.hitFactor,
    thermometerValue: summary.thermometerValue,
    idpaPointsDown: summary.idpaPointsDown,
    idpaPenaltyCount: summary.idpaPenaltyCount,
    socialHandler: socialHandler.trim().replace(/^@+/, '')
  }
}
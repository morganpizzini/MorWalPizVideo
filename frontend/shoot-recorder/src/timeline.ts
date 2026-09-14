import { mapSourceToCompositionTime } from './trim'
import type { AnalysisResult, ShotCandidate } from './types'

export interface ShotTiming {
  candidate: ShotCandidate
  compositionSeconds: number
  relativeSeconds: number
  splitSeconds: number
  shotNumber: number
}

const roundToTenths = (value: number): number => Math.round(value * 10) / 10

export const getSelectedShotTimings = (analysis: AnalysisResult, currentTime?: number): ShotTiming[] => {
  let previousTime = analysis.timerOriginSeconds
  const timings = analysis.candidates
    .filter((candidate) =>
      candidate.selected &&
      candidate.timeSeconds >= analysis.timerOriginSeconds &&
      candidate.timeSeconds >= analysis.trimRange.startSeconds &&
      candidate.timeSeconds <= analysis.trimRange.endSeconds &&
      (currentTime === undefined || candidate.timeSeconds <= currentTime)
    )
    .sort((left, right) => left.timeSeconds - right.timeSeconds)
    .map((candidate, index) => {
      const relativeSeconds = roundToTenths(candidate.timeSeconds - analysis.timerOriginSeconds)
      const splitSeconds = roundToTenths(candidate.timeSeconds - previousTime)
      previousTime = candidate.timeSeconds
      return { candidate, compositionSeconds: mapSourceToCompositionTime(candidate.timeSeconds, analysis.trimRange), relativeSeconds, splitSeconds, shotNumber: index + 1 }
    })
  return currentTime === undefined ? timings : timings.slice(-4)
}

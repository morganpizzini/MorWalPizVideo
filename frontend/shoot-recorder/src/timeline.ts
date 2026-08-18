import type { AnalysisResult, ShotCandidate } from './types'

export interface ShotTiming {
  candidate: ShotCandidate
  relativeSeconds: number
  splitSeconds: number
  shotNumber: number
}

const roundToTenths = (value: number): number => Math.round(value * 10) / 10

export const getSelectedShotTimings = (analysis: AnalysisResult, currentTime?: number): ShotTiming[] => {
  let previousTime = analysis.startBeepSeconds
  const timings = analysis.candidates
    .filter((candidate) =>
      candidate.selected &&
      candidate.timeSeconds >= analysis.startBeepSeconds &&
      (currentTime === undefined || candidate.timeSeconds <= currentTime)
    )
    .sort((left, right) => left.timeSeconds - right.timeSeconds)
    .map((candidate, index) => {
      const relativeSeconds = roundToTenths(candidate.timeSeconds - analysis.startBeepSeconds)
      const splitSeconds = roundToTenths(candidate.timeSeconds - previousTime)
      previousTime = candidate.timeSeconds
      return { candidate, relativeSeconds, splitSeconds, shotNumber: index + 1 }
    })
  return currentTime === undefined ? timings : timings.slice(-4)
}

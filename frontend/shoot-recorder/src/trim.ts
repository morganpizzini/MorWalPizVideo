import type { TrimRange } from './types'

export const MIN_TRIM_DURATION_SECONDS = 0.1

export const createTrimRange = (durationSeconds: number): TrimRange => ({
  startSeconds: 0,
  endSeconds: Math.max(MIN_TRIM_DURATION_SECONDS, durationSeconds)
})

export const isValidTrimRange = (range: TrimRange, durationSeconds: number): boolean =>
  Number.isFinite(range.startSeconds) &&
  Number.isFinite(range.endSeconds) &&
  range.startSeconds >= 0 &&
  range.endSeconds <= durationSeconds &&
  range.endSeconds - range.startSeconds >= MIN_TRIM_DURATION_SECONDS

export const normalizeTrimRange = (range: TrimRange, durationSeconds: number): TrimRange => {
  const safeDuration = Math.max(MIN_TRIM_DURATION_SECONDS, durationSeconds)
  const startSeconds = Math.min(Math.max(0, range.startSeconds), safeDuration - MIN_TRIM_DURATION_SECONDS)
  const endSeconds = Math.min(safeDuration, Math.max(startSeconds + MIN_TRIM_DURATION_SECONDS, range.endSeconds))
  return { startSeconds, endSeconds }
}

export const clampSourceTimeToTrim = (sourceSeconds: number, range: TrimRange): number =>
  Math.min(range.endSeconds, Math.max(range.startSeconds, sourceSeconds))

export const mapSourceToCompositionTime = (sourceSeconds: number, range: TrimRange): number =>
  clampSourceTimeToTrim(sourceSeconds, range) - range.startSeconds

export const mapCompositionToSourceTime = (compositionSeconds: number, range: TrimRange): number =>
  clampSourceTimeToTrim(range.startSeconds + compositionSeconds, range)

export const resolveTimerOrigin = (range: TrimRange, beepSeconds?: number): number =>
  beepSeconds !== undefined && beepSeconds >= range.startSeconds && beepSeconds <= range.endSeconds
    ? beepSeconds
    : range.startSeconds
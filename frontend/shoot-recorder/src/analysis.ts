import type { AnalysisResult, ShotCandidate } from './types'

const SAMPLE_INTERVAL_SECONDS = 0.1
const MAX_SAMPLES = 600
const MIN_SHOT_GAP_SECONDS = 0.08

const waitFor = (video: HTMLVideoElement, event: 'loadedmetadata' | 'seeked'): Promise<void> =>
  new Promise((resolve, reject) => {
    const onEvent = () => {
      video.removeEventListener(event, onEvent)
      video.removeEventListener('error', onError)
      resolve()
    }
    const onError = () => {
      video.removeEventListener(event, onEvent)
      video.removeEventListener('error', onError)
      reject(new Error('The selected video could not be decoded.'))
    }
    video.addEventListener(event, onEvent, { once: true })
    video.addEventListener('error', onError, { once: true })
  })

const frameDifference = (
  context: CanvasRenderingContext2D,
  video: HTMLVideoElement,
  previous: Uint8ClampedArray | undefined
): { difference: number; pixels: Uint8ClampedArray } => {
  context.drawImage(video, 0, 0, 96, 54)
  const pixels = context.getImageData(0, 0, 96, 54).data
  if (!previous) return { difference: 0, pixels }
  let difference = 0
  for (let index = 0; index < pixels.length; index += 4) {
    difference += Math.abs(pixels[index] - previous[index])
    difference += Math.abs(pixels[index + 1] - previous[index + 1])
    difference += Math.abs(pixels[index + 2] - previous[index + 2])
  }
  return { difference: difference / (pixels.length / 4 * 3 * 255), pixels }
}

export const analyzeVideo = async (file: File, peakMultiplier = 1.15, startAtSeconds = 0): Promise<AnalysisResult> => {
  const video = document.createElement('video')
  const canvas = document.createElement('canvas')
  canvas.width = 96
  canvas.height = 54
  const context = canvas.getContext('2d')
  if (!context) throw new Error('Canvas analysis is not available in this browser.')

  const url = URL.createObjectURL(file)
  video.src = url
  video.muted = true
  video.preload = 'metadata'
  try {
    await waitFor(video, 'loadedmetadata')
    const durationSeconds = video.duration
    const analysisStart = Math.min(Math.max(0, startAtSeconds), durationSeconds)
    const analysisDuration = Math.max(0, durationSeconds - analysisStart)
    const sampleCount = Math.min(MAX_SAMPLES, Math.max(1, Math.ceil(analysisDuration / SAMPLE_INTERVAL_SECONDS)))
    const scores: number[] = []
    let previous: Uint8ClampedArray | undefined
    for (let index = 0; index < sampleCount; index += 1) {
      const sampleTime = analysisStart + index * analysisDuration / sampleCount
      if (index > 0 || analysisStart > 0) {
        video.currentTime = Math.min(durationSeconds, sampleTime)
        await waitFor(video, 'seeked')
      }
      const frame = frameDifference(context, video, previous)
      scores.push(frame.difference)
      previous = frame.pixels
    }

    const mean = scores.reduce((total, score) => total + score, 0) / scores.length
    const deviation = Math.sqrt(scores.reduce((total, score) => total + (score - mean) ** 2, 0) / scores.length)
    const threshold = Math.max(0.02, mean + deviation * peakMultiplier)
    const candidates: ShotCandidate[] = []
    scores.forEach((score, index) => {
      const timeSeconds = analysisStart + index * analysisDuration / sampleCount
      const previousCandidate = candidates[candidates.length - 1]
      if (score >= threshold && (!previousCandidate || timeSeconds - previousCandidate.timeSeconds >= MIN_SHOT_GAP_SECONDS)) {
        candidates.push({
          id: `shot-${candidates.length + 1}`,
          timeSeconds,
          confidence: Math.min(99, Math.max(1, Math.round((score / Math.max(threshold, 0.001)) * 70))),
          selected: true,
          source: 'analysis'
        })
      }
    })
    return { durationSeconds, candidates, sampledFrames: sampleCount, startBeepSeconds: analysisStart }
  } finally {
    URL.revokeObjectURL(url)
  }
}

export const formatTime = (seconds: number): string => {
  const safeSeconds = Math.max(0, seconds)
  if (safeSeconds < 60) return safeSeconds.toFixed(1)
  const minutes = Math.floor(safeSeconds / 60)
  const remainingSeconds = Math.floor(safeSeconds % 60)
  const tenths = Math.floor((safeSeconds - Math.floor(safeSeconds)) * 10)
  return `${minutes}:${String(remainingSeconds).padStart(2, '0')}.${tenths}`
}

export const formatShotSeconds = formatTime

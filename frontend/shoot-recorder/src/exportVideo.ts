import { formatShotSeconds } from './analysis'
import { getOverlayCompositionData } from './composition'
import type { AnalysisResult, OverlayElement, OverlayLayout, ScoringMode, ShotScore } from './types'

const MP4_MIME = 'video/mp4;codecs=avc1.42E01E,mp4a.40.2'
interface CapturableVideo extends HTMLVideoElement {
  captureStream: () => MediaStream
}

const captureVideoStream = (video: HTMLVideoElement): MediaStream => {
  if (!('captureStream' in video) || typeof video.captureStream !== 'function') {
    throw new Error('This browser cannot capture the source audio track.')
  }
  return (video as CapturableVideo).captureStream()
}

const seekVideo = (video: HTMLVideoElement, timeSeconds: number): Promise<void> => {
  if (Math.abs(video.currentTime - timeSeconds) <= 0.001) return Promise.resolve()
  return new Promise((resolve, reject) => {
    const onSeeked = () => {
      video.removeEventListener('seeked', onSeeked)
      video.removeEventListener('error', onError)
      resolve()
    }
    const onError = () => {
      video.removeEventListener('seeked', onSeeked)
      video.removeEventListener('error', onError)
      reject(new Error('The selected trim range could not be opened for export.'))
    }
    video.addEventListener('seeked', onSeeked, { once: true })
    video.addEventListener('error', onError, { once: true })
    video.currentTime = timeSeconds
  })
}

const canExportWithMediaRecorder = (): boolean =>
  typeof MediaRecorder !== 'undefined' &&
  typeof MediaRecorder.isTypeSupported === 'function' &&
  MediaRecorder.isTypeSupported(MP4_MIME) &&
  typeof HTMLCanvasElement.prototype.captureStream === 'function'

export const canExportWithRemotion = (): boolean =>
  typeof VideoEncoder !== 'undefined' &&
  typeof VideoDecoder !== 'undefined' &&
  typeof AudioDecoder !== 'undefined'

export const canExportMp4 = (): boolean => canExportWithMediaRecorder() || canExportWithRemotion()

const drawOutlinedText = (
  context: CanvasRenderingContext2D,
  text: string,
  x: number,
  y: number,
  element: OverlayElement,
  font: string
) => {
  context.font = `bold ${font}`
  context.lineWidth = element.strokeWidth
  context.strokeStyle = element.strokeColor
  context.strokeText(text, x, y)
  context.fillStyle = element.color
  context.fillText(text, x, y)
}

const drawSocialOverlay = (
  context: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  layout: OverlayElement,
  handler: string
) => {
  if (!handler) return
  const x = canvas.width * layout.x / 100
  const y = canvas.height * layout.y / 100
  const height = canvas.height * layout.height / 100
  const padding = canvas.width * 0.04
  drawOutlinedText(
    context,
    `@${handler}`,
    x + padding,
    y + height - padding,
    layout,
    `${Math.max(12, height * layout.fontSize / 100)}px sans-serif`
  )
}

const drawShotOverlay = (
  context: CanvasRenderingContext2D,
  canvas: HTMLCanvasElement,
  layout: OverlayElement,
  composition: ReturnType<typeof getOverlayCompositionData>
) => {
  const x = canvas.width * layout.x / 100
  const y = canvas.height * layout.y / 100
  const width = canvas.width * layout.width / 100
  const height = canvas.height * layout.height / 100
  const paddingX = width * 0.04
  const paddingY = paddingX
  const lineHeight = Math.max(22, (height - paddingY * 2) / 6)
  const fontSize = Math.max(12, lineHeight * layout.fontSize / 100)
  context.textAlign = 'left'
  drawOutlinedText(context, 'TIME', x + paddingX, y + paddingY + lineHeight, layout, `${fontSize}px sans-serif`)
  context.textAlign = 'right'
  drawOutlinedText(
    context,
    formatShotSeconds(composition.timerSeconds),
    x + width - paddingX,
    y + paddingY + lineHeight,
    layout,
    `${fontSize}px sans-serif`
  )
  composition.shotTimings.forEach((timing, index) => {
    const lineY = y + paddingY + lineHeight * (index + 2)
    context.textAlign = 'right'
    drawOutlinedText(
      context,
      `split ${formatShotSeconds(timing.splitSeconds)}`,
      x + width - paddingX,
      lineY - lineHeight * 0.38,
      layout,
      `${fontSize * 0.72}px sans-serif`
    )
    context.textAlign = 'left'
    drawOutlinedText(
      context,
      `shoot ${timing.shotNumber}`,
      x + paddingX,
      lineY,
      layout,
      `${fontSize}px sans-serif`
    )
    context.textAlign = 'right'
    drawOutlinedText(
      context,
      formatShotSeconds(timing.relativeSeconds),
      x + width - paddingX,
      lineY,
      layout,
      `${fontSize}px sans-serif`
    )
  })
  context.textAlign = 'left'
  drawOutlinedText(
    context,
    composition.scoringMode === 'IPSC' ? `SCORE ${composition.totalScore}` : `IDPA ${composition.idpaPointsDown ?? 0} down`,
    x + paddingX,
    y + height - paddingY,
    layout,
    `${fontSize * 0.78}px sans-serif`
  )
  if (composition.scoringMode === 'IPSC' && composition.thermometerValue !== null) {
    const thermometerX = x + width - paddingX
    const thermometerHeight = height * 0.12
    context.fillStyle = layout.color
    context.fillRect(thermometerX - 5, y + height - paddingY - thermometerHeight, 5, thermometerHeight * composition.thermometerValue / 11)
    context.textAlign = 'right'
    drawOutlinedText(context, `HF ${composition.hitFactor?.toFixed(2) ?? '0.00'}`, thermometerX, y + height - paddingY, layout, `${fontSize * 0.65}px sans-serif`)
  }
  context.textAlign = 'left'
}

export const exportAnnotatedMp4 = async (
  file: File,
  analysis: AnalysisResult,
  overlay: OverlayLayout,
  socialHandler: string,
  scoringMode: ScoringMode,
  scores: Record<string, ShotScore>,
  onProgress?: (progress: number) => void
): Promise<Blob> => {
  if (!canExportMp4()) {
    throw new Error('This browser cannot encode MP4 recordings with Remotion WebCodecs or MediaRecorder. Use a current Chromium browser or Safari with MP4 support.')
  }
  if (canExportWithRemotion() && analysis.trimRange.startSeconds <= 0.01 && Math.abs(analysis.trimRange.endSeconds - analysis.durationSeconds) <= 0.05) {
    try {
      const remotionBlob = await exportWithRemotionWebCodecs(file, analysis, overlay, socialHandler, scoringMode, scores, onProgress)
      if (remotionBlob) return remotionBlob
    } catch (remotionError) {
      if (!canExportWithMediaRecorder()) {
        throw new Error(remotionError instanceof Error ? `Remotion MP4 export failed: ${remotionError.message}` : 'Remotion MP4 export failed.')
      }
    }
  }
  if (!canExportWithMediaRecorder()) {
    throw new Error('Remotion browser MP4 export is unavailable for this trim range, and the MediaRecorder fallback is not supported.')
  }
  const video = document.createElement('video')
  const canvas = document.createElement('canvas')
  const url = URL.createObjectURL(file)
  video.src = url
  video.muted = false
  video.playsInline = true
  video.preload = 'auto'
  video.controls = false
  video.style.display = 'none'
  video.setAttribute('aria-hidden', 'true')
  await new Promise<void>((resolve, reject) => {
    video.onloadedmetadata = () => resolve()
    video.onerror = () => reject(new Error('The selected video could not be decoded for export.'))
  })
  await new Promise<void>((resolve, reject) => {
    const onSeeked = () => {
      video.removeEventListener('seeked', onSeeked)
      video.removeEventListener('error', onError)
      resolve()
    }
    const onError = () => {
      video.removeEventListener('seeked', onSeeked)
      video.removeEventListener('error', onError)
      reject(new Error('The selected trim range could not be opened for export.'))
    }
    video.addEventListener('seeked', onSeeked, { once: true })
    video.addEventListener('error', onError, { once: true })
    video.currentTime = analysis.trimRange.startSeconds
  })
  canvas.width = video.videoWidth || 1280
  canvas.height = video.videoHeight || 720
  const context = canvas.getContext('2d')
  if (!context) throw new Error('Canvas export is not available in this browser.')
  const canvasStream = canvas.captureStream(30)
  let sourceStream: MediaStream | undefined
  let audioContext: AudioContext | undefined
  let audioTrackAdded = false
  const captureStreamAudio = async (): Promise<boolean> => {
    await video.play()
    video.pause()
    await seekVideo(video, analysis.trimRange.startSeconds)
    const capturedStream = captureVideoStream(video)
    sourceStream = capturedStream
    const audioTracks = capturedStream.getAudioTracks()
    if (audioTracks.length === 0) return false
    audioTracks.forEach((track) => canvasStream.addTrack(track))
    return true
  }

  if (typeof AudioContext !== 'undefined') {
    try {
      audioContext = new AudioContext()
      const source = audioContext.createMediaElementSource(video)
      const destination = audioContext.createMediaStreamDestination()
      source.connect(destination)
      const audioTrack = destination.stream.getAudioTracks()[0]
      if (audioTrack) {
        canvasStream.addTrack(audioTrack)
        audioTrackAdded = true
      }
    } catch {
      if (audioContext) await audioContext.close()
      audioContext = undefined
    }
  }
  if (!audioTrackAdded) {
    try {
      audioTrackAdded = await captureStreamAudio()
    } catch {
      audioTrackAdded = false
    }
  }
  if (!audioTrackAdded) {
    throw new Error('The source audio track could not be captured for MP4 export.')
  }

  const recorder = new MediaRecorder(canvasStream, { mimeType: MP4_MIME })
  const chunks: Blob[] = []
  const finished = new Promise<Blob>((resolve, reject) => {
    recorder.ondataavailable = (event) => {
      if (event.data.size > 0) chunks.push(event.data)
    }
    recorder.onerror = () => reject(new Error('MP4 export stopped unexpectedly.'))
    recorder.onstop = () => resolve(new Blob(chunks, { type: 'video/mp4' }))
  })

  const drawFrame = () => {
    context.drawImage(video, 0, 0, canvas.width, canvas.height)
    const composition = getOverlayCompositionData(analysis, video.currentTime, scoringMode, scores, socialHandler)
    drawShotOverlay(context, canvas, overlay.shot, composition)
    drawSocialOverlay(context, canvas, overlay.social, composition.socialHandler)
    onProgress?.(Math.min(1, composition.compositionSeconds / (analysis.trimRange.endSeconds - analysis.trimRange.startSeconds)))
  }

  try {
    recorder.start(250)
    await audioContext?.resume()
    await video.play()
    return await new Promise<Blob>((resolve, reject) => {
      const draw = () => {
        drawFrame()
        if (video.ended || video.currentTime >= analysis.trimRange.endSeconds) {
          video.pause()
          onProgress?.(1)
          recorder.stop()
          finished.then(resolve).catch(reject)
        } else {
          requestAnimationFrame(draw)
        }
      }
      requestAnimationFrame(draw)
    })
  } finally {
    video.pause()
    canvasStream.getTracks().forEach((track) => track.stop())
    sourceStream?.getTracks().forEach((track) => track.stop())
    await audioContext?.close()
    URL.revokeObjectURL(url)
  }
}

const exportWithRemotionWebCodecs = async (
  file: File,
  analysis: AnalysisResult,
  overlay: OverlayLayout,
  socialHandler: string,
  scoringMode: ScoringMode,
  scores: Record<string, ShotScore>,
  onProgress?: (progress: number) => void
): Promise<Blob> => {
  const { convertMedia } = await import('@remotion/webcodecs')
  const sourceUrl = URL.createObjectURL(file)
  let canvas: HTMLCanvasElement | undefined
  let context: CanvasRenderingContext2D | null = null
  try {
    const result = await convertMedia({
      src: sourceUrl,
      container: 'mp4',
      videoCodec: 'h264',
      audioCodec: 'aac',
      onProgress: (state) => {
        if (state.overallProgress !== null) onProgress?.(state.overallProgress)
      },
      onVideoFrame: ({ frame }) => {
        if (!canvas) {
          canvas = document.createElement('canvas')
          canvas.width = frame.displayWidth
          canvas.height = frame.displayHeight
          context = canvas.getContext('2d')
        }
        if (!context || !canvas) return frame
        const sourceSeconds = frame.timestamp / 1_000_000
        context.drawImage(frame, 0, 0, canvas.width, canvas.height)
        const composition = getOverlayCompositionData(analysis, sourceSeconds, scoringMode, scores, socialHandler)
        drawShotOverlay(context, canvas, overlay.shot, composition)
        drawSocialOverlay(context, canvas, overlay.social, composition.socialHandler)
        const encodedFrame = new VideoFrame(canvas, { timestamp: frame.timestamp, duration: frame.duration ?? undefined })
        frame.close()
        return encodedFrame
      }
    })
    const blob = await result.save()
    await result.remove()
    return blob
  } finally {
    URL.revokeObjectURL(sourceUrl)
  }
}

export const downloadBlob = (blob: Blob, filename: string): void => {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  anchor.click()
  URL.revokeObjectURL(url)
}

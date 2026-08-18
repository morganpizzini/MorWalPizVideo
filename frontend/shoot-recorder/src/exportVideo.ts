import { formatShotSeconds } from './analysis'
import { getSelectedShotTimings } from './timeline'
import type { AnalysisResult, OverlayElement, OverlayLayout } from './types'

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

export const canExportMp4 = (): boolean =>
  typeof MediaRecorder !== 'undefined' &&
  typeof MediaRecorder.isTypeSupported === 'function' &&
  MediaRecorder.isTypeSupported(MP4_MIME) &&
  typeof HTMLCanvasElement.prototype.captureStream === 'function'

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
  socialHandler: string
) => {
  const handler = socialHandler.trim().replace(/^@+/, '')
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
  analysis: AnalysisResult,
  currentTime: number
) => {
  const timings = getSelectedShotTimings(analysis, currentTime)
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
    formatShotSeconds(currentTime - analysis.startBeepSeconds),
    x + width - paddingX,
    y + paddingY + lineHeight,
    layout,
    `${fontSize}px sans-serif`
  )
  timings.forEach((timing, index) => {
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
}

export const exportAnnotatedMp4 = async (
  file: File,
  analysis: AnalysisResult,
  overlay: OverlayLayout,
  socialHandler: string,
  onProgress?: (progress: number) => void
): Promise<Blob> => {
  if (!canExportMp4()) {
    throw new Error('This browser cannot encode MP4 recordings. Safari or a browser with MP4 MediaRecorder support is required.')
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
    video.currentTime = 0
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
    drawShotOverlay(context, canvas, overlay.shot, analysis, video.currentTime)
    drawSocialOverlay(context, canvas, overlay.social, socialHandler)
    if (video.duration > 0) onProgress?.(Math.min(1, video.currentTime / video.duration))
  }

  try {
    recorder.start(250)
    await audioContext?.resume()
    await video.play()
    return await new Promise<Blob>((resolve, reject) => {
      const draw = () => {
        drawFrame()
        if (video.ended) {
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

export const downloadBlob = (blob: Blob, filename: string): void => {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  anchor.click()
  URL.revokeObjectURL(url)
}

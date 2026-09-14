import { afterEach, describe, expect, it, vi } from 'vitest'
import { exportAnnotatedMp4 } from '../exportVideo'
import { DEFAULT_OVERLAY } from '../types'

describe('MP4 export', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it('starts capture-stream audio at the confirmed trim start', async () => {
    const video = document.createElement('video')
    const canvas = document.createElement('canvas')
    let currentTime = 0
    const capturedTimes: number[] = []
    const audioTrack = { stop: vi.fn() }
    const canvasTrack = { stop: vi.fn() }
    const canvasStream = {
      addTrack: vi.fn(),
      getTracks: () => [canvasTrack]
    }
    const sourceStream = {
      getAudioTracks: () => [audioTrack],
      getTracks: () => [audioTrack]
    }
    const context = {
      drawImage: vi.fn(),
      fillRect: vi.fn(),
      fillText: vi.fn(),
      strokeText: vi.fn()
    } as unknown as CanvasRenderingContext2D

    Object.defineProperty(video, 'currentTime', {
      configurable: true,
      get: () => currentTime,
      set: (value: number) => {
        currentTime = value
        queueMicrotask(() => video.dispatchEvent(new Event('seeked')))
      }
    })
    Object.defineProperties(video, {
      duration: { configurable: true, value: 8 },
      videoWidth: { configurable: true, value: 1920 },
      videoHeight: { configurable: true, value: 1080 },
      captureStream: {
        configurable: true,
        value: vi.fn(() => {
          capturedTimes.push(video.currentTime)
          return sourceStream
        })
      },
      play: { configurable: true, value: vi.fn(async () => undefined) },
      pause: { configurable: true, value: vi.fn() }
    })
    Object.defineProperty(canvas, 'getContext', { configurable: true, value: vi.fn(() => context) })
    Object.defineProperty(HTMLCanvasElement.prototype, 'captureStream', { configurable: true, value: vi.fn(() => canvasStream) })
    vi.spyOn(document, 'createElement').mockImplementation((tagName: string) => tagName === 'video' ? video : canvas)
    vi.stubGlobal('AudioContext', undefined)
    vi.stubGlobal('VideoEncoder', undefined)
    vi.stubGlobal('VideoDecoder', undefined)
    vi.stubGlobal('AudioDecoder', undefined)
    vi.stubGlobal('requestAnimationFrame', (callback: FrameRequestCallback) => {
      currentTime += 1
      callback(0)
      return 1
    })
    class FakeMediaRecorder {
      static isTypeSupported = vi.fn(() => true)
      ondataavailable: ((event: { data: Blob }) => void) | null = null
      onerror: (() => void) | null = null
      onstop: (() => void) | null = null

      constructor(_stream: MediaStream, _options: { mimeType: string }) {}

      start = vi.fn()

      stop = vi.fn(() => {
        this.ondataavailable?.({ data: new Blob(['encoded']) })
        this.onstop?.()
      })
    }
    vi.stubGlobal('MediaRecorder', FakeMediaRecorder)
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: vi.fn(() => 'blob:stage') })
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: vi.fn() })

    const exportPromise = exportAnnotatedMp4(
      new File(['video'], 'stage.mp4', { type: 'video/mp4' }),
      {
        durationSeconds: 8,
        candidates: [],
        sampledFrames: 1,
        trimRange: { startSeconds: 3, endSeconds: 5 },
        timerOriginSeconds: 3
      },
      DEFAULT_OVERLAY,
      '',
      'IPSC',
      {}
    )
    video.dispatchEvent(new Event('loadedmetadata'))

    await expect(exportPromise).resolves.toBeInstanceOf(Blob)
    expect(capturedTimes).toEqual([3])
    expect(audioTrack.stop).toHaveBeenCalled()
    expect(canvasTrack.stop).toHaveBeenCalled()
  })
})
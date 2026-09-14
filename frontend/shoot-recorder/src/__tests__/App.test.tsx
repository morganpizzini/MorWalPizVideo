import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../App'

vi.mock('@remotion/player', () => ({
  Player: ({ inputProps }: { inputProps: { scoringMode: string } }) => <div data-testid="remotion-preview">{inputProps.scoringMode}</div>
}))

vi.mock('../exportVideo', () => ({
  canExportMp4: () => true,
  downloadBlob: vi.fn(),
  exportAnnotatedMp4: vi.fn()
}))

vi.mock('../analysis', () => ({
  analyzeVideo: vi.fn().mockResolvedValue({
    durationSeconds: 10,
    candidates: [{ id: 'shot-1', timeSeconds: 2, confidence: 90, selected: true, source: 'analysis' }],
    sampledFrames: 100,
    trimRange: { startSeconds: 0, endSeconds: 10 },
    timerOriginSeconds: 0
  }),
  formatTime: (seconds: number) => seconds.toFixed(1),
  formatShotSeconds: (seconds: number) => seconds.toFixed(1)
}))

const loadVideo = async () => {
  const file = new File(['video'], 'stage.mp4', { type: 'video/mp4' })
  const input = screen.getByLabelText('Choose an MP4 video')
  fireEvent.change(input, { target: { files: [file] } })
  await waitFor(() => expect(document.querySelector('video')).toBeInTheDocument())
  const video = document.querySelector('video') as HTMLVideoElement
  Object.defineProperties(video, {
    duration: { configurable: true, value: 10 },
    videoWidth: { configurable: true, value: 1920 },
    videoHeight: { configurable: true, value: 1080 }
  })
  fireEvent.loadedMetadata(video)
}

beforeEach(() => {
  Object.defineProperty(URL, 'createObjectURL', { configurable: true, writable: true, value: vi.fn(() => 'blob:stage') })
  Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, writable: true, value: vi.fn() })
})

describe('Shoot Recorder', () => {
  it('explains the session-only behavior and provides the MP4 input', () => {
    render(<App />)
    expect(screen.getByText('Session-only workspace')).toBeInTheDocument()
    expect(screen.getByLabelText('Choose an MP4 video')).toHaveAttribute('accept', 'video/mp4,.mp4')
    expect(screen.getByText(/Reloading clears the video and analysis/)).toBeInTheDocument()
  })

  it('requires a confirmed trim before analysis, edits scores, and resets them after trim changes', async () => {
    render(<App />)
    await loadVideo()

    expect(screen.getByRole('button', { name: 'Confirm trim range' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Confirm trim to analyze' })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: 'Confirm trim range' }))
    expect(screen.getByRole('button', { name: 'Run analysis' })).toBeEnabled()
    fireEvent.click(screen.getByRole('button', { name: 'Run analysis' }))
    await screen.findByText('1 candidate spikes')

    const video = document.querySelector('video') as HTMLVideoElement
    Object.defineProperty(video, 'currentTime', { configurable: true, writable: true, value: 3 })
    fireEvent.timeUpdate(video)
    fireEvent.change(screen.getByLabelText('shot-1 points'), { target: { value: '12' } })
    fireEvent.change(screen.getByLabelText('shot-1 penalties'), { target: { value: '1' } })
    fireEvent.change(screen.getByLabelText('shot-1 misses'), { target: { value: '1' } })
    expect(screen.getByText('-8')).toBeInTheDocument()
    expect(screen.getByLabelText('IPSC hit factor thermometer')).toBeInTheDocument()

    fireEvent.click(screen.getByLabelText('IDPA'))
    expect(screen.getByText('IDPA summary')).toBeInTheDocument()
    expect(screen.queryByLabelText('IPSC hit factor thermometer')).not.toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Trim start'), { target: { value: '1' } })
    await waitFor(() => expect(screen.queryByText('1 candidate spikes')).not.toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Confirm trim range' })).toBeInTheDocument()
    expect(screen.queryByLabelText('shot-1 points')).not.toBeInTheDocument()
  })
})

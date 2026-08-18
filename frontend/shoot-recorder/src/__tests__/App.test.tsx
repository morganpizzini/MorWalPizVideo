import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { App } from '../App'

describe('Shoot Recorder', () => {
  it('explains the session-only behavior and provides the MP4 input', () => {
    render(<App />)
    expect(screen.getByText('Session-only workspace')).toBeInTheDocument()
    expect(screen.getByLabelText('Choose an MP4 video')).toHaveAttribute('accept', 'video/mp4,.mp4')
    expect(screen.getByText(/Reloading clears the video and analysis/)).toBeInTheDocument()
  })
})

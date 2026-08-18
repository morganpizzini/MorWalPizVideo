import { describe, expect, it } from 'vitest'
import { formatShotSeconds, formatTime } from '../analysis'
import { getSelectedShotTimings } from '../timeline'
import type { AnalysisResult } from '../types'

describe('shot timeline', () => {
  it('formats spike times with tenths of a second', () => {
    expect(formatTime(1.34)).toBe('1.3')
    expect(formatTime(61.98)).toBe('1:01.9')
    expect(formatShotSeconds(1)).toBe('1.0')
    expect(formatShotSeconds(0.3)).toBe('0.3')
  })

  it('calculates beep-relative times and splits', () => {
    const analysis: AnalysisResult = {
      durationSeconds: 5,
      sampledFrames: 50,
      startBeepSeconds: 1,
      candidates: [
        { id: 'first', timeSeconds: 1.3, confidence: 80, selected: true, source: 'analysis' },
        { id: 'ignored', timeSeconds: 1.5, confidence: 80, selected: false, source: 'analysis' },
        { id: 'second', timeSeconds: 1.6, confidence: 100, selected: true, source: 'manual' }
      ]
    }
    expect(getSelectedShotTimings(analysis).map((timing) => [timing.relativeSeconds, timing.splitSeconds])).toEqual([
      [0.3, 0.3],
      [0.6, 0.3]
    ])
  })

  it('shows only shots already reached and keeps the last four for the overlay', () => {
    const analysis: AnalysisResult = {
      durationSeconds: 10,
      sampledFrames: 100,
      startBeepSeconds: 0,
      candidates: Array.from({ length: 5 }, (_, index) => ({
        id: `shot-${index + 1}`,
        timeSeconds: index + 1,
        confidence: 80,
        selected: true,
        source: 'analysis' as const
      }))
    }
    expect(getSelectedShotTimings(analysis, 4.5).map((timing) => timing.candidate.id)).toEqual([
      'shot-1', 'shot-2', 'shot-3', 'shot-4'
    ])
    expect(getSelectedShotTimings(analysis, 6).map((timing) => timing.candidate.id)).toEqual([
      'shot-2', 'shot-3', 'shot-4', 'shot-5'
    ])
  })
})

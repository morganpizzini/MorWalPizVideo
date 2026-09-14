import { describe, expect, it } from 'vitest'
import { createTrimRange, isValidTrimRange, mapCompositionToSourceTime, mapSourceToCompositionTime, normalizeTrimRange, resolveTimerOrigin } from '../trim'

describe('trim and timeline mapping', () => {
  it('accepts inclusive boundaries while rejecting reversed or too-short ranges', () => {
    expect(isValidTrimRange({ startSeconds: 0, endSeconds: 4 }, 4)).toBe(true)
    expect(isValidTrimRange({ startSeconds: 2, endSeconds: 1 }, 4)).toBe(false)
    expect(isValidTrimRange({ startSeconds: 1, endSeconds: 1.05 }, 4)).toBe(false)
  })

  it('normalizes both handles without allowing them to cross', () => {
    expect(normalizeTrimRange({ startSeconds: -2, endSeconds: 99 }, 8)).toEqual({ startSeconds: 0, endSeconds: 8 })
    expect(normalizeTrimRange({ startSeconds: 7.9, endSeconds: 2 }, 8)).toEqual({ startSeconds: 7.9, endSeconds: 8 })
    expect(createTrimRange(8)).toEqual({ startSeconds: 0, endSeconds: 8 })
  })

  it('maps source timestamps through the selected composition range', () => {
    const range = { startSeconds: 12, endSeconds: 42 }
    expect(mapSourceToCompositionTime(12, range)).toBe(0)
    expect(mapSourceToCompositionTime(20, range)).toBe(8)
    expect(mapSourceToCompositionTime(50, range)).toBe(30)
    expect(mapCompositionToSourceTime(8, range)).toBe(20)
  })

  it('uses the trim start when the optional beep is absent or outside the range', () => {
    const range = { startSeconds: 12, endSeconds: 42 }
    expect(resolveTimerOrigin(range)).toBe(12)
    expect(resolveTimerOrigin(range, 4)).toBe(12)
    expect(resolveTimerOrigin(range, 20)).toBe(20)
  })
})
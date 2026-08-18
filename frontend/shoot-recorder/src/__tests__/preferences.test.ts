import { describe, expect, it, beforeEach } from 'vitest'
import { DEFAULT_OVERLAY, DEFAULT_PEAK_MULTIPLIER, PREFERENCES_KEY } from '../types'
import { loadPreferences, savePreferences } from '../preferences'

describe('shoot recorder preferences', () => {
  beforeEach(() => window.localStorage.clear())

  it('stores only overlay layout and social handler', () => {
    savePreferences({
      overlay: {
        shot: { x: 12, y: 8, width: 30, height: 16, color: '#ffffff', strokeColor: '#0b1220', strokeWidth: 2, fontSize: 45 },
        social: { x: 4, y: 82, width: 28, height: 12, color: '#f6b73c', strokeColor: '#0b1220', strokeWidth: 2, fontSize: 45 }
      },
      socialHandler: 'mysocialpage',
      peakMultiplier: DEFAULT_PEAK_MULTIPLIER
    })
    expect(JSON.parse(window.localStorage.getItem(PREFERENCES_KEY) ?? '')).toEqual({
      overlay: {
        shot: { x: 12, y: 8, width: 30, height: 16, color: '#ffffff', strokeColor: '#0b1220', strokeWidth: 2, fontSize: 45 },
        social: { x: 4, y: 82, width: 28, height: 12, color: '#f6b73c', strokeColor: '#0b1220', strokeWidth: 2, fontSize: 45 }
      },
      socialHandler: 'mysocialpage',
      peakMultiplier: DEFAULT_PEAK_MULTIPLIER
    })
  })

  it('falls back safely when stored preferences are invalid', () => {
    window.localStorage.setItem(PREFERENCES_KEY, '{"overlay":{"x":"bad"}}')
    expect(loadPreferences()).toEqual({ overlay: DEFAULT_OVERLAY, socialHandler: '', peakMultiplier: DEFAULT_PEAK_MULTIPLIER })
  })
})

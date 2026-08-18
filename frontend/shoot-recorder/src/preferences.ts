import {
  DEFAULT_OVERLAY,
  DEFAULT_SHOT_OVERLAY,
  DEFAULT_SOCIAL_OVERLAY,
  DEFAULT_PEAK_MULTIPLIER,
  PREFERENCES_KEY,
  type OverlayElement,
  type OverlayLayout,
  type ShootPreferences
} from './types'

const isOverlayElement = (value: unknown): value is Partial<OverlayElement> => {
  if (!value || typeof value !== 'object') return false
  const layout = value as Record<string, unknown>
  return ['x', 'y', 'width', 'height', 'color', 'strokeColor', 'strokeWidth']
    .every((key) => layout[key] === undefined || typeof layout[key] === (key === 'color' || key === 'strokeColor' ? 'string' : 'number'))
}

const isPositioned = (value: unknown): value is Pick<OverlayElement, 'x' | 'y' | 'width' | 'height'> => {
  if (!value || typeof value !== 'object') return false
  const layout = value as Record<string, unknown>
  return ['x', 'y', 'width', 'height'].every((key) => typeof layout[key] === 'number')
}

const readOverlay = (value: unknown): OverlayLayout => {
  if (value && typeof value === 'object') {
    const values = value as Record<string, unknown>
    if (isOverlayElement(values.shot) && isOverlayElement(values.social)) {
      return {
        shot: { ...DEFAULT_SHOT_OVERLAY, ...values.shot },
        social: { ...DEFAULT_SOCIAL_OVERLAY, ...values.social }
      }
    }
    if (isPositioned(value)) {
      return { shot: { ...DEFAULT_SHOT_OVERLAY, ...value }, social: DEFAULT_SOCIAL_OVERLAY }
    }
  }
  return DEFAULT_OVERLAY
}

export const loadPreferences = (): ShootPreferences => {
  if (typeof window === 'undefined') return { overlay: DEFAULT_OVERLAY, socialHandler: '', peakMultiplier: DEFAULT_PEAK_MULTIPLIER }
  try {
    const raw = window.localStorage.getItem(PREFERENCES_KEY)
    if (!raw) return { overlay: DEFAULT_OVERLAY, socialHandler: '', peakMultiplier: DEFAULT_PEAK_MULTIPLIER }
    const parsed: unknown = JSON.parse(raw)
    if (!parsed || typeof parsed !== 'object') return { overlay: DEFAULT_OVERLAY, socialHandler: '', peakMultiplier: DEFAULT_PEAK_MULTIPLIER }
    const values = parsed as Record<string, unknown>
    return {
      overlay: readOverlay(values.overlay),
      socialHandler: typeof values.socialHandler === 'string' ? values.socialHandler : '',
      peakMultiplier: typeof values.peakMultiplier === 'number'
        ? Math.min(3, Math.max(0.2, values.peakMultiplier))
        : DEFAULT_PEAK_MULTIPLIER
    }
  } catch {
    return { overlay: DEFAULT_OVERLAY, socialHandler: '', peakMultiplier: DEFAULT_PEAK_MULTIPLIER }
  }
}

export const savePreferences = (preferences: ShootPreferences): void => {
  window.localStorage.setItem(PREFERENCES_KEY, JSON.stringify(preferences))
}

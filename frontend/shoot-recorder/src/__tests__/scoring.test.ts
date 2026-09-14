import { describe, expect, it } from 'vitest'
import { calculateHitFactor, calculateIdpaScore, calculateIpscScore, clampThermometerValue, getScoreSummary, preserveShotScores } from '../scoring'

describe('session scoring', () => {
  it('applies the approved IPSC -10 value to penalties and misses', () => {
    expect(calculateIpscScore({ points: 30, penalties: 1, misses: 1 })).toBe(10)
    expect(getScoreSummary('IPSC', [{ points: 30, penalties: 1, misses: 1 }], 2)).toMatchObject({ totalScore: 10, hitFactor: 5, thermometerValue: 5 })
  })

  it('keeps IDPA as a points-down summary without hit factor', () => {
    expect(calculateIdpaScore({ points: 12, penalties: 2, misses: 1 })).toEqual({ pointsDown: 17, penaltySeconds: 6 })
    expect(getScoreSummary('IDPA', [{ points: 12, penalties: 2, misses: 1 }], 2)).toMatchObject({ totalScore: 17, hitFactor: null, idpaPointsDown: 17, idpaPenaltyCount: 2 })
  })

  it('decays hit factor as elapsed time grows and clamps the thermometer to 0..11', () => {
    expect(calculateHitFactor(44, 2)).toBe(22)
    expect(calculateHitFactor(44, 4)).toBe(11)
    expect(clampThermometerValue(22)).toBe(11)
    expect(clampThermometerValue(-1)).toBe(0)
    expect(clampThermometerValue(Number.NaN)).toBe(0)
  })

  it('preserves score edits by candidate id or nearby source timestamp', () => {
    const scores = preserveShotScores(
      { old: { points: 5, penalties: 1, misses: 0 } },
      [{ id: 'old', timeSeconds: 2 }],
      [{ id: 'new', timeSeconds: 2.04 }]
    )
    expect(scores.new).toEqual({ points: 5, penalties: 1, misses: 0 })
  })
})
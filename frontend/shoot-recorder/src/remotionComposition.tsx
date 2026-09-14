import { AbsoluteFill, useCurrentFrame, useVideoConfig } from 'remotion'
import { formatShotSeconds } from './analysis'
import { getOverlayCompositionData } from './composition'
import type { AnalysisResult, OverlayElement, ScoringMode, ShotScore } from './types'
import { mapCompositionToSourceTime } from './trim'

export interface RemotionCompositionProps {
  analysis: AnalysisResult
  scoringMode: ScoringMode
  scores: Record<string, ShotScore>
  socialHandler: string
  shotOverlay: OverlayElement
  socialOverlay: OverlayElement
}

const formatStroke = (width: number, color: string): string => {
  const offsets = [-width, 0, width]
  return offsets.flatMap((x) => offsets.map((y) => `${x}px ${y}px 0 ${color}`)).join(', ')
}

const overlayStyle = (element: OverlayElement, fontSize?: number): React.CSSProperties => ({
  left: `${element.x}%`,
  top: `${element.y}%`,
  width: `${element.width}%`,
  height: `${element.height}%`,
  color: element.color,
  textShadow: formatStroke(element.strokeWidth, element.strokeColor),
  ...(fontSize ? { fontSize: `${fontSize}px` } : {})
})

export function RemotionComposition({ analysis, scoringMode, scores, socialHandler, shotOverlay, socialOverlay }: RemotionCompositionProps) {
  const frame = useCurrentFrame()
  const { fps } = useVideoConfig()
  const compositionSeconds = frame / fps
  const data = getOverlayCompositionData(
    analysis,
    mapCompositionToSourceTime(compositionSeconds, analysis.trimRange),
    scoringMode,
    scores,
    socialHandler
  )
  const shotFontSize = `clamp(12px, ${shotOverlay.fontSize / 6}%, 48px)`
  const socialFontSize = `clamp(12px, ${socialOverlay.fontSize / 4}%, 48px)`

  return (
    <AbsoluteFill className="remotion-overlay" style={{ pointerEvents: 'none' }}>
      <div className="shot-overlay" style={{ ...overlayStyle(shotOverlay), fontSize: shotFontSize }}>
        <div className="shot-heading"><span>TIME</span><span>{formatShotSeconds(data.timerSeconds)}</span></div>
        {data.shotTimings.map((timing) => (
          <div className="shot-entry" key={timing.candidate.id}>
            <small>split {formatShotSeconds(timing.splitSeconds)}</small>
            <div><span>shoot {timing.shotNumber}</span><strong>{formatShotSeconds(timing.relativeSeconds)}</strong></div>
          </div>
        ))}
        <div className="score-summary">
          <span>{scoringMode === 'IPSC' ? 'SCORE' : 'IDPA SCORE'}</span>
          <strong>{scoringMode === 'IPSC' ? data.totalScore : `${data.idpaPointsDown ?? 0} down`}</strong>
        </div>
      </div>
      {scoringMode === 'IPSC' && data.thermometerValue !== null && (
        <div className="ipsc-thermometer" aria-label={`Hit factor ${data.hitFactor?.toFixed(2) ?? '0.00'}`}>
          <span style={{ height: `${data.thermometerValue / 11 * 100}%` }} />
          <b>{data.hitFactor?.toFixed(2) ?? '0.00'}</b>
        </div>
      )}
      {data.socialHandler && (
        <div className="social-overlay" style={{ ...overlayStyle(socialOverlay), fontSize: socialFontSize }}>
          <b>@{data.socialHandler}</b>
        </div>
      )}
    </AbsoluteFill>
  )
}
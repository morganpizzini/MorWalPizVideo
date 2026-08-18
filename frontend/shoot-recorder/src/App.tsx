import { useEffect, useMemo, useRef, useState } from 'react'
import type { ChangeEvent, CSSProperties } from 'react'
import { analyzeVideo, formatShotSeconds, formatTime } from './analysis'
import { canExportMp4, downloadBlob, exportAnnotatedMp4 } from './exportVideo'
import { loadPreferences, savePreferences } from './preferences'
import { getSelectedShotTimings } from './timeline'
import {
  DEFAULT_OVERLAY,
  type AnalysisResult,
  type OverlayElement,
  type OverlayLayout
} from './types'

type AnalysisStatus = 'idle' | 'analyzing' | 'ready' | 'error'
type VideoSize = { width: number; height: number }
type AudioStatus = 'unknown' | 'available' | 'unavailable'

const clamp = (value: number, minimum: number, maximum: number): number =>
  Math.min(maximum, Math.max(minimum, value))

const formatStroke = (width: number, color: string): string => {
  const offsets = [-width, 0, width]
  return offsets.flatMap((x) => offsets.map((y) => `${x}px ${y}px 0 ${color}`)).join(', ')
}

const overlayStyle = (element: OverlayElement, fontSize?: number): CSSProperties => ({
  left: `${element.x}%`,
  top: `${element.y}%`,
  width: `${element.width}%`,
  height: `${element.height}%`,
  color: element.color,
  textShadow: formatStroke(element.strokeWidth, element.strokeColor),
  ...(fontSize ? { fontSize: `${fontSize}px` } : {})
})

const displayHandler = (handler: string): string => {
  const cleanHandler = handler.trim().replace(/^@+/, '')
  return cleanHandler ? `@${cleanHandler}` : ''
}

type AudioInspectableVideo = HTMLVideoElement & {
  audioTracks?: { length: number }
  mozHasAudio?: boolean
  webkitAudioDecodedByteCount?: number
}

const detectAudioStatus = (video: HTMLVideoElement): AudioStatus => {
  const inspectable = video as AudioInspectableVideo
  if (inspectable.audioTracks) return inspectable.audioTracks.length > 0 ? 'available' : 'unavailable'
  if (typeof inspectable.mozHasAudio === 'boolean') return inspectable.mozHasAudio ? 'available' : 'unavailable'
  if (typeof inspectable.webkitAudioDecodedByteCount === 'number' && inspectable.webkitAudioDecodedByteCount > 0) return 'available'
  return 'unknown'
}

export function App() {
  const [file, setFile] = useState<File | null>(null)
  const [videoUrl, setVideoUrl] = useState('')
  const [videoSize, setVideoSize] = useState<VideoSize>({ width: 16, height: 9 })
  const [currentTime, setCurrentTime] = useState(0)
  const [analysis, setAnalysis] = useState<AnalysisResult | null>(null)
  const [status, setStatus] = useState<AnalysisStatus>('idle')
  const [error, setError] = useState('')
  const [exporting, setExporting] = useState(false)
  const [exportProgress, setExportProgress] = useState(0)
  const [analysisDebouncing, setAnalysisDebouncing] = useState(false)
  const [manualShotTime, setManualShotTime] = useState('')
  const [beepTime, setBeepTime] = useState('0')
  const [audioStatus, setAudioStatus] = useState<AudioStatus>('unknown')
  const [stagePixels, setStagePixels] = useState({ width: 0, height: 0 })
  const [preferences, setPreferences] = useState(loadPreferences)
  const videoRef = useRef<HTMLVideoElement>(null)
  const stageRef = useRef<HTMLDivElement>(null)
  const previousFileRef = useRef<File | null>(null)

  useEffect(() => {
    if (!file) return undefined
    const url = URL.createObjectURL(file)
    setVideoUrl(url)
    return () => URL.revokeObjectURL(url)
  }, [file])

  useEffect(() => {
    if (!file) return undefined
    let active = true
    const isNewFile = previousFileRef.current !== file
    previousFileRef.current = file
    const delay = isNewFile ? 0 : 2000
    setAnalysisDebouncing(!isNewFile)
    const timer = window.setTimeout(() => {
      if (!active) return
      setAnalysisDebouncing(false)
      setStatus('analyzing')
      setError('')
      void analyzeVideo(file, preferences.peakMultiplier, Number(beepTime) || 0)
        .then((result) => {
          if (active) {
            setAnalysis(result)
            setBeepTime(result.startBeepSeconds.toFixed(1))
            setStatus('ready')
          }
        })
        .catch((analysisError: unknown) => {
          if (active) {
            setStatus('error')
            setError(analysisError instanceof Error ? analysisError.message : 'Video analysis failed.')
          }
        })
    }, delay)
    return () => {
      active = false
      window.clearTimeout(timer)
    }
  }, [file, preferences.peakMultiplier])

  useEffect(() => {
    if (!stageRef.current || typeof ResizeObserver === 'undefined') return undefined
    const observer = new ResizeObserver(([entry]) => {
      if (entry) setStagePixels({ width: entry.contentRect.width, height: entry.contentRect.height })
    })
    observer.observe(stageRef.current)
    return () => observer.disconnect()
  }, [videoUrl])

  useEffect(() => {
    savePreferences(preferences)
  }, [preferences])

  const shotTimings = useMemo(
    () => analysis ? getSelectedShotTimings(analysis) : [],
    [analysis]
  )

  const updateOverlay = (key: keyof OverlayLayout, change: Partial<OverlayElement>) => {
    setPreferences((current) => ({
      ...current,
      overlay: {
        ...current.overlay,
        [key]: { ...current.overlay[key], ...change }
      }
    }))
  }

  const selectFile = (event: ChangeEvent<HTMLInputElement>) => {
    const nextFile = event.target.files?.[0]
    if (!nextFile) return
    if (nextFile.type !== 'video/mp4' && !nextFile.name.toLowerCase().endsWith('.mp4')) {
      setError('Please choose an MP4 video file.')
      return
    }
    setFile(nextFile)
    setAnalysis(null)
    setStatus('idle')
    setCurrentTime(0)
    setBeepTime('0')
    setAudioStatus('unknown')
    setError('')
  }

  const runAnalysis = async () => {
    if (!file) return
    setAnalysisDebouncing(false)
    setStatus('analyzing')
    setError('')
    try {
      const result = await analyzeVideo(file, preferences.peakMultiplier, Number(beepTime) || 0)
      setAnalysis(result)
      setBeepTime(result.startBeepSeconds.toFixed(1))
      setStatus('ready')
    } catch (analysisError) {
      setStatus('error')
      setError(analysisError instanceof Error ? analysisError.message : 'Video analysis failed.')
    }
  }

  const reanalyzeFromBeep = async (timeSeconds: number = Number(beepTime) || currentTime) => {
    if (!file || !analysis) return
    const safeTime = clamp(timeSeconds, 0, analysis.durationSeconds)
    setBeepTime(safeTime.toFixed(1))
    setAnalysisDebouncing(false)
    setStatus('analyzing')
    setError('')
    try {
      const result = await analyzeVideo(file, preferences.peakMultiplier, safeTime)
      setAnalysis(result)
      setCurrentTime(safeTime)
      if (videoRef.current) videoRef.current.currentTime = safeTime
      setStatus('ready')
    } catch (analysisError) {
      setStatus('error')
      setError(analysisError instanceof Error ? analysisError.message : 'Video analysis failed.')
    }
  }

  const toggleCandidate = (id: string) => {
    setAnalysis((current) => current && {
      ...current,
      candidates: current.candidates.map((candidate) =>
        candidate.id === id ? { ...candidate, selected: !candidate.selected } : candidate
      )
    })
  }

  const addManualShot = (time: number) => {
    if (!analysis || !Number.isFinite(time)) return
    const safeTime = clamp(time, 0, analysis.durationSeconds)
    if (analysis.candidates.some((candidate) => Math.abs(candidate.timeSeconds - safeTime) < 0.05)) {
      setError('A shot is already recorded at this time.')
      return
    }
    setAnalysis({
      ...analysis,
      candidates: [
        ...analysis.candidates,
        {
          id: `manual-${Date.now()}`,
          timeSeconds: safeTime,
          confidence: 100,
          selected: true,
          source: 'manual' as const
        }
      ].sort((left, right) => left.timeSeconds - right.timeSeconds)
    })
    setError('')
  }

  const addManualShotFromInput = () => {
    const time = Number(manualShotTime)
    if (!Number.isFinite(time)) {
      setError('Enter a video time in seconds before adding a shot.')
      return
    }
    addManualShot(time)
  }

  const exportVideo = async () => {
    if (!file || !analysis) return
    setExporting(true)
    setExportProgress(0)
    setError('')
    try {
      const blob = await exportAnnotatedMp4(
        file,
        analysis,
        preferences.overlay,
        preferences.socialHandler,
        (progress) => setExportProgress(progress)
      )
      downloadBlob(blob, `${file.name.replace(/\.mp4$/i, '')}-annotated.mp4`)
    } catch (exportError) {
      setError(exportError instanceof Error ? exportError.message : 'MP4 export failed.')
    } finally {
      setExporting(false)
    }
  }

  const seekTo = (timeSeconds: number) => {
    if (videoRef.current) {
      videoRef.current.currentTime = timeSeconds
      setCurrentTime(timeSeconds)
    }
  }

  const socialHandler = displayHandler(preferences.socialHandler)
  const controlsDisabled = exporting || status === 'analyzing'

  return (
    <main className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">IPSC / IDPA VIDEO TOOL</p>
          <h1>Shoot Recorder</h1>
        </div>
        <span className="session-badge">Session-only workspace</span>
      </header>

      <section className="intro panel">
        <div>
          <h2>Review a dynamic stage</h2>
          <p>Load an MP4, trace the range officer&apos;s beep, correct shot spikes, and export a timed overlay.</p>
        </div>
        <label className="file-picker">
          <span>{file ? 'Choose another MP4' : 'Choose an MP4'}</span>
          <input aria-label="Choose an MP4 video" type="file" accept="video/mp4,.mp4" disabled={controlsDisabled} onChange={selectFile} />
        </label>
      </section>

      {error && <div className="alert" role="alert">{error}</div>}

      <section className="workspace">
        <div className="panel preview-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">PREVIEW</p>
              <h2>{file?.name ?? 'No video loaded'}</h2>
            </div>
            {analysis && <span className="metric">{shotTimings.length} selected shots</span>}
          </div>
          <div ref={stageRef} className="video-stage" style={{ aspectRatio: `${videoSize.width} / ${videoSize.height}` }}>
            {videoUrl ? (
              <>
                <video
                  ref={videoRef}
                  controls={!exporting}
                  preload="auto"
                  src={videoUrl}
                  onLoadedMetadata={(event) => {
                    event.currentTarget.muted = false
                    event.currentTarget.volume = 1
                    setVideoSize({
                      width: event.currentTarget.videoWidth || 16,
                      height: event.currentTarget.videoHeight || 9
                    })
                    setAudioStatus(detectAudioStatus(event.currentTarget))
                  }}
                  onTimeUpdate={(event) => {
                    setCurrentTime(event.currentTarget.currentTime)
                    if (audioStatus === 'unknown') setAudioStatus(detectAudioStatus(event.currentTarget))
                  }}
                />
                {analysis && currentTime >= analysis.startBeepSeconds && (
                  <ShotOverlay analysis={analysis} currentTime={currentTime} stageHeight={stagePixels.height} element={preferences.overlay.shot} />
                )}
                {socialHandler && (
                  <div
                    className="social-overlay"
                    style={overlayStyle(
                      preferences.overlay.social,
                      stagePixels.height * preferences.overlay.social.height / 100 * preferences.overlay.social.fontSize / 100
                    )}
                  >
                    <b>{socialHandler}</b>
                  </div>
                )}
              </>
            ) : (
              <div className="empty-preview">
                <span className="target-mark">+</span>
                <p>Choose a stage video to begin</p>
              </div>
            )}
          </div>
          <p className={`audio-status ${audioStatus}`}>
            {audioStatus === 'unavailable'
              ? 'No audio track was detected in this MP4. Try an MP4 with AAC audio; browser codec support controls preview sound.'
              : "Preview audio uses the browser media player. Check the player volume and your browser's MP4 audio codec support."}
          </p>
          <div className="actions">
            <button className="primary-button" disabled={!file || controlsDisabled} onClick={() => void runAnalysis()}>
              {analysisDebouncing ? 'Analysis scheduled…' : status === 'analyzing' ? 'Analyzing at 0.1s…' : 'Run analysis'}
            </button>
            <button className="secondary-button" disabled={!file || !analysis || controlsDisabled || analysisDebouncing} onClick={() => void exportVideo()}>
              {exporting ? `Exporting MP4… ${Math.round(exportProgress)}%` : 'Export annotated MP4'}
            </button>
            {!canExportMp4() && <small className="hint">MP4 encoding is not available in this browser.</small>}
          </div>
          {exporting && (
            <progress className="export-progress" max="100" value={exportProgress} aria-label="Export progress">
              {exportProgress}%
            </progress>
          )}
        </div>

        <aside className="panel settings-panel">
          <p className="eyebrow">OVERLAY SETUP</p>
          <h2>Place and style overlays</h2>
          <label className="field">
            <span>Social handler</span>
            <input
              value={preferences.socialHandler}
              placeholder="mysocialpage"
              disabled={controlsDisabled}
              onChange={(event) => setPreferences((current) => ({ ...current, socialHandler: event.target.value }))}
            />
            <small>The @ prefix is added automatically and the handler is bold.</small>
          </label>
          <OverlayEditor
            label="Shot timeline"
            element={preferences.overlay.shot}
            disabled={controlsDisabled}
            onChange={(change) => updateOverlay('shot', change)}
          />
          <OverlayEditor
            label="Social handler"
            element={preferences.overlay.social}
            disabled={controlsDisabled}
            onChange={(change) => updateOverlay('social', change)}
          />
          <Slider
            label="Peak sensitivity"
            value={preferences.peakMultiplier}
            min={0.2}
            max={3}
            step={0.05}
            suffix="x"
            disabled={controlsDisabled}
            onChange={(value) => setPreferences((current) => ({ ...current, peakMultiplier: value }))}
          />
          <small className="field-hint">Lower values include more motion peaks; higher values filter more.</small>
          <div className="reset-actions">
            <button className="text-button" disabled={controlsDisabled} onClick={() => setPreferences((current) => ({ ...current, overlay: { ...current.overlay, shot: DEFAULT_OVERLAY.shot } }))}>
              Reset shot overlay
            </button>
            <button className="text-button" disabled={controlsDisabled} onClick={() => setPreferences((current) => ({ ...current, socialHandler: '', overlay: { ...current.overlay, social: DEFAULT_OVERLAY.social } }))}>
              Reset social settings
            </button>
          </div>
        </aside>
      </section>

      <section className={`panel timing-panel ${controlsDisabled ? 'section-busy' : ''}`}>
        <div className="panel-heading">
          <div>
            <p className="eyebrow">RANGE TIMER</p>
            <h2>Trace the first beep</h2>
          </div>
          {analysis && <span className="metric">Beep {formatTime(analysis.startBeepSeconds)}</span>}
        </div>
        <p className="muted">Pause the preview on the range officer&apos;s first beep, then set the start. Shot times and splits are measured from it with tenths of a second.</p>
        <div className="timing-controls">
          <button className="secondary-button" disabled={!analysis || controlsDisabled} onClick={() => void reanalyzeFromBeep(currentTime)}>
            Set current time and re-analyze ({formatTime(currentTime)})
          </button>
          <label className="compact-field">
            <span>Beep at video second</span>
            <input
              type="number"
              min="0"
              step="0.1"
              value={beepTime}
              disabled={!analysis || controlsDisabled}
              onChange={(event) => setBeepTime(event.target.value)}
            />
          </label>
          <button className="text-button" disabled={!analysis || controlsDisabled} onClick={() => void reanalyzeFromBeep()}>
            Re-analyze from entered beep
          </button>
        </div>
      </section>

      <section className={`panel results-panel ${controlsDisabled ? 'section-busy' : ''}`}>
        <div className="panel-heading">
          <div>
            <p className="eyebrow">SPIKE REVIEW</p>
            <h2>{analysis ? `${analysis.candidates.length} candidate spikes` : 'Analysis results'}</h2>
          </div>
          {analysis && <span className="metric">{formatTime(analysis.durationSeconds)} · {analysis.sampledFrames} frames sampled</span>}
        </div>
        {!analysis ? (
          <p className="muted">Run analysis to see potential shot moments. Review is editable because camera movement and nearby stages can create false positives.</p>
        ) : (
          <fieldset className="section-controls" disabled={controlsDisabled}>
            {analysis.candidates.length === 0 && <p className="muted">No motion spikes crossed the threshold. Add a shot manually below.</p>}
            <div className="candidate-list">
              {analysis.candidates.map((candidate) => (
                <label className={`candidate ${candidate.selected ? 'selected' : ''}`} key={candidate.id}>
                  <input type="checkbox" checked={candidate.selected} onChange={() => toggleCandidate(candidate.id)} />
                  <span className="candidate-time">{formatTime(candidate.timeSeconds)}</span>
                  <span><strong>{candidate.source === 'manual' ? 'manual shot' : candidate.id}</strong><small>{candidate.source === 'manual' ? 'manually added' : `${candidate.confidence}% motion confidence`}</small></span>
                  <button type="button" className="jump-button" onClick={() => seekTo(candidate.timeSeconds)}>Jump</button>
                </label>
              ))}
            </div>
            <div className="manual-shot">
              <label className="compact-field">
                <span>Add shot at video second</span>
                <input type="number" min="0" max={analysis.durationSeconds} step="0.1" placeholder={currentTime.toFixed(1)} value={manualShotTime} onChange={(event) => setManualShotTime(event.target.value)} />
              </label>
              <button className="secondary-button" onClick={addManualShotFromInput}>Add shot</button>
              <button className="text-button" onClick={() => addManualShot(currentTime)}>Add at current time ({formatTime(currentTime)})</button>
            </div>
          </fieldset>
        )}
      </section>

      <footer>
        <span>Nothing is uploaded. Reloading clears the video and analysis.</span>
        <span>Only overlay layout and social preferences are stored in this browser.</span>
      </footer>
    </main>
  )
}

function ShotOverlay({ analysis, currentTime, stageHeight, element }: { analysis: AnalysisResult; currentTime: number; stageHeight: number; element: OverlayElement }) {
  const timings = getSelectedShotTimings(analysis, currentTime)
  const fontSize = stageHeight > 0
    ? Math.max(12, stageHeight * element.height / 100 / 6 * element.fontSize / 100)
    : undefined
  return (
    <div className="shot-overlay" style={overlayStyle(element, fontSize)}>
      <div className="shot-heading"><span>TIME</span><span>{formatShotSeconds(currentTime - analysis.startBeepSeconds)}</span></div>
      {timings.map((timing) => (
        <div className="shot-entry" key={timing.candidate.id}>
          <small>split {formatShotSeconds(timing.splitSeconds)}</small>
          <div><span>shoot {timing.shotNumber}</span><strong>{formatShotSeconds(timing.relativeSeconds)}</strong></div>
        </div>
      ))}
    </div>
  )
}

function OverlayEditor({
  label,
  element,
  disabled,
  onChange
}: {
  label: string
  element: OverlayElement
  disabled: boolean
  onChange: (change: Partial<OverlayElement>) => void
}) {
  return (
    <details className="overlay-editor" open>
      <summary>{label}</summary>
      <fieldset className="editor-controls" disabled={disabled}>
        <Slider label="Horizontal" value={element.x} min={0} max={85} onChange={(value) => onChange({ x: value })} />
        <Slider label="Vertical" value={element.y} min={0} max={88} onChange={(value) => onChange({ y: value })} />
        <Slider label="Width" value={element.width} min={10} max={90} onChange={(value) => onChange({ width: value })} />
        <Slider label="Height" value={element.height} min={8} max={90} onChange={(value) => onChange({ height: value })} />
        <div className="color-fields">
          <label>Text color<input type="color" value={element.color} onChange={(event) => onChange({ color: event.target.value })} /></label>
          <label>Stroke color<input type="color" value={element.strokeColor} onChange={(event) => onChange({ strokeColor: event.target.value })} /></label>
        </div>
        <Slider label="Stroke width" value={element.strokeWidth} min={0} max={8} onChange={(value) => onChange({ strokeWidth: value })} suffix="px" />
        <Slider label="Font size" value={element.fontSize} min={20} max={100} step={1} onChange={(value) => onChange({ fontSize: value })} />
      </fieldset>
    </details>
  )
}

function Slider({
  label,
  value,
  min,
  max,
  step = 1,
  suffix = '%',
  disabled = false,
  onChange
}: {
  label: string
  value: number
  min: number
  max: number
  step?: number
  suffix?: string
  disabled?: boolean
  onChange: (value: number) => void
}) {
  return (
    <label className="slider-field">
      <span>{label}<output>{value}{suffix}</output></span>
      <input type="range" min={min} max={max} step={step} value={value} disabled={disabled} onChange={(event) => onChange(clamp(Number(event.target.value), min, max))} />
    </label>
  )
}

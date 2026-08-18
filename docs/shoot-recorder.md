# Shoot Recorder

## Purpose

Shoot Recorder is a standalone React Progressive Web App for reviewing IPSC and
IDPA action-stage videos. A user imports an MP4 showing a dynamic stage, runs a
browser-local motion analysis, reviews candidate shot spikes, and exports an
annotated MP4.

The first release is entirely frontend-only. It has no backend, authentication,
API calls, uploads, or server-side persistence.

## Repository and module map

The application is a Yarn Classic workspace registered in
`frontend/package.json` as `shoot-recorder`. The CI frontend build matrix in
`.github/workflows/ci.yml` builds it alongside the maintained frontend
applications.

| File | Responsibility |
|---|---|
| `frontend/shoot-recorder/src/App.tsx` | Session state, user workflow, preview, controls, and overlay editors |
| `frontend/shoot-recorder/src/types.ts` | App-local preferences, overlay, analysis, and candidate contracts |
| `frontend/shoot-recorder/src/preferences.ts` | Validated localStorage read/write and migration from earlier overlay shapes |
| `frontend/shoot-recorder/src/analysis.ts` | Video metadata, frame sampling, motion-spike detection, and time formatting |
| `frontend/shoot-recorder/src/timeline.ts` | Beep-relative shot timing, split calculation, shot numbering, and last-four filtering |
| `frontend/shoot-recorder/src/exportVideo.ts` | Canvas rendering, audio routing, MP4 MediaRecorder export, progress, and download |
| `frontend/shoot-recorder/src/styles.css` | Responsive layout and preview overlay styling |
| `frontend/shoot-recorder/vite.config.ts` | Vite, React, Vitest, and PWA configuration |
| `frontend/shoot-recorder/src/__tests__/` | Preference persistence, timeline arithmetic, formatting, and shell tests |

The app currently has a browser-router shell with a wildcard route. Keep
recorder-specific behavior inside this workspace. Do not move capture,
analysis, or overlay controls into `@morwalpizvideo/models`, `@morwalpizvideo/services`,
or `@morwalpiz/layout` unless a second application needs the same contract.

## User workflow

1. Choose an MP4 from the device.
2. Analysis starts automatically when the video metadata is ready. The browser
   samples video frames at 0.1-second intervals and compares consecutive frames
   to find motion spikes. The persisted peak-sensitivity control changes how
   far a motion score must rise above the average; lower values include more
   candidates. This is a candidate detector, not a certified shot-timer or
   scoring system.
3. Pause the preview on the range officer timer's first beep and set it as the
   start beep. The video is re-analyzed from that point, so the review list only
   contains spikes after the beep. Shot times and splits are then measured
   relative to that beep.
4. Review the candidate list. Candidates can be deselected when camera motion,
   a nearby stage, or another false positive was counted. The `Jump` control
   seeks the preview to a candidate. A manual shot can be added at any
   user-provided video time in 0.1-second increments.
5. Configure independent position, size, text color, stroke color, and stroke
   width for the shot timeline and social-handler overlays. Entering
   `mysocialpage` renders `@mysocialpage` in bold.
6. Export the selected analysis as an annotated MP4.

While the preview plays, the shot overlay timer advances from the beep and shows
the last four selected shots that have already occurred. The split is rendered
as a right-aligned line above each shot.

Changing peak sensitivity waits two seconds after the last slider change before
re-running analysis. This avoids starting a new analysis for every slider
movement. Analysis, export, range-timer controls, and spike-review controls are
locked while their operation is active. Separate reset buttons restore the shot
overlay or social-handler settings.

The UI is designed for desktop and mobile browsers. Touch-friendly controls and
responsive layout are required.

## Runtime lifecycle and control states

The selected `File` is the session identity. Selecting a new file clears the
analysis, current time, beep input, audio status, and video URL. The file-load
effect starts analysis immediately from video time `0`. A second analysis effect
handles peak-sensitivity changes:

1. The sensitivity slider is persisted immediately.
2. A two-second timer starts after the last slider event.
3. A later slider event cancels and replaces that timer.
4. The analysis starts with the current beep time after the timer expires.

The file-load path is not debounced. This preserves the requirement that a
selected video starts analysis without an extra user action.

`status` is `idle`, `analyzing`, `ready`, or `error`. `analysisDebouncing` is
separate from `status`, so the user can see that a sensitivity analysis is
scheduled without the app pretending that frame work has already started.

While `status === "analyzing"`:

- peak sensitivity is disabled;
- export is disabled;
- range-timer controls are disabled;
- spike-review checkboxes, jump buttons, and manual-shot controls are disabled.

While export is active, all user commands are disabled, including file
selection, native preview controls, social/overlay settings, resets, analysis,
timer controls, and spike review. The export progress percentage remains
visible.

## State and privacy

The selected file, object URL, video preview, analysis candidates, and export
state are held in React memory only. Reloading the page clears them and starts a
new session. The application must never upload or log video content.

The only persisted data is the overlay preference:

- shot and social overlay position: `x`, `y` percentages;
- shot and social overlay size: `width`, `height` percentages;
- shot and social overlay text/stroke colors and stroke widths;
- shot-timeline and social-handler font-size multipliers;
- peak sensitivity multiplier;
- the optional social handler string.

It is stored under `shoot-recorder.preferences` in localStorage. No video,
analysis result, session metadata, or export is stored in localStorage,
IndexedDB, Cache Storage, or another durable browser store.

The social handler is rendered as bold text with an automatic `@` prefix in the
preview and exported frames. The preview uses the source video's metadata
aspect ratio, including portrait videos, instead of forcing a 16:9 viewport.
The preview explicitly enables player audio and reports when the browser cannot
detect an audio track; an MP4 with a browser-supported audio codec (normally
AAC) is required.

### Persisted preference shape

`preferences.ts` stores one JSON object under `shoot-recorder.preferences`.
Values are layout percentages except for colors, stroke pixels, font-size
percentages, and the peak multiplier:

```json
{
  "overlay": {
    "shot": {
      "x": 4,
      "y": 4,
      "width": 30,
      "height": 42,
      "color": "#ffffff",
      "strokeColor": "#0b1220",
      "strokeWidth": 2,
      "fontSize": 45
    },
    "social": {
      "x": 4,
      "y": 82,
      "width": 28,
      "height": 12,
      "color": "#f6b73c",
      "strokeColor": "#0b1220",
      "strokeWidth": 2,
      "fontSize": 45
    }
  },
  "socialHandler": "",
  "peakMultiplier": 1.15
}
```

The reader merges missing style fields with defaults so preferences from the
earlier single-overlay shape remain usable. Keep migrations additive and do not
put media, candidate arrays, beep time, or export state into this object.

## Analysis algorithm

`analyzeVideo` creates a temporary video object URL and a 96x54 canvas. It:

1. waits for `loadedmetadata`;
2. chooses the requested start time (`0` for initial load or the selected beep);
3. samples at 0.1-second intervals, capped at 600 samples;
4. draws each frame and calculates normalized RGB difference from the previous
   sample;
5. computes the mean and standard deviation of all scores;
6. treats scores at or above
   `max(0.02, mean + deviation * peakMultiplier)` as candidates;
7. enforces a 0.08-second minimum candidate gap;
8. returns absolute video times, confidence, selected state, and source
   (`analysis` or `manual`).

The detector is a motion heuristic. It does not identify muzzle flash, shot
sound, plates, cards, the shooter, or stage geometry. A lower peak multiplier
produces more candidates; a higher value filters more motion. The current
control range is `0.2` to `3`.

Choosing the first beep calls `analyzeVideo` again with that video time as the
sampling start. The returned `startBeepSeconds` becomes the timer origin and
candidate review contains only samples from that point onward.

## Timeline and overlay rendering

Candidate `timeSeconds` remain absolute video times. `timeline.ts` derives:

- `relativeSeconds = candidate.timeSeconds - startBeepSeconds`;
- `splitSeconds = candidate.timeSeconds - previous selected candidate time`;
- `shotNumber`, assigned from the complete selected timeline before windowing.

Values are rounded/displayed to tenths. Times below one minute render as
`11.0`; the `m:ss.t` form is used only when minutes are required.

For the live preview and export, candidates later than the current playback time
are hidden and the last four already-reached candidates are retained. The
original `shotNumber` is preserved, so a window containing the tenth shot still
renders `shoot 10`, not `shoot 1`.

The shot overlay uses a fixed six-line logical grid: one line for the timer and
up to four shot entries plus spacing. The timer remains at the top of the
configured frame; shot entries flow below it. Each entry has a right-aligned
split line followed by a shot label and right-aligned relative time. Preview
font size is calculated from measured stage pixels and the persisted font-size
percentage. Export uses the same proportion against canvas pixels. Both paths
apply the same four-percent padding so text and strokes are not clipped.

The social overlay is independent from the shot overlay. Its position, size,
font size, color, stroke color, stroke width, and handler text can be reset
without resetting the shot timeline.

## MP4 export

Export uses a hidden canvas renderer and `MediaRecorder` with an MP4 MIME type
where the browser supports it. The source video's audio is routed into the
export stream through an `AudioContext` media destination where available, with
the video capture stream as a fallback. The
output is downloaded as
`<source-name>-annotated.mp4`. Browsers that cannot encode MP4 show an explicit
error instead of silently producing a different file format. MP4
`MediaRecorder` support varies; Safari and compatible Chromium configurations
should be tested on the target devices.

The exported shot overlay contains a live `TIME` heading, the last four shots
already reached by playback (`shoot 1`, `shoot 2`, etc.), their beep-relative
times, and right-aligned splits from the previous shot. Values under one minute
use seconds and tenths (for example `11.0`); minutes are added only when
needed. The red candidate marker is not exported. The export pipeline must
release object URLs, media tracks, and playback resources after completion or
failure. If the source audio track cannot be captured, export fails visibly
rather than silently producing a mute file.
The user-facing preview does not play the export renderer, and export progress
is reported as a percentage while frames are rendered.

Export must render the media a second time: the browser cannot add a canvas
overlay to an existing MP4 without decoding and encoding the frames. The source
video element is never added to the document, has no controls, and is routed
through an `AudioContext` media destination when that API is available. This
keeps the renderer out of the visible UI and prevents a second user-facing
preview while preserving audio for the output stream. A capture-stream audio
fallback is retained for browsers without `AudioContext`.

If the preview itself is silent, this is normally a browser decode/codec issue,
not an input-control issue. The player is explicitly unmuted and preloads media,
but the browser still needs to decode the MP4 audio codec. Test with AAC audio
in an MP4 container. The application does not transcode unsupported source
audio in the browser.

## Error and recovery behavior

- Invalid file extensions/types show an MP4 validation error.
- Decoder failures show a user-visible analysis or export error.
- Unsupported canvas or MP4 MediaRecorder capabilities disable/show an export
  capability message.
- Missing source audio causes export to fail visibly rather than produce an
  apparently successful mute file.
- Temporary object URLs, canvas streams, source streams, audio tracks, and
  audio contexts are released after export completion or failure.
- Reload, tab close, browser storage clearing, private browsing restrictions,
  device loss, and quota eviction can discard the session because media is
  intentionally not durable.

## PWA and hosting

The app is a Vite workspace at `frontend/shoot-recorder` and uses
`vite-plugin-pwa` with an auto-updating service worker and standalone manifest.
Only static application assets may be cached. The imported video and generated
media must not be treated as service-worker durable storage.

Production hosting must provide HTTPS (required for reliable media APIs and PWA
installation), SPA fallback to `index.html`, and correct JavaScript, manifest,
service-worker, SVG, and MP4 MIME handling.

The generated service worker should cache application assets only. Do not add
video files, media blobs, or user exports to the precache/runtime cache.

## Extension points

Keep the motion detector, candidate model, overlay preferences, and export
pipeline behind app-local modules so future improvements can add a stronger
computer-vision pass, audio analysis, stage metadata, or scoring without
introducing a backend dependency. Shared repository packages should not be
changed unless another application later consumes these contracts.

## Next-iteration priorities

Recommended order:

1. Add a browser test fixture or manual test asset with known video dimensions,
   AAC audio, a range-timer beep, and known shot times.
2. Add an audio waveform/beep detector to suggest the first beep, while keeping
   manual beep selection authoritative.
3. Replace or supplement frame-difference detection with audio peak detection,
   configurable smoothing, and per-stage/lighting calibration.
4. Preserve manual candidates when sensitivity re-analysis completes, marking
   them separately from newly detected candidates.
5. Add a waveform/timeline editor for precise tenth/hundredth-second candidate
   placement instead of relying only on native video controls.
6. Improve export compatibility with a tested WebCodecs or worker-based
   pipeline, while preserving an explicit MP4-only contract or adding a clearly
   labeled fallback format.
7. Add end-to-end browser coverage for preview audio, portrait video scaling,
   four-shot windowing, export progress, audio-bearing export, and PWA reload.
8. Add application deployment automation only after a hosting origin and
   secure-context policy are chosen.

Do not add a backend, upload endpoint, IndexedDB media store, or shared API
contract as a workaround for browser codec or storage limitations without a
separate product decision.

## Validation

- Build with `yarn workspace shoot-recorder build`.
- Run focused tests with `yarn workspace shoot-recorder test`.
- Manually test MP4 import, analysis, candidate selection, overlay persistence,
  reload reset, export capability errors, PWA installation, and mobile touch
  controls in Chromium and Safari.

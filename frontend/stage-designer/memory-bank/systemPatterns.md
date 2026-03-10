# System Patterns

## Architecture Overview

Single-page application with vanilla JavaScript class-based architecture. No framework dependencies - pure web standards implementation.

### Core Components

```
StageDesigner (Main Class)
├── Canvas Management
├── Tool System
├── Object Management
├── Stage Management
└── Export/Import System
```

## Key Technical Decisions

### Class-Based Architecture
- Single `StageDesigner` class manages entire application state
- Constructor initializes all subsystems
- Event-driven architecture with centralized event handlers

### Canvas Implementation
- HTML5 Canvas API for all rendering
- Transform-based zoom/pan system
- Immediate mode rendering (full redraw on each frame)

### Data Structure

#### Stage Object
```javascript
{
  id: timestamp,
  name: string,
  notes: string,
  drawings: Array<Drawing>,
  fills: Array<Fill>,
  objects: Array<Object>,
  measurements: Array<Measurement>
}
```

#### Drawing Object
```javascript
{
  points: Array<{x, y}>,
  color: string,
  width: number
}
```

#### Stage Object (Target/Pepper/etc)
```javascript
{
  id: timestamp,
  type: 'pepper' | 'plate' | 'target' | 'barricade',
  x: number,
  y: number,
  angle: number,
  size: number
}
```

#### Measurement Object
```javascript
{
  id: timestamp,
  start: {x, y},
  end: {x, y}
}
```

## Design Patterns in Use

### State Management
- Application state stored in class instance properties
- No external state management library
- LocalStorage for persistence
- All state mutations trigger `render()` and `saveToLocalStorage()`

### Event Handling
- Unified event handlers for mouse and touch
- Event normalization in handler methods
- Passive event listeners where appropriate
- Touch event `preventDefault()` to avoid scroll

### Tool Pattern
- Current tool stored as string identifier
- Tool-specific behavior in conditional branches
- Cursor updates reflect active tool
- Tool state persisted in toolbar UI

### Coordinate Transformation
```javascript
// Canvas coordinates → World coordinates
getCanvasPoint(clientX, clientY) {
  const rect = this.canvas.getBoundingClientRect();
  let x = (clientX - rect.left - this.translateX) / this.scale;
  let y = (clientY - rect.top - this.translateY) / this.scale;
  // Optional grid snapping
  if (this.snapToGrid) {
    x = Math.round(x / this.gridSize) * this.gridSize;
    y = Math.round(y / this.gridSize) * this.gridSize;
  }
  return { x, y };
}
```

### Rendering Pipeline
1. Clear canvas
2. Apply transformations (translate + scale)
3. Draw grid (if enabled)
4. Draw fills (background layer)
5. Draw drawings (line layer)
6. Draw objects (foreground layer)
7. Draw measurements (annotation layer)
8. Draw temporary elements (ruler preview, current path)

### Touch Handling Strategy
- Single touch: Tool-specific action (draw, select, place)
- Two-finger touch: Pan and zoom
- Touch distance calculation for pinch-to-zoom
- Midpoint calculation for two-finger pan

## Critical Implementation Paths

### Object Selection
1. User clicks canvas in select mode
2. Check rotation handle hit test first (if object selected)
3. Then check object hit tests (reverse order for top-most)
4. Set `selectedObject` and render with blue outline
5. Track drag offsets for smooth movement

### Object Rotation
1. Rotation handle drawn 20px above selected object
2. On mousedown: Check handle proximity (10px radius)
3. Calculate angle from object center to mouse
4. During drag: Update angle differential continuously
5. Rotation handle provides visual feedback

### Measurement Persistence
1. Ruler tool: First click sets start point
2. Move mouse shows temporary line
3. Second click sets end point
4. Measurement added to `currentStage.measurements` array
5. Measurements persist across tool changes
6. Eraser removes measurements on proximity hit

### Multi-Stage Management
1. Stages stored in array: `this.stages[]`
2. Current stage tracked by index: `this.currentStageIndex`
3. Tab UI generated from stages array
4. Switching stages clears selection but preserves zoom/pan
5. Each stage has independent drawing/object arrays

### Export Image Strategy
1. Create off-screen canvas at 1920×1080 with 2x DPI
2. Calculate bounding box of all content
3. Scale content to fit canvas area (preserve aspect ratio)
4. Center content in canvas
5. Render all stage elements
6. Add annotation panel with stage name/notes
7. Convert to blob and trigger download

## Component Relationships

### Toolbar ↔ Canvas
- Toolbar buttons update `currentTool` property
- Active tool reflected in button styling
- Canvas cursor updates based on tool
- Color/width pickers update drawing properties

### Sidebar ↔ Canvas
- Sidebar contains object library
- Drag & drop or click-to-place interaction
- Object placement uses transformed coordinates
- Sidebar closes after placement

### Bottom Panel ↔ Stage Data
- Panel displays current stage info
- Input changes directly update stage object
- Tab clicks switch `currentStageIndex`
- Changes trigger localStorage save

### Canvas ↔ LocalStorage
- All mutations call `saveToLocalStorage()`
- Data structure includes version number
- Load on initialization
- Automatic recovery on refresh

## Performance Considerations

### Rendering Optimization
- Full canvas redraw (acceptable for this scale)
- Transform-based rendering (GPU accelerated)
- No DOM manipulation during drawing
- Event throttling not needed (fast enough)

### Touch Performance
- `touch-action: none` on canvas
- Passive listeners where appropriate
- Immediate visual feedback
- No layout thrashing

### Data Size Management
- LocalStorage limit: ~5-10MB (browser dependent)
- Typical stage: <50KB JSON
- Multiple stages easily within limits
- Image export: Memory-only (no storage)

## Error Handling Patterns

### Graceful Degradation
```javascript
try {
  localStorage.setItem('ipsc-stages', JSON.stringify(data));
} catch (e) {
  console.error('Failed to save to localStorage:', e);
  // Continue operation - user can still export JSON
}
```

### Validation
- File import wrapped in try-catch
- Minimum distance threshold for measurements
- Bounds checking on zoom levels (0.1 to 5)
- Stage deletion prevents last stage removal

## Mobile-Specific Patterns

### Touch Disambiguation
- Two-finger = always pan/zoom
- One-finger = tool-specific action
- `pendingObjectType` for mobile placement
- Touch distance thresholds for gestures

### Responsive Sizing
- Canvas fills container (flex: 1)
- Resize handler recalculates dimensions
- Toolbar uses flexible wrapping
- Bottom panel max-height with scroll

### Viewport Configuration
```html
<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
<meta name="apple-mobile-web-app-capable" content="yes">
```

## Future Extensibility Points

### Adding New Tools
1. Add button to toolbar with `data-tool="newTool"`
2. Add case in `handleStart/Move/End` methods
3. Update `updateCanvasCursor()` for cursor style
4. Add rendering logic if needed

### Adding New Objects
1. Add to object library HTML
2. Add rendering case in `drawObject()`
3. Add preview style in CSS
4. No code changes needed in placement logic

### Export Formats
- Image export: Easily adaptable for PDF/SVG
- Data export: JSON structure is extensible
- Import: Version checking allows migration

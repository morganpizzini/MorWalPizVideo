# Active Context

## Current State

### Project Status
**Stage**: Fully functional production application

The IPSC Stage Designer is complete and operational with all core features implemented:
- ✅ Drawing tools (freehand, fill, eraser)
- ✅ Object library (peppers, plates, targets, barricades)
- ✅ Measurement system (ruler with persistent measurements)
- ✅ Multi-stage management
- ✅ Export/import functionality (JSON and PNG)
- ✅ Mobile and desktop support
- ✅ LocalStorage persistence

### Recent Work
Last session created the Memory Bank structure to document the project comprehensively.

## Current Focus

### Recent Development (Dec 31, 2025)
Just completed implementation of new IPSC target variants and visibility controls:
- Added 5 new object types (half target, mini target, no-shoot variants)
- Implemented visibility masking system for targets
- Added interactive visibility control UI

### Documentation Maintenance
The Memory Bank is now established and provides:
1. **Project Brief**: Core requirements and scope
2. **Product Context**: User needs and UX goals
3. **System Patterns**: Architecture and design patterns
4. **Tech Context**: Technologies and constraints
5. **Active Context**: Current state (this file)
6. **Progress**: Feature status (next file)

## Key Implementation Details

### Critical Code Sections

#### Coordinate Transformation
The `getCanvasPoint()` method is crucial for all interactions:
- Transforms screen coordinates to canvas world coordinates
- Accounts for pan (translateX/Y) and zoom (scale)
- Optional grid snapping for precision

#### Tool State Management
Tool switching is centralized:
- `currentTool` property stores active tool
- Event handlers branch based on tool type
- UI reflects tool state through `.active` class

#### Measurement Persistence
Unlike temporary drawing paths, measurements persist:
- Stored in `currentStage.measurements[]` array
- Survive tool changes
- Rendered on every frame
- Removable with eraser tool

#### Touch vs Mouse Handling
Unified event handling:
- `handleStart/Move/End` methods accept both event types
- Touch events normalized to single-point coordinates
- Two-finger gestures handled separately for pan/zoom

### Important Patterns to Remember

#### State Updates Always Trigger Saves
```javascript
// Pattern used throughout
this.currentStage.objects.push(obj);
this.render();
this.saveToLocalStorage();
```

#### Object Identification
Objects use timestamp-based IDs:
```javascript
id: Date.now()
```

#### Grid Snapping
Optional alignment to 10-pixel grid (1 meter):
```javascript
if (this.snapToGrid) {
  x = Math.round(x / this.gridSize) * this.gridSize;
  y = Math.round(y / this.gridSize) * this.gridSize;
}
```

### Mobile-Specific Considerations

#### Object Placement on Mobile
Desktop uses drag-and-drop, but mobile uses two-step process:
1. Tap object in sidebar → sets `pendingObjectType`
2. Tap canvas → places object at tap location
3. Cursor changes to indicate pending placement

#### Two-Finger Gestures
Pinch-to-zoom and pan:
```javascript
if (touches && touches.length === 2) {
  // Calculate distance for zoom
  // Calculate midpoint for pan
}
```

## Current Decisions

### Architecture Choices
- **No framework**: Keeps app lightweight and simple
- **Single class**: All state in one place, easier to reason about
- **Immediate mode rendering**: Full redraw is fast enough
- **LocalStorage only**: No backend needed, works offline

### UX Decisions
- **Mobile-first**: Touch interactions are primary design consideration
- **Persistent measurements**: Unlike drawings, ruler measurements don't disappear
- **Auto-save**: Every change saves to localStorage immediately
- **No confirmation dialogs**: Except for destructive actions (delete stage)

### Technical Decisions
- **Canvas over SVG**: Better for freehand drawing and performance
- **No undo/redo**: Use eraser instead (simpler implementation)
- **Fixed export size**: 1920×1080 @2x DPI is sufficient for most uses
- **Grid is 10px = 1m**: Standard IPSC scale works well

## Active Considerations

### User Feedback (if received)
- Monitor for feature requests
- Track any reported bugs
- Consider usability improvements

### Potential Enhancements
Ideas for future development (not currently planned):
1. **Undo/redo system**: Command pattern implementation
2. **Shape tools**: Rectangles, circles, arrows as drawing aids
3. **Layer system**: Separate layers for different elements
4. **Snap to objects**: Align objects to each other
5. **Copy/paste objects**: Duplicate existing objects
6. **Object grouping**: Select and move multiple objects
7. **Print optimization**: Better formatting for paper output
8. **PWA configuration**: Offline install capability
9. **SVG export**: Vector format for scalability
10. **Templates**: Pre-made stage layouts

### Known Issues
None currently reported. Application is stable.

### Browser Compatibility Notes
- Tested and working on Chrome, Firefox, Safari
- Mobile Safari (iOS) fully functional
- Chrome Mobile (Android) fully functional
- No IE/Legacy Edge support (by design)

## Next Steps

### If Bugs Found
1. Document the issue
2. Reproduce consistently
3. Identify affected code section
4. Implement fix
5. Test across devices
6. Update Memory Bank if patterns change

### If Features Added
1. Update projectbrief.md (scope change)
2. Update systemPatterns.md (new patterns)
3. Update activeContext.md (current state)
4. Update progress.md (new feature status)
5. Maintain code quality standards

### Memory Bank Updates
Update when:
- New patterns emerge
- Architecture changes
- Key decisions are made
- Features are added/removed
- User feedback reveals insights

## Project Insights

### What Works Well
1. **Simple architecture**: Single class is easy to understand
2. **Mobile experience**: Touch interactions feel natural
3. **Performance**: No lag even with many objects
4. **Data persistence**: localStorage works reliably
5. **Export quality**: PNG exports are professional

### Lessons Learned
1. **Coordinate transforms**: Getting canvas coordinates right is critical
2. **Touch events**: Requires careful handling of multi-touch
3. **Hit testing**: Order matters (check handles before objects)
4. **Mobile placement**: Drag-and-drop doesn't work well on mobile
5. **Measurement persistence**: Different from temporary drawing state

### Design Philosophy
- Favor simplicity over features
- Mobile experience is equal to desktop
- Performance is non-negotiable
- Data safety is paramount (auto-save everything)
- User shouldn't need instructions

## Context for Cline

When I (Cline) return to this project:

1. **Read ALL Memory Bank files** - they contain the complete project context
2. **Check activeContext.md first** - it has the current state
3. **Review systemPatterns.md** - for architecture understanding
4. **This is a complete, working application** - not in development
5. **Any changes should maintain the zero-dependency approach**
6. **Test on mobile after any changes** - mobile support is critical
7. **Update Memory Bank** - document any new patterns or decisions

### Quick Reference
- **Main class**: `StageDesigner` in index.js
- **Entry point**: DOMContentLoaded event listener at bottom of index.js
- **State location**: Instance properties of StageDesigner class
- **Persistence**: `saveToLocalStorage()` called after every change
- **Rendering**: `render()` method redraws entire canvas
- **Coordinate transform**: `getCanvasPoint(clientX, clientY)` method

# Product Context

## Why This Project Exists

IPSC shooting competitions require precise stage designs that must be documented and communicated clearly. Traditional methods involve:
- Hand-drawn diagrams on paper
- Generic drawing tools not suited for shooting stages
- Difficulty measuring distances accurately
- Challenges sharing designs digitally
- No standardized format for stage documentation

This tool solves these problems by providing a purpose-built, digital solution for IPSC stage design.

## Problems It Solves

### For Match Directors
- **Time Efficiency**: Quickly design multiple stages for a match
- **Accuracy**: Precise measurements ensure IPSC rule compliance
- **Documentation**: Professional exports for competitor briefings
- **Iteration**: Easy to modify designs and test variations

### For Course Designers
- **Visualization**: See the stage layout before construction
- **Object Placement**: Experiment with target positions and angles
- **Distance Verification**: Ensure shooting positions meet requirements
- **Collaboration**: Share JSON files for review and feedback

### For Range Officers
- **Reference Material**: Clear diagrams during stage setup
- **Measurements**: Built-in ruler for verification
- **Notes**: Stage-specific instructions and requirements

## How It Should Work

### User Flow
1. **Start**: Open application, see default stage
2. **Design**: Use tools to draw boundaries and place objects
3. **Measure**: Apply ruler tool to verify distances
4. **Document**: Add stage name and notes
5. **Save**: Auto-saved to browser, exportable as JSON/PNG
6. **Iterate**: Create additional stages using tabs

### Key Interactions

#### Tool Selection
- Click toolbar buttons to switch between tools
- Active tool is visually highlighted
- Canvas cursor changes to reflect tool mode

#### Drawing
- Click/tap and drag to draw freehand lines
- Lines follow current color and width settings
- Visual feedback during drawing

#### Object Placement
- Desktop: Drag objects from sidebar to canvas
- Mobile: Tap object in library, then tap canvas to place
- Objects can be moved, rotated, and deleted after placement

#### Selection & Manipulation
- Select tool: Click object to select (blue outline)
- Drag selected object to move
- Drag rotation handle (above object) to rotate
- Delete key removes selected object

#### Measurement
- Ruler tool: Click start point, move to end point, click again
- Displays distance in meters
- Measurements persist on canvas
- Eraser removes measurements

#### Canvas Navigation
- Desktop: Mouse wheel to zoom, drag background to pan
- Mobile: Pinch to zoom, two-finger drag to pan
- Reset button returns to default view

### User Experience Goals

#### Simplicity
- Minimal learning curve
- Intuitive iconography
- Clear tool states
- Immediate visual feedback

#### Reliability
- Auto-save prevents data loss
- LocalStorage backup
- Consistent behavior across devices
- Error-free exports

#### Flexibility
- Works on phone, tablet, and desktop
- Portrait and landscape orientations
- Offline capable
- Multiple export formats

#### Professional Output
- Clean, clear exported images
- Properly scaled diagrams
- Annotated with stage info
- Print-ready quality

## Design Principles

1. **Mobile-First**: Touch interactions are primary, mouse is secondary
2. **Forgiving**: Easy undo via eraser, no destructive actions without confirmation
3. **Transparent**: Scale and measurements always visible
4. **Efficient**: Common actions require minimal steps
5. **Persistent**: Work is never lost unexpectedly

## Success Metrics

### Usability
- Can create basic stage in under 2 minutes
- All tools discoverable without instructions
- Mobile users have equivalent functionality
- Zero crashes or data loss

### Adoption Indicators
- Users create multiple stages per session
- Export features are regularly used
- Users return to edit saved designs
- Designs are shared within IPSC community

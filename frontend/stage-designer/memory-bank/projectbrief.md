# Project Brief: IPSC Stage Designer

## Project Overview
A web-based stage designer for IPSC (International Practical Shooting Confederation) competitions. This tool allows match directors and course designers to create, visualize, and document shooting stages with precision.

## Core Purpose
Enable users to design IPSC shooting stages by:
- Drawing stage layouts with lines and filled areas
- Placing shooting objects (targets, peppers, plates, barricades)
- Measuring distances between elements
- Managing multiple stages in a single session
- Exporting designs as JSON (data) or PNG (visual)

## Target Users
- IPSC match directors
- Course designers
- Range officers
- Competition organizers

## Key Requirements

### Essential Features
1. **Drawing Tools**
   - Freehand drawing for stage boundaries
   - Fill tool for designated areas
   - Eraser for corrections
   - Line color and width customization

2. **Object Placement**
   - Peppers (circular shooting targets)
   - Plates (square reactive targets)
   - Targets (rectangular paper targets with rotation)
   - Barricades (obstacles/cover)
   - Drag-and-drop or tap-to-place interface

3. **Measurement System**
   - Ruler tool for distance measurements
   - Real-world scale conversion (pixels to meters)
   - Grid system with snap-to-grid option
   - Persistent measurement lines

4. **Stage Management**
   - Multiple stages per session
   - Stage naming and notes
   - Tab-based navigation between stages
   - Add/delete stage functionality

5. **Canvas Controls**
   - Pan and zoom
   - Grid toggle
   - Touch and mouse support
   - Mobile-responsive design

6. **Data Management**
   - Export stage designs to JSON
   - Import previously saved designs
   - Export visual PNG images with annotations
   - LocalStorage auto-save

### Technical Constraints
- Pure HTML/CSS/JavaScript (no framework dependencies)
- Mobile-first, touch-enabled interface
- Works offline after initial load
- Browser-based (no backend required)

## Success Criteria
- Intuitive tool for both desktop and mobile users
- Accurate distance measurements
- Professional-quality exported images
- Reliable data persistence
- Smooth performance on mobile devices

## Out of Scope (Current Version)
- 3D visualization
- Collaborative editing
- Cloud storage/sync
- Print optimization
- Advanced shape libraries

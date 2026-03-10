// IPSC Stage Designer - Main Application
class StageDesigner {
    constructor() {
        this.canvas = document.getElementById('mainCanvas');
        this.ctx = this.canvas.getContext('2d');

        // Canvas state
        this.scale = 1;
        this.translateX = 0;
        this.translateY = 0;
        this.pixelsPerMeter = 10; // 1 meter = 10 pixels at scale 1

        // Drawing state
        this.currentTool = 'select';
        this.isDrawing = false;
        this.currentPath = [];
        this.lineColor = '#000000';
        this.fillColor = '#ffff00';
        this.lineWidth = 2;

        // Grid settings
        this.showGrid = false;
        this.snapToGrid = false;
        this.gridSize = 10; // pixels (1 meter)

        // Objects and stages
        this.stages = [];
        this.currentStageIndex = 0;
        this.selectedObject = null;
        this.draggingObject = null;
        this.rotatingObject = false;
        this.rotationStartAngle = 0;
        this.dragRotateHistorySaved = false;

        // Touch/Mouse tracking
        this.lastX = 0;
        this.lastY = 0;
        this.isPanning = false;
        this.touchStartDistance = 0;
        this.initialScale = 1;

        // Ruler tool state
        this.rulerStart = null;
        this.rulerEnd = null;

        // Line tool state
        this.lineStart = null;
        this.lineEnd = null;
        this.lineDrawingActive = false;

        // Mobile object placement
        this.pendingObjectType = null;

        // Undo history
        this.historyStack = [];
        this.maxHistorySize = 50;

        this.init();
    }

    init() {
        this.resizeCanvas();
        window.addEventListener('resize', () => this.resizeCanvas());

        // Create initial stage
        this.addStage();

        this.setupEventListeners();
        this.render();

        // Load from localStorage if available
        this.loadFromLocalStorage();
        
        // Restore collapsed state for stage info
        const isCollapsed = localStorage.getItem('ipsc-stage-info-collapsed') === 'true';
        if (isCollapsed) {
            document.getElementById('stageInfo').classList.add('collapsed');
            document.getElementById('collapseInfoBtn').classList.add('collapsed');
        }
    }

    resizeCanvas() {
        const container = this.canvas.parentElement;
        this.canvas.width = container.clientWidth;
        this.canvas.height = container.clientHeight;
        this.render();
    }

    setupEventListeners() {
        // Tool buttons
        document.querySelectorAll('[data-tool]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                document.querySelectorAll('[data-tool]').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                this.currentTool = btn.dataset.tool;
                this.updateCanvasCursor();
            });
        });

        // Color pickers
        document.getElementById('colorPicker').addEventListener('input', (e) => {
            this.lineColor = e.target.value;
        });

        document.getElementById('fillColorPicker').addEventListener('input', (e) => {
            this.fillColor = e.target.value;
        });

        document.getElementById('lineWidth').addEventListener('input', (e) => {
            this.lineWidth = parseInt(e.target.value);
        });

        // Grid and snap
        document.getElementById('gridToggle').addEventListener('click', () => {
            this.showGrid = !this.showGrid;
            document.getElementById('gridToggle').classList.toggle('active', this.showGrid);
            this.render();
        });

        document.getElementById('snapToggle').addEventListener('click', () => {
            this.snapToGrid = !this.snapToGrid;
            document.getElementById('snapToggle').classList.toggle('active', this.snapToGrid);
        });

        // Zoom controls
        document.getElementById('zoomIn').addEventListener('click', () => this.zoom(1.2));
        document.getElementById('zoomOut').addEventListener('click', () => this.zoom(0.8));
        document.getElementById('zoomReset').addEventListener('click', () => this.resetView());

        // Object library
        document.getElementById('objectLibraryBtn').addEventListener('click', () => {
            document.getElementById('objectLibrary').classList.toggle('open');
        });

        document.getElementById('closeSidebar').addEventListener('click', () => {
            document.getElementById('objectLibrary').classList.remove('open');
        });

        // Panel toggle
        document.getElementById('togglePanel').addEventListener('click', () => {
            document.getElementById('bottomPanel').classList.toggle('collapsed');
            // Resize canvas after panel animation to fill freed space
            setTimeout(() => this.resizeCanvas(), 300);
        });

        // Stage info collapse toggle
        document.getElementById('collapseInfoBtn').addEventListener('click', () => {
            const stageInfo = document.getElementById('stageInfo');
            const collapseBtn = document.getElementById('collapseInfoBtn');
            stageInfo.classList.toggle('collapsed');
            collapseBtn.classList.toggle('collapsed');
            
            // Save collapsed state to localStorage
            const isCollapsed = stageInfo.classList.contains('collapsed');
            localStorage.setItem('ipsc-stage-info-collapsed', isCollapsed);
        });

        // Stage management
        document.getElementById('addStage').addEventListener('click', () => this.addStage());
        document.getElementById('deleteStage').addEventListener('click', () => this.deleteStage());

        // Stage info
        document.getElementById('stageName').addEventListener('input', (e) => {
            if (this.currentStage) {
                this.currentStage.name = e.target.value;
                this.updateStageTabs();
                this.saveToLocalStorage();
            }
        });

        document.getElementById('stageNotes').addEventListener('input', (e) => {
            if (this.currentStage) {
                this.currentStage.notes = e.target.value;
                this.saveToLocalStorage();
            }
        });

        // Export/Import
        document.getElementById('exportJson').addEventListener('click', () => this.exportJSON());
        document.getElementById('importJson').addEventListener('click', () => {
            document.getElementById('fileInput').click();
        });

        document.getElementById('fileInput').addEventListener('change', (e) => {
            this.importJSON(e.target.files[0]);
        });

        document.getElementById('exportImage').addEventListener('click', () => this.exportImage());

        // Canvas events - Mouse
        this.canvas.addEventListener('mousedown', (e) => this.handleStart(e));
        this.canvas.addEventListener('mousemove', (e) => this.handleMove(e));
        this.canvas.addEventListener('mouseup', (e) => this.handleEnd(e));
        this.canvas.addEventListener('wheel', (e) => this.handleWheel(e));

        // Canvas events - Touch
        this.canvas.addEventListener('touchstart', (e) => this.handleStart(e), { passive: false });
        this.canvas.addEventListener('touchmove', (e) => this.handleMove(e), { passive: false });
        this.canvas.addEventListener('touchend', (e) => this.handleEnd(e), { passive: false });

        // Drag and drop for objects
        document.querySelectorAll('.object-item').forEach(item => {
            item.addEventListener('dragstart', (e) => {
                e.dataTransfer.setData('objectType', item.dataset.object);
            });

            // Touch alternative for mobile
            item.addEventListener('click', (e) => {
                e.stopPropagation();
                this.pendingObjectType = item.dataset.object;
                document.getElementById('objectLibrary').classList.remove('open');
                // Visual feedback
                this.canvas.style.cursor = 'copy';
                setTimeout(() => {
                    if (this.pendingObjectType) {
                        this.canvas.style.cursor = '';
                        this.pendingObjectType = null;
                    }
                }, 10000); // Clear after 10 seconds if not placed
            });
        });

        this.canvas.addEventListener('drop', (e) => {
            e.preventDefault();
            const objectType = e.dataTransfer.getData('objectType');
            this.addObjectToCanvas(objectType, e.clientX, e.clientY);
        });

        this.canvas.addEventListener('dragover', (e) => e.preventDefault());

        // Undo button
        document.getElementById('undoBtn').addEventListener('click', () => this.undo());

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Delete') {
                if (this.selectedObject) {
                    this.deleteSelectedObject();
                }
            } else if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
                e.preventDefault();
                this.undo();
            }
        });
    }

    get currentStage() {
        return this.stages[this.currentStageIndex];
    }

    addStage() {
        const stageNumber = this.stages.length + 1;
        const newStage = {
            id: Date.now(),
            name: `Stage ${stageNumber}`,
            notes: '',
            drawings: [],
            fills: [],
            objects: [],
            measurements: []
        };

        this.stages.push(newStage);
        this.currentStageIndex = this.stages.length - 1;
        this.updateStageTabs();
        this.updateStageInfo();
        this.render();
        this.saveToLocalStorage();
    }

    deleteStage() {
        if (this.stages.length === 1) {
            alert('Cannot delete the last stage');
            return;
        }

        if (confirm(`Delete "${this.currentStage.name}"?`)) {
            this.stages.splice(this.currentStageIndex, 1);
            this.currentStageIndex = Math.max(0, this.currentStageIndex - 1);
            this.updateStageTabs();
            this.updateStageInfo();
            this.render();
            this.saveToLocalStorage();
        }
    }

    switchStage(index) {
        this.currentStageIndex = index;
        this.selectedObject = null;
        this.updateStageTabs();
        this.updateStageInfo();
        this.render();
    }

    updateStageTabs() {
        const container = document.getElementById('stageTabs');
        container.innerHTML = '';

        this.stages.forEach((stage, index) => {
            const tab = document.createElement('button');
            tab.className = 'stage-tab';
            if (index === this.currentStageIndex) {
                tab.classList.add('active');
            }
            tab.textContent = stage.name || `Stage ${index + 1}`;
            tab.addEventListener('click', () => this.switchStage(index));
            container.appendChild(tab);
        });
    }

    updateStageInfo() {
        if (this.currentStage) {
            document.getElementById('stageName').value = this.currentStage.name;
            document.getElementById('stageNotes').value = this.currentStage.notes;
        }
    }

    updateCanvasCursor() {
        this.canvas.className = `cursor-${this.currentTool}`;
    }

    getCanvasPoint(clientX, clientY) {
        const rect = this.canvas.getBoundingClientRect();
        let x = (clientX - rect.left - this.translateX) / this.scale;
        let y = (clientY - rect.top - this.translateY) / this.scale;

        if (this.snapToGrid) {
            x = Math.round(x / this.gridSize) * this.gridSize;
            y = Math.round(y / this.gridSize) * this.gridSize;
        }

        return { x, y };
    }

    handleStart(e) {
        e.preventDefault();

        const touches = e.touches;

        // Two-finger touch for pan/zoom
        if (touches && touches.length === 2) {
            this.isPanning = true;
            this.touchStartDistance = this.getTouchDistance(touches[0], touches[1]);
            this.initialScale = this.scale;
            const midpoint = this.getTouchMidpoint(touches[0], touches[1]);
            this.lastX = midpoint.x;
            this.lastY = midpoint.y;
            return;
        }

        const clientX = touches ? touches[0].clientX : e.clientX;
        const clientY = touches ? touches[0].clientY : e.clientY;
        const point = this.getCanvasPoint(clientX, clientY);

        this.lastX = clientX;
        this.lastY = clientY;

        // Handle pending object placement from mobile
        if (this.pendingObjectType) {
            this.addObjectToCanvas(this.pendingObjectType, clientX, clientY);
            this.pendingObjectType = null;
            this.canvas.style.cursor = '';
            return;
        }

        if (this.currentTool === 'select') {
            // Check if clicking on visibility control buttons first (for targets only)
            if (this.selectedObject && this.isTargetType(this.selectedObject)) {
                const visibilityClick = this.checkVisibilityControlClick(point.x, point.y);
                if (visibilityClick) {
                    this.saveStateToHistory();
                    this.selectedObject.visibility = visibilityClick;
                    this.render();
                    this.saveToLocalStorage();
                    return;
                }
            }

            // Check if clicking on rotation handle
            if (this.selectedObject) {
                const handleX = this.selectedObject.x;
                const handleY = this.selectedObject.y - this.selectedObject.size - 20;
                const dx = point.x - handleX;
                const dy = point.y - handleY;
                const distToHandle = Math.sqrt(dx * dx + dy * dy);

                if (distToHandle <= 10) {
                    this.saveStateToHistory();
                    this.rotatingObject = true;
                    this.dragRotateHistorySaved = true;
                    this.rotationStartAngle = Math.atan2(point.y - this.selectedObject.y, point.x - this.selectedObject.x) * 180 / Math.PI;
                    return;
                }
            }

            // Check if clicking on object
            const obj = this.getObjectAt(point.x, point.y);
            if (obj) {
                this.selectedObject = obj;
                this.draggingObject = obj;
                this.dragOffsetX = point.x - obj.x;
                this.dragOffsetY = point.y - obj.y;
                this.dragRotateHistorySaved = false; // Will save on first move
            } else {
                this.selectedObject = null;
                // Start panning if not on object
                if (!touches || touches.length === 1) {
                    this.isPanning = true;
                }
            }
            this.render();
        } else if (this.currentTool === 'draw') {
            this.isDrawing = true;
            this.currentPath = [point];
        } else if (this.currentTool === 'fill') {
            this.floodFill(point.x, point.y);
        } else if (this.currentTool === 'eraser') {
            this.eraseAt(point.x, point.y);
        } else if (this.currentTool === 'ruler') {
            if (!this.rulerStart) {
                // First click - start the measurement
                this.rulerStart = point;
                this.rulerEnd = null;
            } else {
                // Second click - end the measurement
                this.rulerEnd = point;
            }
        } else if (this.currentTool === 'line') {
            if (!this.lineStart) {
                // First click - start the line
                this.lineStart = point;
                this.lineEnd = null;
                this.lineDrawingActive = true;
            } else if (this.lineDrawingActive) {
                // Second click - complete the line and STOP
                this.saveStateToHistory();
                this.lineEnd = point;
                this.currentStage.drawings.push({
                    points: [{ ...this.lineStart }, { ...this.lineEnd }],
                    color: this.lineColor,
                    width: this.lineWidth
                });
                this.saveToLocalStorage();
                // Reset - don't continue to next line
                this.lineStart = null;
                this.lineEnd = null;
                this.lineDrawingActive = false;
                this.render();
            }
        }
    }

    handleMove(e) {
        e.preventDefault();

        const touches = e.touches;

        // Two-finger pinch/zoom
        if (touches && touches.length === 2) {
            const currentDistance = this.getTouchDistance(touches[0], touches[1]);
            const scaleFactor = currentDistance / this.touchStartDistance;
            this.scale = this.initialScale * scaleFactor;
            this.scale = Math.max(0.1, Math.min(5, this.scale));

            // Pan
            const midpoint = this.getTouchMidpoint(touches[0], touches[1]);
            const dx = midpoint.x - this.lastX;
            const dy = midpoint.y - this.lastY;
            this.translateX += dx;
            this.translateY += dy;
            this.lastX = midpoint.x;
            this.lastY = midpoint.y;

            this.updateScaleDisplay();
            this.render();
            return;
        }

        const clientX = touches ? touches[0].clientX : e.clientX;
        const clientY = touches ? touches[0].clientY : e.clientY;
        const point = this.getCanvasPoint(clientX, clientY);

        if (this.isPanning) {
            const dx = clientX - this.lastX;
            const dy = clientY - this.lastY;
            this.translateX += dx;
            this.translateY += dy;
            this.lastX = clientX;
            this.lastY = clientY;
            this.render();
        } else if (this.rotatingObject && this.selectedObject) {
            const currentAngle = Math.atan2(point.y - this.selectedObject.y, point.x - this.selectedObject.x) * 180 / Math.PI;
            let angleDiff = currentAngle - this.rotationStartAngle;
            this.selectedObject.angle += angleDiff;
            this.rotationStartAngle = currentAngle;
            this.render();
            this.saveToLocalStorage();
        } else if (this.currentTool === 'select' && this.draggingObject && !this.rotatingObject) {
            // Save history once at the start of dragging
            if (!this.dragRotateHistorySaved) {
                this.saveStateToHistory();
                this.dragRotateHistorySaved = true;
            }
            this.draggingObject.x = point.x - this.dragOffsetX;
            this.draggingObject.y = point.y - this.dragOffsetY;
            this.render();
            this.saveToLocalStorage();
        } else if (this.currentTool === 'draw' && this.isDrawing) {
            this.currentPath.push(point);
            this.render();
        } else if (this.currentTool === 'eraser' && e.buttons === 1) {
            this.eraseAt(point.x, point.y);
        } else if (this.currentTool === 'ruler' && this.rulerStart) {
            this.rulerEnd = point;
            this.render();
        } else if (this.currentTool === 'line' && this.lineStart) {
            this.lineEnd = point;
            this.render();
        }
    }

    handleEnd(e) {
        e.preventDefault();

        if (this.isDrawing && this.currentPath.length > 1) {
            this.saveStateToHistory();
            this.currentStage.drawings.push({
                points: [...this.currentPath],
                color: this.lineColor,
                width: this.lineWidth
            });
            this.saveToLocalStorage();
        }

        this.isDrawing = false;
        this.isPanning = false;
        this.draggingObject = null;
        this.rotatingObject = false;
        this.currentPath = [];

        if (this.currentTool === 'ruler' && this.rulerStart && this.rulerEnd) {
            // Save ruler as persistent measurement
            const dx = this.rulerEnd.x - this.rulerStart.x;
            const dy = this.rulerEnd.y - this.rulerStart.y;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            if (distance > 5) { // Minimum distance threshold
                this.saveStateToHistory();
                this.currentStage.measurements.push({
                    id: Date.now(),
                    start: { ...this.rulerStart },
                    end: { ...this.rulerEnd }
                });
                this.saveToLocalStorage();
            }
            this.rulerStart = null;
            this.rulerEnd = null;
        }
    }

    handleWheel(e) {
        e.preventDefault();
        const delta = e.deltaY > 0 ? 0.9 : 1.1;
        this.zoom(delta, e.clientX, e.clientY);
    }

    zoom(factor, centerX = null, centerY = null) {
        const oldScale = this.scale;
        this.scale *= factor;
        this.scale = Math.max(0.1, Math.min(5, this.scale));

        if (centerX !== null && centerY !== null) {
            const rect = this.canvas.getBoundingClientRect();
            const x = centerX - rect.left;
            const y = centerY - rect.top;

            this.translateX = x - (x - this.translateX) * (this.scale / oldScale);
            this.translateY = y - (y - this.translateY) * (this.scale / oldScale);
        }

        this.updateScaleDisplay();
        this.render();
    }

    resetView() {
        this.scale = 1;
        this.translateX = 0;
        this.translateY = 0;
        this.updateScaleDisplay();
        this.render();
    }

    updateScaleDisplay() {
        const zoomPercent = Math.round(this.scale * 100);
        document.getElementById('zoomReset').textContent = `${zoomPercent}%`;

        const metersPerPixel = 0.1 / this.scale;
        const scaleRatio = Math.round(1 / (metersPerPixel * this.pixelsPerMeter));
        document.getElementById('scaleDisplay').textContent =
            `Scale: 1:${scaleRatio} (1px = ${metersPerPixel.toFixed(2)}m)`;
    }

    getTouchDistance(touch1, touch2) {
        const dx = touch2.clientX - touch1.clientX;
        const dy = touch2.clientY - touch1.clientY;
        return Math.sqrt(dx * dx + dy * dy);
    }

    getTouchMidpoint(touch1, touch2) {
        return {
            x: (touch1.clientX + touch2.clientX) / 2,
            y: (touch1.clientY + touch2.clientY) / 2
        };
    }

    getObjectAt(x, y) {
        if (!this.currentStage) return null;

        for (let i = this.currentStage.objects.length - 1; i >= 0; i--) {
            const obj = this.currentStage.objects[i];
            const dx = x - obj.x;
            const dy = y - obj.y;
            const dist = Math.sqrt(dx * dx + dy * dy);

            if (obj.type === 'pepper' && dist <= obj.size) {
                return obj;
            } else if (dist <= obj.size * 1.5) {
                return obj;
            }
        }

        return null;
    }

    isTargetType(obj) {
        // Check if object is a target type that can have visibility controls
        const targetTypes = ['target', 'halfTarget', 'miniTarget', 'noShootTarget'];
        return targetTypes.includes(obj.type) || obj.baseType === 'target';
    }

    checkVisibilityControlClick(x, y) {
        // Check if click is on visibility control buttons
        if (!this.selectedObject) return null;

        const obj = this.selectedObject;
        const controlY = obj.y - obj.size - 45;
        const controlX = obj.x - 50; // Center the 5 buttons
        const buttonWidth = 18;
        const buttonHeight = 12;
        const buttonSpacing = 2;

        // Check each of the 5 buttons
        const buttons = [
            { mode: 'full', x: controlX },
            { mode: 'bottomHidden', x: controlX + buttonWidth + buttonSpacing },
            { mode: 'leftOnly', x: controlX + (buttonWidth + buttonSpacing) * 2 },
            { mode: 'centerOnly', x: controlX + (buttonWidth + buttonSpacing) * 3 },
            { mode: 'rightOnly', x: controlX + (buttonWidth + buttonSpacing) * 4 }
        ];

        for (const btn of buttons) {
            if (x >= btn.x && x <= btn.x + buttonWidth &&
                y >= controlY && y <= controlY + buttonHeight) {
                return btn.mode;
            }
        }

        return null;
    }

    addObjectToCanvas(type, clientX, clientY) {
        const point = this.getCanvasPoint(clientX, clientY);

        // Determine default size and properties based on type
        let defaultSize = 30;
        let isNoShoot = false;
        let baseType = type;

        // Handle size variants
        if (type === 'halfTarget') {
            defaultSize = 15;
            baseType = 'target';
        } else if (type === 'miniTarget') {
            defaultSize = 10;
            baseType = 'target';
        } else if (type === 'pepper' || type === 'plate') {
            defaultSize = 15;
        }

        // Handle no-shoot variants
        if (type === 'noShootTarget') {
            isNoShoot = true;
            baseType = 'target';
            defaultSize = 30;
        } else if (type === 'noShootPepper') {
            isNoShoot = true;
            baseType = 'pepper';
            defaultSize = 15;
        } else if (type === 'noShootPlate') {
            isNoShoot = true;
            baseType = 'plate';
            defaultSize = 15;
        }

        this.saveStateToHistory();

        const obj = {
            id: Date.now(),
            type: type,
            baseType: baseType,
            isNoShoot: isNoShoot,
            visibility: 'full', // 'full', 'bottomHidden', 'halfSize', 'bothSides'
            x: point.x,
            y: point.y,
            angle: 0,
            size: defaultSize
        };

        this.currentStage.objects.push(obj);
        document.getElementById('objectLibrary').classList.remove('open');
        this.render();
        this.saveToLocalStorage();
    }

    deleteSelectedObject() {
        if (!this.selectedObject || !this.currentStage) return;

        const index = this.currentStage.objects.indexOf(this.selectedObject);
        if (index > -1) {
            this.currentStage.objects.splice(index, 1);
            this.selectedObject = null;
            this.render();
            this.saveToLocalStorage();
        }
    }

    floodFill(x, y) {
        this.saveStateToHistory();
        
        // Simple fill implementation - adds a filled rectangle
        this.currentStage.fills.push({
            x: Math.round(x / this.gridSize) * this.gridSize,
            y: Math.round(y / this.gridSize) * this.gridSize,
            width: this.gridSize,
            height: this.gridSize,
            color: this.fillColor
        });
        this.render();
        this.saveToLocalStorage();
    }

    eraseAt(x, y) {
        const eraseRadius = 20;
        
        // Save state before erasing
        this.saveStateToHistory();

        // Erase drawings
        this.currentStage.drawings = this.currentStage.drawings.filter(drawing => {
            return !drawing.points.some(p => {
                const dx = p.x - x;
                const dy = p.y - y;
                return Math.sqrt(dx * dx + dy * dy) < eraseRadius;
            });
        });

        // Erase fills
        this.currentStage.fills = this.currentStage.fills.filter(fill => {
            return !(x >= fill.x && x <= fill.x + fill.width &&
                y >= fill.y && y <= fill.y + fill.height);
        });

        // Erase measurements (ruler lines)
        this.currentStage.measurements = this.currentStage.measurements.filter(measurement => {
            const dist = this.pointToLineSegmentDistance(
                x, y,
                measurement.start.x, measurement.start.y,
                measurement.end.x, measurement.end.y
            );
            return dist >= eraseRadius;
        });

        this.render();
        this.saveToLocalStorage();
    }

    pointToLineSegmentDistance(px, py, x1, y1, x2, y2) {
        // Calculate the distance from point (px, py) to line segment (x1, y1) - (x2, y2)
        const dx = x2 - x1;
        const dy = y2 - y1;
        const lengthSquared = dx * dx + dy * dy;

        if (lengthSquared === 0) {
            // Line segment is actually a point
            const dpx = px - x1;
            const dpy = py - y1;
            return Math.sqrt(dpx * dpx + dpy * dpy);
        }

        // Calculate projection of point onto line segment
        let t = ((px - x1) * dx + (py - y1) * dy) / lengthSquared;
        t = Math.max(0, Math.min(1, t));

        // Find closest point on line segment
        const closestX = x1 + t * dx;
        const closestY = y1 + t * dy;

        // Calculate distance from point to closest point
        const distX = px - closestX;
        const distY = py - closestY;
        return Math.sqrt(distX * distX + distY * distY);
    }

    render() {
        if (!this.currentStage) return;

        this.ctx.save();

        // Clear canvas
        this.ctx.fillStyle = '#ffffff';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

        // Apply transformations
        this.ctx.translate(this.translateX, this.translateY);
        this.ctx.scale(this.scale, this.scale);

        // Draw grid
        if (this.showGrid) {
            this.drawGrid();
        }

        // Draw fills
        this.currentStage.fills.forEach(fill => {
            this.ctx.fillStyle = fill.color + '80'; // Add transparency
            this.ctx.fillRect(fill.x, fill.y, fill.width, fill.height);
        });

        // Draw paths
        this.currentStage.drawings.forEach(drawing => {
            if (drawing.points.length < 2) return;

            this.ctx.strokeStyle = drawing.color;
            this.ctx.lineWidth = drawing.width;
            this.ctx.lineCap = 'round';
            this.ctx.lineJoin = 'round';

            this.ctx.beginPath();
            this.ctx.moveTo(drawing.points[0].x, drawing.points[0].y);

            for (let i = 1; i < drawing.points.length; i++) {
                this.ctx.lineTo(drawing.points[i].x, drawing.points[i].y);
            }

            this.ctx.stroke();
        });

        // Draw current path being drawn
        if (this.isDrawing && this.currentPath.length > 1) {
            this.ctx.strokeStyle = this.lineColor;
            this.ctx.lineWidth = this.lineWidth;
            this.ctx.lineCap = 'round';
            this.ctx.lineJoin = 'round';

            this.ctx.beginPath();
            this.ctx.moveTo(this.currentPath[0].x, this.currentPath[0].y);

            for (let i = 1; i < this.currentPath.length; i++) {
                this.ctx.lineTo(this.currentPath[i].x, this.currentPath[i].y);
            }

            this.ctx.stroke();
        }

        // Draw objects
        this.currentStage.objects.forEach(obj => {
            this.drawObject(obj);
        });

        // Draw persistent measurements
        if (this.currentStage.measurements) {
            this.currentStage.measurements.forEach(measurement => {
                this.drawRuler(measurement.start, measurement.end);
            });
        }

        // Draw ruler (temporary while drawing)
        if (this.rulerStart && this.rulerEnd) {
            this.drawRuler(this.rulerStart, this.rulerEnd);
        }

        // Draw line tool preview (temporary while drawing)
        if (this.lineStart && this.lineEnd) {
            this.ctx.strokeStyle = this.lineColor;
            this.ctx.lineWidth = this.lineWidth;
            this.ctx.lineCap = 'round';
            this.ctx.setLineDash([]);

            this.ctx.beginPath();
            this.ctx.moveTo(this.lineStart.x, this.lineStart.y);
            this.ctx.lineTo(this.lineEnd.x, this.lineEnd.y);
            this.ctx.stroke();
        }

        this.ctx.restore();
    }

    drawGrid() {
        const startX = Math.floor(-this.translateX / this.scale / this.gridSize) * this.gridSize;
        const startY = Math.floor(-this.translateY / this.scale / this.gridSize) * this.gridSize;
        const endX = startX + this.canvas.width / this.scale + this.gridSize;
        const endY = startY + this.canvas.height / this.scale + this.gridSize;

        this.ctx.strokeStyle = '#ddd';
        this.ctx.lineWidth = 0.5 / this.scale;

        for (let x = startX; x <= endX; x += this.gridSize) {
            this.ctx.beginPath();
            this.ctx.moveTo(x, startY);
            this.ctx.lineTo(x, endY);
            this.ctx.stroke();
        }

        for (let y = startY; y <= endY; y += this.gridSize) {
            this.ctx.beginPath();
            this.ctx.moveTo(startX, y);
            this.ctx.lineTo(endX, y);
            this.ctx.stroke();
        }
    }

    drawObject(obj) {
        this.ctx.save();
        this.ctx.translate(obj.x, obj.y);
        this.ctx.rotate((obj.angle * Math.PI) / 180);

        const isSelected = obj === this.selectedObject;
        const baseType = obj.baseType || obj.type;

        if (isSelected) {
            this.ctx.strokeStyle = '#3498db';
            this.ctx.lineWidth = 3 / this.scale;
        } else {
            this.ctx.strokeStyle = '#2c3e50';
            this.ctx.lineWidth = 2 / this.scale;
        }

        // Determine color based on no-shoot status
        const shootColor = obj.isNoShoot ? '#e8e8e8' : '#3498db';
        const targetColor = obj.isNoShoot ? '#e8e8e8' : '#d4a373';

        // Draw based on base type
        if (baseType === 'pepper' || obj.type === 'pepper' || obj.type === 'noShootPepper') {
            this.ctx.fillStyle = shootColor;
            this.ctx.beginPath();
            this.ctx.arc(0, 0, obj.size, 0, Math.PI * 2);
            this.ctx.fill();
            this.ctx.stroke();

            this.ctx.fillStyle = obj.isNoShoot ? '#666' : 'white';
            this.ctx.font = `${obj.size}px Arial`;
            this.ctx.textAlign = 'center';
            this.ctx.textBaseline = 'middle';
            this.ctx.fillText('P', 0, 0);
        } else if (baseType === 'plate' || obj.type === 'plate' || obj.type === 'noShootPlate') {
            this.ctx.fillStyle = shootColor;
            this.ctx.fillRect(-obj.size * 0.6, -obj.size * 0.6, obj.size * 1.2, obj.size * 1.2);
            this.ctx.strokeRect(-obj.size * 0.6, -obj.size * 0.6, obj.size * 1.2, obj.size * 1.2);

            this.ctx.fillStyle = obj.isNoShoot ? '#666' : 'white';
            this.ctx.font = `${obj.size * 0.6}px Arial`;
            this.ctx.textAlign = 'center';
            this.ctx.textBaseline = 'middle';
            this.ctx.fillText('PL', 0, 0);
        } else if (baseType === 'target' || obj.type === 'target' || obj.type === 'halfTarget' || 
                   obj.type === 'miniTarget' || obj.type === 'noShootTarget') {
            // Draw octagon shape (IPSC target)
            this.ctx.fillStyle = targetColor;
            this.ctx.beginPath();
            for (let i = 0; i < 8; i++) {
                const angle = (i * Math.PI) / 4 - Math.PI / 2;
                const x = Math.cos(angle) * obj.size * 0.5;
                const y = Math.sin(angle) * obj.size;
                if (i === 0) {
                    this.ctx.moveTo(x, y);
                } else {
                    this.ctx.lineTo(x, y);
                }
            }
            this.ctx.closePath();
            this.ctx.fill();
            this.ctx.stroke();

            this.ctx.fillStyle = obj.isNoShoot ? '#666' : 'white';
            this.ctx.font = `${obj.size}px Arial`;
            this.ctx.textAlign = 'center';
            this.ctx.textBaseline = 'middle';
            this.ctx.fillText('T', 0, 0);

            // Apply visibility masking for targets
            if (obj.visibility && obj.visibility !== 'full') {
                this.ctx.fillStyle = '#000000';
                const thirdWidth = obj.size / 3;
                
                if (obj.visibility === 'bottomHidden') {
                    // Black rectangle covering bottom half
                    this.ctx.fillRect(-obj.size * 0.5, 0, obj.size, obj.size);
                } else if (obj.visibility === 'leftOnly') {
                    // White left third only - black covers center and right
                    this.ctx.fillRect(-obj.size * 0.5 + thirdWidth, -obj.size, thirdWidth * 2, obj.size * 2);
                } else if (obj.visibility === 'centerOnly') {
                    // White center third only - black covers left and right
                    this.ctx.fillRect(-obj.size * 0.5, -obj.size, thirdWidth, obj.size * 2);
                    this.ctx.fillRect(-obj.size * 0.5 + thirdWidth * 2, -obj.size, thirdWidth, obj.size * 2);
                } else if (obj.visibility === 'rightOnly') {
                    // White right third only - black covers left and center
                    this.ctx.fillRect(-obj.size * 0.5, -obj.size, thirdWidth * 2, obj.size * 2);
                } else if (obj.visibility === 'halfSize') {
                    // Legacy support - same as centerOnly
                    this.ctx.fillRect(-obj.size * 0.5, -obj.size, thirdWidth, obj.size * 2);
                    this.ctx.fillRect(-obj.size * 0.5 + thirdWidth * 2, -obj.size, thirdWidth, obj.size * 2);
                } else if (obj.visibility === 'bothSides') {
                    // Legacy support - same as centerOnly
                    this.ctx.fillRect(-obj.size * 0.5, -obj.size, thirdWidth, obj.size * 2);
                    this.ctx.fillRect(-obj.size * 0.5 + thirdWidth * 2, -obj.size, thirdWidth, obj.size * 2);
                }
            }

            // Angle indicator
            if (obj.angle !== 0 && !obj.isNoShoot) {
                this.ctx.strokeStyle = '#e74c3c';
                this.ctx.beginPath();
                this.ctx.moveTo(0, -obj.size);
                this.ctx.lineTo(0, -obj.size - 15);
                this.ctx.stroke();
            }
        } else if (obj.type === 'barricade') {
            this.ctx.fillStyle = '#8b4513';
            this.ctx.fillRect(-obj.size, -obj.size * 0.3, obj.size * 2, obj.size * 0.6);
            this.ctx.strokeRect(-obj.size, -obj.size * 0.3, obj.size * 2, obj.size * 0.6);

            this.ctx.fillStyle = 'white';
            this.ctx.font = `${obj.size * 0.6}px Arial`;
            this.ctx.textAlign = 'center';
            this.ctx.textBaseline = 'middle';
            this.ctx.fillText('B', 0, 0);
        }

        // Rotation handle for selected object
        if (isSelected) {
            this.ctx.fillStyle = '#3498db';
            this.ctx.beginPath();
            this.ctx.arc(0, -obj.size - 20, 5 / this.scale, 0, Math.PI * 2);
            this.ctx.fill();
        }

        this.ctx.restore();

        // Draw visibility control buttons OUTSIDE rotation transform (in screen space)
        if (isSelected && this.isTargetType(obj)) {
            this.ctx.save();
            
            const controlY = obj.y - obj.size - 45;
            const controlX = obj.x - 50;
            const buttonWidth = 18;
            const buttonHeight = 12;
            const buttonSpacing = 2;

            // Button configurations - 5 modes
            const buttons = [
                { mode: 'full', x: controlX },
                { mode: 'bottomHidden', x: controlX + buttonWidth + buttonSpacing },
                { mode: 'leftOnly', x: controlX + (buttonWidth + buttonSpacing) * 2 },
                { mode: 'centerOnly', x: controlX + (buttonWidth + buttonSpacing) * 3 },
                { mode: 'rightOnly', x: controlX + (buttonWidth + buttonSpacing) * 4 }
            ];

            buttons.forEach(btn => {
                const isActive = obj.visibility === btn.mode;
                
                // Draw button background
                this.ctx.fillStyle = isActive ? '#3498db' : '#f0f0f0';
                this.ctx.fillRect(btn.x, controlY, buttonWidth, buttonHeight);
                
                // Draw button border
                this.ctx.strokeStyle = isActive ? '#2980b9' : '#ccc';
                this.ctx.lineWidth = 1 / this.scale;
                this.ctx.strokeRect(btn.x, controlY, buttonWidth, buttonHeight);

                // Draw icon representing visibility mode
                const iconMargin = 2;
                const iconWidth = buttonWidth - iconMargin * 2;
                const iconHeight = buttonHeight - iconMargin * 2;
                
                if (btn.mode === 'full') {
                    // Full white rectangle
                    this.ctx.fillStyle = '#ffffff';
                    this.ctx.fillRect(btn.x + iconMargin, controlY + iconMargin, iconWidth, iconHeight);
                } else if (btn.mode === 'bottomHidden') {
                    // White top, black bottom
                    this.ctx.fillStyle = '#ffffff';
                    this.ctx.fillRect(btn.x + iconMargin, controlY + iconMargin, iconWidth, iconHeight / 2);
                    this.ctx.fillStyle = '#000000';
                    this.ctx.fillRect(btn.x + iconMargin, controlY + buttonHeight / 2, iconWidth, iconHeight / 2);
                } else if (btn.mode === 'leftOnly') {
                    // White left third, black rest
                    const thirdWidth = iconWidth / 3;
                    this.ctx.fillStyle = '#ffffff';
                    this.ctx.fillRect(btn.x + iconMargin, controlY + iconMargin, thirdWidth, iconHeight);
                    this.ctx.fillStyle = '#000000';
                    this.ctx.fillRect(btn.x + iconMargin + thirdWidth, controlY + iconMargin, thirdWidth * 2, iconHeight);
                } else if (btn.mode === 'centerOnly') {
                    // White center third, black sides
                    const thirdWidth = iconWidth / 3;
                    this.ctx.fillStyle = '#000000';
                    this.ctx.fillRect(btn.x + iconMargin, controlY + iconMargin, thirdWidth, iconHeight);
                    this.ctx.fillStyle = '#ffffff';
                    this.ctx.fillRect(btn.x + iconMargin + thirdWidth, controlY + iconMargin, thirdWidth, iconHeight);
                    this.ctx.fillStyle = '#000000';
                    this.ctx.fillRect(btn.x + iconMargin + thirdWidth * 2, controlY + iconMargin, thirdWidth, iconHeight);
                } else if (btn.mode === 'rightOnly') {
                    // White right third, black rest
                    const thirdWidth = iconWidth / 3;
                    this.ctx.fillStyle = '#000000';
                    this.ctx.fillRect(btn.x + iconMargin, controlY + iconMargin, thirdWidth * 2, iconHeight);
                    this.ctx.fillStyle = '#ffffff';
                    this.ctx.fillRect(btn.x + iconMargin + thirdWidth * 2, controlY + iconMargin, thirdWidth, iconHeight);
                }
            });
            
            this.ctx.restore();
        }
    }

    drawRuler(start, end) {
        const dx = end.x - start.x;
        const dy = end.y - start.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        const meters = (distance / this.pixelsPerMeter).toFixed(2);

        this.ctx.strokeStyle = '#e74c3c';
        this.ctx.lineWidth = 2 / this.scale;
        this.ctx.setLineDash([5 / this.scale, 5 / this.scale]);

        this.ctx.beginPath();
        this.ctx.moveTo(start.x, start.y);
        this.ctx.lineTo(end.x, end.y);
        this.ctx.stroke();

        this.ctx.setLineDash([]);

        // Draw measurement text
        const midX = (start.x + end.x) / 2;
        const midY = (start.y + end.y) / 2;

        this.ctx.fillStyle = '#e74c3c';
        this.ctx.font = `${16 / this.scale}px Arial`;
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'bottom';
        this.ctx.fillText(`${meters}m`, midX, midY - 5 / this.scale);
    }

    saveStateToHistory() {
        // Save current stage state to history
        const stateSnapshot = JSON.parse(JSON.stringify(this.currentStage));
        this.historyStack.push(stateSnapshot);
        
        // Limit history size
        if (this.historyStack.length > this.maxHistorySize) {
            this.historyStack.shift();
        }
    }

    undo() {
        if (this.historyStack.length === 0) {
            return; // Nothing to undo
        }

        // Restore previous state
        const previousState = this.historyStack.pop();
        this.stages[this.currentStageIndex] = previousState;
        
        this.selectedObject = null;
        this.render();
        this.saveToLocalStorage();
    }

    exportJSON() {
        const data = {
            version: '1.0',
            stages: this.stages
        };

        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'ipsc-stages.json';
        a.click();
        URL.revokeObjectURL(url);
    }

    importJSON(file) {
        if (!file) return;

        const reader = new FileReader();
        reader.onload = (e) => {
            try {
                const data = JSON.parse(e.target.result);
                this.stages = data.stages;
                this.currentStageIndex = 0;
                this.updateStageTabs();
                this.updateStageInfo();
                this.render();
                this.saveToLocalStorage();
            } catch (error) {
                alert('Error loading file: ' + error.message);
            }
        };
        reader.readAsText(file);
    }

    exportImage() {
        // Create off-screen canvas with higher resolution
        const exportCanvas = document.createElement('canvas');
        const exportCtx = exportCanvas.getContext('2d');
        const dpi = 2;

        exportCanvas.width = 1920 * dpi;
        exportCanvas.height = 1080 * dpi;

        // White background
        exportCtx.fillStyle = '#ffffff';
        exportCtx.fillRect(0, 0, exportCanvas.width, exportCanvas.height);

        // Calculate canvas area (leave room for annotations)
        const canvasArea = {
            x: 50 * dpi,
            y: 50 * dpi,
            width: exportCanvas.width - 100 * dpi,
            height: exportCanvas.height - 300 * dpi
        };

        // Calculate bounding box of all content
        let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;

        // Check objects
        this.currentStage.objects.forEach(obj => {
            const size = obj.size * 2; // Account for object size
            minX = Math.min(minX, obj.x - size);
            minY = Math.min(minY, obj.y - size);
            maxX = Math.max(maxX, obj.x + size);
            maxY = Math.max(maxY, obj.y + size);
        });

        // Check drawings
        this.currentStage.drawings.forEach(drawing => {
            drawing.points.forEach(p => {
                minX = Math.min(minX, p.x);
                minY = Math.min(minY, p.y);
                maxX = Math.max(maxX, p.x);
                maxY = Math.max(maxY, p.y);
            });
        });

        // Check fills
        this.currentStage.fills.forEach(fill => {
            minX = Math.min(minX, fill.x);
            minY = Math.min(minY, fill.y);
            maxX = Math.max(maxX, fill.x + fill.width);
            maxY = Math.max(maxY, fill.y + fill.height);
        });

        // Default bounds if no content
        if (minX === Infinity) {
            minX = 0; minY = 0; maxX = 100; maxY = 100;
        }

        // Add padding
        const padding = 50;
        minX -= padding;
        minY -= padding;
        maxX += padding;
        maxY += padding;

        const contentWidth = maxX - minX;
        const contentHeight = maxY - minY;

        // Calculate scale to fit content in canvas area
        const scaleX = canvasArea.width / contentWidth;
        const scaleY = canvasArea.height / contentHeight;
        const contentScale = Math.min(scaleX, scaleY);

        // Center the content
        const scaledWidth = contentWidth * contentScale;
        const scaledHeight = contentHeight * contentScale;
        const offsetX = canvasArea.x + (canvasArea.width - scaledWidth) / 2;
        const offsetY = canvasArea.y + (canvasArea.height - scaledHeight) / 2;

        // Draw stage content
        exportCtx.save();
        exportCtx.translate(offsetX, offsetY);
        exportCtx.scale(contentScale, contentScale);
        exportCtx.translate(-minX, -minY);

        // Grid is intentionally not drawn in exports

        // Draw fills
        this.currentStage.fills.forEach(fill => {
            exportCtx.fillStyle = fill.color + '80';
            exportCtx.fillRect(fill.x, fill.y, fill.width, fill.height);
        });

        // Draw drawings
        this.currentStage.drawings.forEach(drawing => {
            if (drawing.points.length < 2) return;
            exportCtx.strokeStyle = drawing.color;
            exportCtx.lineWidth = drawing.width;
            exportCtx.lineCap = 'round';
            exportCtx.lineJoin = 'round';
            exportCtx.beginPath();
            exportCtx.moveTo(drawing.points[0].x, drawing.points[0].y);
            for (let i = 1; i < drawing.points.length; i++) {
                exportCtx.lineTo(drawing.points[i].x, drawing.points[i].y);
            }
            exportCtx.stroke();
        });

        // Draw objects (with full rendering for new types)
        this.currentStage.objects.forEach(obj => {
            exportCtx.save();
            exportCtx.translate(obj.x, obj.y);
            exportCtx.rotate((obj.angle * Math.PI) / 180);
            exportCtx.strokeStyle = '#2c3e50';
            exportCtx.lineWidth = 2;

            const baseType = obj.baseType || obj.type;
            const shootColor = obj.isNoShoot ? '#e8e8e8' : '#3498db';
            const targetColor = obj.isNoShoot ? '#e8e8e8' : '#d4a373';
            const textColor = obj.isNoShoot ? '#666' : 'white';

            if (baseType === 'pepper' || obj.type === 'pepper' || obj.type === 'noShootPepper') {
                exportCtx.fillStyle = shootColor;
                exportCtx.beginPath();
                exportCtx.arc(0, 0, obj.size, 0, Math.PI * 2);
                exportCtx.fill();
                exportCtx.stroke();
                exportCtx.fillStyle = textColor;
                exportCtx.font = `${obj.size}px Arial`;
                exportCtx.textAlign = 'center';
                exportCtx.textBaseline = 'middle';
                exportCtx.fillText('P', 0, 0);
            } else if (baseType === 'plate' || obj.type === 'plate' || obj.type === 'noShootPlate') {
                exportCtx.fillStyle = shootColor;
                exportCtx.fillRect(-obj.size * 0.6, -obj.size * 0.6, obj.size * 1.2, obj.size * 1.2);
                exportCtx.strokeRect(-obj.size * 0.6, -obj.size * 0.6, obj.size * 1.2, obj.size * 1.2);
                exportCtx.fillStyle = textColor;
                exportCtx.font = `${obj.size * 0.6}px Arial`;
                exportCtx.textAlign = 'center';
                exportCtx.textBaseline = 'middle';
                exportCtx.fillText('PL', 0, 0);
            } else if (baseType === 'target' || obj.type === 'target' || obj.type === 'halfTarget' || 
                       obj.type === 'miniTarget' || obj.type === 'noShootTarget') {
                // Draw octagon
                exportCtx.fillStyle = targetColor;
                exportCtx.beginPath();
                for (let i = 0; i < 8; i++) {
                    const angle = (i * Math.PI) / 4 - Math.PI / 2;
                    const x = Math.cos(angle) * obj.size * 0.5;
                    const y = Math.sin(angle) * obj.size;
                    if (i === 0) {
                        exportCtx.moveTo(x, y);
                    } else {
                        exportCtx.lineTo(x, y);
                    }
                }
                exportCtx.closePath();
                exportCtx.fill();
                exportCtx.stroke();

                exportCtx.fillStyle = textColor;
                exportCtx.font = `${obj.size}px Arial`;
                exportCtx.textAlign = 'center';
                exportCtx.textBaseline = 'middle';
                exportCtx.fillText('T', 0, 0);

                // Apply visibility masking
                if (obj.visibility && obj.visibility !== 'full') {
                    exportCtx.fillStyle = '#000000';
                    const thirdWidth = obj.size / 3;
                    
                    if (obj.visibility === 'bottomHidden') {
                        exportCtx.fillRect(-obj.size * 0.5, 0, obj.size, obj.size);
                    } else if (obj.visibility === 'leftOnly') {
                        exportCtx.fillRect(-obj.size * 0.5 + thirdWidth, -obj.size, thirdWidth * 2, obj.size * 2);
                    } else if (obj.visibility === 'centerOnly') {
                        exportCtx.fillRect(-obj.size * 0.5, -obj.size, thirdWidth, obj.size * 2);
                        exportCtx.fillRect(-obj.size * 0.5 + thirdWidth * 2, -obj.size, thirdWidth, obj.size * 2);
                    } else if (obj.visibility === 'rightOnly') {
                        exportCtx.fillRect(-obj.size * 0.5, -obj.size, thirdWidth * 2, obj.size * 2);
                    } else if (obj.visibility === 'halfSize' || obj.visibility === 'bothSides') {
                        // Legacy support
                        exportCtx.fillRect(-obj.size * 0.5, -obj.size, thirdWidth, obj.size * 2);
                        exportCtx.fillRect(-obj.size * 0.5 + thirdWidth * 2, -obj.size, thirdWidth, obj.size * 2);
                    }
                }
            } else if (obj.type === 'barricade') {
                exportCtx.fillStyle = '#8b4513';
                exportCtx.fillRect(-obj.size, -obj.size * 0.3, obj.size * 2, obj.size * 0.6);
                exportCtx.strokeRect(-obj.size, -obj.size * 0.3, obj.size * 2, obj.size * 0.6);
                exportCtx.fillStyle = 'white';
                exportCtx.font = `${obj.size * 0.6}px Arial`;
                exportCtx.textAlign = 'center';
                exportCtx.textBaseline = 'middle';
                exportCtx.fillText('B', 0, 0);
            }

            exportCtx.restore();
        });

        // Draw persistent measurements (ruler lines)
        if (this.currentStage.measurements && this.currentStage.measurements.length > 0) {
            this.currentStage.measurements.forEach(measurement => {
                const start = measurement.start;
                const end = measurement.end;
                const dx = end.x - start.x;
                const dy = end.y - start.y;
                const distance = Math.sqrt(dx * dx + dy * dy);
                const meters = (distance / this.pixelsPerMeter).toFixed(2);

                // Draw ruler line
                exportCtx.strokeStyle = '#e74c3c';
                exportCtx.lineWidth = 2;
                exportCtx.setLineDash([5, 5]);
                exportCtx.beginPath();
                exportCtx.moveTo(start.x, start.y);
                exportCtx.lineTo(end.x, end.y);
                exportCtx.stroke();
                exportCtx.setLineDash([]);

                // Draw measurement text
                const midX = (start.x + end.x) / 2;
                const midY = (start.y + end.y) / 2;
                exportCtx.fillStyle = '#e74c3c';
                exportCtx.font = '16px Arial';
                exportCtx.textAlign = 'center';
                exportCtx.textBaseline = 'bottom';
                exportCtx.fillText(`${meters}m`, midX, midY - 5);
            });
        }

        exportCtx.restore();

        // Draw annotations panel
        const annotY = canvasArea.y + canvasArea.height + 30 * dpi;
        exportCtx.fillStyle = '#2c3e50';
        exportCtx.font = `${24 * dpi}px Arial`;
        exportCtx.fillText(this.currentStage.name, 50 * dpi, annotY);

        if (this.currentStage.notes) {
            exportCtx.font = `${16 * dpi}px Arial`;
            exportCtx.fillStyle = '#555';
            const lines = this.currentStage.notes.split('\n');
            lines.forEach((line, i) => {
                exportCtx.fillText(line, 50 * dpi, annotY + (40 + i * 25) * dpi);
            });
        }

        // Download
        exportCanvas.toBlob((blob) => {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${this.currentStage.name.replace(/[^a-z0-9]/gi, '_')}.png`;
            a.click();
            URL.revokeObjectURL(url);
        });
    }

    saveToLocalStorage() {
        try {
            localStorage.setItem('ipsc-stages', JSON.stringify({
                version: '1.0',
                stages: this.stages,
                currentStageIndex: this.currentStageIndex
            }));
        } catch (e) {
            console.error('Failed to save to localStorage:', e);
        }
    }

    loadFromLocalStorage() {
        try {
            const data = localStorage.getItem('ipsc-stages');
            if (data) {
                const parsed = JSON.parse(data);
                this.stages = parsed.stages;
                this.currentStageIndex = parsed.currentStageIndex || 0;
                this.updateStageTabs();
                this.updateStageInfo();
                this.render();
            }
        } catch (e) {
            console.error('Failed to load from localStorage:', e);
        }
    }
}

// Initialize application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new StageDesigner();
});

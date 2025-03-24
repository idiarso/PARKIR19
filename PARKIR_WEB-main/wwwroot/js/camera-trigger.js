// Camera Trigger Configuration and Implementation

const CAMERA_CONFIG = {
    motionThreshold: 30, // Minimum pixel difference to trigger motion detection
    debounceDelay: 500, // Milliseconds to wait before allowing another trigger
    triggerCooldown: 2000, // Milliseconds to wait after a successful capture
    roiPadding: 20, // Padding around ROI for better detection
    minMotionFrames: 3, // Minimum consecutive frames with motion to trigger
    maxMotionFrames: 10, // Maximum frames to wait before resetting motion detection
    captureQuality: 0.9 // JPEG quality for captured images (0-1)
};

class CameraTrigger {
    constructor(videoElement, roiElement) {
        this.video = videoElement;
        this.roi = roiElement;
        this.canvas = document.createElement('canvas');
        this.ctx = this.canvas.getContext('2d');
        this.previousFrame = null;
        this.motionFrameCount = 0;
        this.lastTriggerTime = 0;
        this.isProcessing = false;
        this.enabled = true;
    }

    // Initialize motion detection
    async initialize() {
        this.canvas.width = this.video.videoWidth;
        this.canvas.height = this.video.videoHeight;
        await this.startDetection();
    }

    // Start motion detection loop
    async startDetection() {
        if (!this.enabled) return;

        this.processFrame();
        requestAnimationFrame(() => this.startDetection());
    }

    // Process single frame for motion detection
    processFrame() {
        if (!this.enabled || this.isProcessing) return;

        try {
            this.isProcessing = true;
            this.ctx.drawImage(this.video, 0, 0);
            const currentFrame = this.ctx.getImageData(
                this.roi.offsetLeft,
                this.roi.offsetTop,
                this.roi.offsetWidth,
                this.roi.offsetHeight
            );

            if (this.previousFrame && this.detectMotion(currentFrame, this.previousFrame)) {
                this.motionFrameCount++;
                if (this.shouldTrigger()) {
                    this.triggerCapture();
                    this.motionFrameCount = 0;
                }
            } else {
                this.motionFrameCount = Math.max(0, this.motionFrameCount - 1);
            }

            this.previousFrame = currentFrame;
        } finally {
            this.isProcessing = false;
        }
    }

    // Detect motion between frames
    detectMotion(current, previous) {
        const diff = new Uint32Array(current.data.length / 4);
        let motionPixels = 0;

        for (let i = 0; i < current.data.length; i += 4) {
            const rDiff = Math.abs(current.data[i] - previous.data[i]);
            const gDiff = Math.abs(current.data[i + 1] - previous.data[i + 1]);
            const bDiff = Math.abs(current.data[i + 2] - previous.data[i + 2]);
            
            if ((rDiff + gDiff + bDiff) / 3 > CAMERA_CONFIG.motionThreshold) {
                motionPixels++;
            }
        }

        return motionPixels > (current.width * current.height * 0.01); // 1% of frame changed
    }

    // Check if capture should be triggered
    shouldTrigger() {
        const now = Date.now();
        if (this.motionFrameCount >= CAMERA_CONFIG.minMotionFrames &&
            now - this.lastTriggerTime > CAMERA_CONFIG.triggerCooldown) {
            return true;
        }
        return false;
    }

    // Trigger camera capture
    triggerCapture() {
        this.lastTriggerTime = Date.now();
        const event = new CustomEvent('cameraTriggered', {
            detail: {
                timestamp: this.lastTriggerTime,
                motionStrength: this.motionFrameCount
            }
        });
        document.dispatchEvent(event);
    }

    // Enable/disable trigger
    setEnabled(enabled) {
        this.enabled = enabled;
        if (enabled) this.startDetection();
    }

    // Update ROI position
    updateROI(x, y, width, height) {
        this.roi.style.left = `${x}px`;
        this.roi.style.top = `${y}px`;
        this.roi.style.width = `${width}px`;
        this.roi.style.height = `${height}px`;
    }
}

// Export for use in other modules
window.CameraTrigger = CameraTrigger;
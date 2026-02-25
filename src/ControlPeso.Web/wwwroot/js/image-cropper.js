// image-cropper.js - JSInterop wrapper for Cropper.js
// Provides Blazor-friendly interface to Cropper.js library

window.ImageCropper = {
    cropper: null,
    imageElement: null,

    /**
     * Initializes Cropper.js on the specified image element
     * @param {string} imageElementId - ID of the <img> element
     * @param {object} options - Cropper.js configuration options
     * @returns {boolean} Success status
     */
    initialize: function (imageElementId, options) {
        try {
            console.log('[ImageCropper] Initializing cropper for element:', imageElementId);

            // Check if Cropper library is loaded
            if (typeof Cropper === 'undefined') {
                console.error('[ImageCropper] Cropper.js library not loaded');
                return false;
            }

            // Get image element
            this.imageElement = document.getElementById(imageElementId);
            if (!this.imageElement) {
                console.error('[ImageCropper] Image element not found:', imageElementId);
                return false;
            }

            // Check if image is loaded
            if (!this.imageElement.complete) {
                console.warn('[ImageCropper] Image not fully loaded yet, but proceeding with initialization');
            } else {
                console.log('[ImageCropper] Image already loaded, dimensions:', 
                    this.imageElement.naturalWidth, 'x', this.imageElement.naturalHeight);
            }

            // Destroy existing cropper if any
            if (this.cropper) {
                console.log('[ImageCropper] Destroying existing cropper instance');
                this.cropper.destroy();
                this.cropper = null;
            }

            // Default options for avatar cropping
            const defaultOptions = {
                aspectRatio: 1, // Square crop for avatars
                viewMode: 1, // Restrict crop box to canvas
                dragMode: 'move',
                autoCropArea: 0.9,
                restore: false,
                guides: true,
                center: true,
                highlight: true,
                cropBoxMovable: true,
                cropBoxResizable: true,
                toggleDragModeOnDblclick: false,
                responsive: true,
                background: true,
                modal: true,
                minContainerWidth: 300,
                minContainerHeight: 300,
                minCropBoxWidth: 100,
                minCropBoxHeight: 100,
                // Ready callback
                ready: function() {
                    console.log('[ImageCropper] Cropper is ready and functional');
                }
            };

            // Merge user options with defaults
            const finalOptions = { ...defaultOptions, ...options };

            // Initialize Cropper.js - it handles image load internally
            this.cropper = new Cropper(this.imageElement, finalOptions);

            console.log('[ImageCropper] Cropper initialization started successfully');
            return true;
        } catch (error) {
            console.error('[ImageCropper] Error initializing cropper:', error);
            return false;
        }
    },

    /**
     * Gets the cropped image data as a Base64 string
     * OPTIMIZED: Reduced canvas size to 256x256 to prevent timeout
     * @param {string} format - Output format (image/jpeg, image/png, image/webp)
     * @param {number} quality - Quality for lossy formats (0.0 to 1.0)
     * @returns {string} Base64 data URL of cropped image
     */
    getCroppedImageBase64: function (format = 'image/jpeg', quality = 0.95) {
        try {
            if (!this.cropper) {
                console.error('[ImageCropper] Cropper not initialized');
                return null;
            }

            console.log('[ImageCropper] Getting cropped image - Format:', format, 'Quality:', quality);

            // CRITICAL: Reduce canvas size to prevent timeout/circuit disconnect
            // Avatar only needs 256x256 for profile picture (512x512 was too large)
            const canvas = this.cropper.getCroppedCanvas({
                width: 256,
                height: 256,
                minWidth: 128,
                minHeight: 128,
                maxWidth: 512,
                maxHeight: 512,
                fillColor: '#fff',
                imageSmoothingEnabled: true,
                imageSmoothingQuality: 'high'
            });

            if (!canvas) {
                console.error('[ImageCropper] Failed to get cropped canvas');
                return null;
            }

            console.log('[ImageCropper] Canvas created - Size:', canvas.width, 'x', canvas.height);

            // Convert to Base64 (this operation can be slow for large images)
            const startTime = performance.now();
            const base64 = canvas.toDataURL(format, quality);
            const duration = (performance.now() - startTime).toFixed(2);

            console.log('[ImageCropper] Base64 conversion completed in', duration, 'ms - Size:', (base64.length / 1024).toFixed(2), 'KB');

            return base64;
        } catch (error) {
            console.error('[ImageCropper] Error getting cropped image:', error);
            return null;
        }
    },

    /**
     * Gets the cropped image data as a Blob for upload
     * @param {string} format - Output format (image/jpeg, image/png, image/webp)
     * @param {number} quality - Quality for lossy formats (0.0 to 1.0)
     * @returns {Promise<Blob>} Blob of cropped image
     */
    getCroppedImageBlob: function (format = 'image/jpeg', quality = 0.95) {
        return new Promise((resolve, reject) => {
            try {
                if (!this.cropper) {
                    console.error('[ImageCropper] Cropper not initialized');
                    reject(new Error('Cropper not initialized'));
                    return;
                }

                const canvas = this.cropper.getCroppedCanvas({
                    width: 512,
                    height: 512,
                    minWidth: 256,
                    minHeight: 256,
                    maxWidth: 2048,
                    maxHeight: 2048,
                    fillColor: '#fff',
                    imageSmoothingEnabled: true,
                    imageSmoothingQuality: 'high'
                });

                if (!canvas) {
                    reject(new Error('Failed to get cropped canvas'));
                    return;
                }

                canvas.toBlob(
                    (blob) => {
                        if (blob) {
                            console.log('[ImageCropper] Blob generated, size:', (blob.size / 1024).toFixed(2), 'KB');
                            resolve(blob);
                        } else {
                            reject(new Error('Failed to create blob'));
                        }
                    },
                    format,
                    quality
                );
            } catch (error) {
                console.error('[ImageCropper] Error getting cropped blob:', error);
                reject(error);
            }
        });
    },

    /**
     * Rotates the image by specified degrees
     * @param {number} degrees - Rotation angle in degrees
     */
    rotate: function (degrees) {
        try {
            if (!this.cropper) {
                console.error('[ImageCropper] Cropper not initialized');
                return;
            }
            this.cropper.rotate(degrees);
            console.log('[ImageCropper] Image rotated by', degrees, 'degrees');
        } catch (error) {
            console.error('[ImageCropper] Error rotating image:', error);
        }
    },

    /**
     * Zooms the image
     * @param {number} ratio - Zoom ratio (positive to zoom in, negative to zoom out)
     */
    zoom: function (ratio) {
        try {
            if (!this.cropper) {
                console.error('[ImageCropper] Cropper not initialized');
                return;
            }
            this.cropper.zoom(ratio);
            console.log('[ImageCropper] Image zoomed by ratio:', ratio);
        } catch (error) {
            console.error('[ImageCropper] Error zooming image:', error);
        }
    },

    /**
     * Resets the crop box to its initial state
     */
    reset: function () {
        try {
            if (!this.cropper) {
                console.error('[ImageCropper] Cropper not initialized');
                return;
            }
            this.cropper.reset();
            console.log('[ImageCropper] Cropper reset to initial state');
        } catch (error) {
            console.error('[ImageCropper] Error resetting cropper:', error);
        }
    },

    /**
     * Destroys the Cropper.js instance and cleans up
     */
    destroy: function () {
        try {
            if (this.cropper) {
                console.log('[ImageCropper] Destroying cropper instance');
                this.cropper.destroy();
                this.cropper = null;
            }
            this.imageElement = null;
            console.log('[ImageCropper] Cleanup completed');
        } catch (error) {
            console.error('[ImageCropper] Error destroying cropper:', error);
        }
    },

    /**
     * Gets current crop box data
     * @returns {object} Crop box data (x, y, width, height)
     */
    getCropBoxData: function () {
        try {
            if (!this.cropper) {
                console.error('[ImageCropper] Cropper not initialized');
                return null;
            }
            return this.cropper.getCropBoxData();
        } catch (error) {
            console.error('[ImageCropper] Error getting crop box data:', error);
            return null;
        }
    },

    /**
     * Sets crop box data
     * @param {object} data - Crop box data (x, y, width, height)
     */
    setCropBoxData: function (data) {
        try {
            if (!this.cropper) {
                console.error('[ImageCropper] Cropper not initialized');
                return;
            }
            this.cropper.setCropBoxData(data);
            console.log('[ImageCropper] Crop box data updated');
        } catch (error) {
            console.error('[ImageCropper] Error setting crop box data:', error);
        }
    }
};

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// Dialog component for interactive image cropping using Cropper.js.
/// Allows user to crop, zoom, rotate and adjust image before upload.
/// </summary>
public partial class ImageCropperDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private ILogger<ImageCropperDialog> Logger { get; set; } = null!;

    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    /// <summary>
    /// The browser file selected by the user.
    /// </summary>
    [Parameter, EditorRequired]
    public IBrowserFile? SelectedFile { get; set; }

    /// <summary>
    /// Maximum file size in bytes (default: 5MB).
    /// </summary>
    [Parameter]
    public long MaxFileSize { get; set; } = 5 * 1024 * 1024; // 5MB

    /// <summary>
    /// Output format for cropped image (default: image/webp).
    /// </summary>
    [Parameter]
    public string OutputFormat { get; set; } = "image/webp";

    /// <summary>
    /// Quality for lossy compression (0.0 to 1.0, default: 0.95).
    /// </summary>
    [Parameter]
    public double Quality { get; set; } = 0.95;

    /// <summary>
    /// EventCallback invoked when image is successfully cropped.
    /// Receives Base64 data URL of the cropped image.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnImageCropped { get; set; }

    // Component state
    private bool _isLoading = true;
    private bool _isProcessing;
    private string? _errorMessage;
    private string _imageDataUrl = string.Empty;
    private readonly string _imageElementId = $"cropper-image-{Guid.NewGuid():N}";
    private IJSObjectReference? _cropperModule;
    private bool _cropperInitialized;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeAsync();
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing ImageCropperDialog");

            if (SelectedFile == null)
            {
                _errorMessage = "No se seleccionó ningún archivo.";
                _isLoading = false;
                StateHasChanged();
                return;
            }

            // Validate file size
            if (SelectedFile.Size > MaxFileSize)
            {
                var sizeMB = MaxFileSize / (1024.0 * 1024.0);
                _errorMessage = $"El archivo es demasiado grande. Tamaño máximo: {sizeMB:F1} MB.";
                _isLoading = false;
                StateHasChanged();
                return;
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(SelectedFile.ContentType.ToLowerInvariant()))
            {
                _errorMessage = "Formato de archivo no válido. Solo se permiten: JPG, PNG, GIF, WebP.";
                _isLoading = false;
                StateHasChanged();
                return;
            }

            Logger.LogDebug(
                "Loading file for crop - Name: {FileName}, Size: {FileSize} bytes, Type: {ContentType}",
                SelectedFile.Name, SelectedFile.Size, SelectedFile.ContentType);

            // Read file as Base64 data URL
            using var stream = SelectedFile.OpenReadStream(MaxFileSize);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            _imageDataUrl = $"data:{SelectedFile.ContentType};base64,{base64}";

            _isLoading = false;
            StateHasChanged();

            // Wait significantly longer for DOM update and image rendering
            // Base64 images can take time to render, especially large ones
            await Task.Delay(1000);

            // Initialize Cropper.js via JSInterop
            await InitializeCropperAsync();

            Logger.LogInformation("ImageCropperDialog initialized successfully");
        }
        catch (JSException jsEx)
        {
            Logger.LogError(jsEx, "JavaScript error initializing ImageCropperDialog - Message: {JsMessage}", jsEx.Message);
            _errorMessage = $"Error de JavaScript al cargar la imagen: {jsEx.Message}";
            _isLoading = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing ImageCropperDialog - Type: {ExceptionType}, Message: {Message}", 
                ex.GetType().Name, ex.Message);
            _errorMessage = $"Error al cargar la imagen: {ex.Message}";
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task InitializeCropperAsync()
    {
        try
        {
            // Check if ImageCropper namespace exists in JavaScript
            var cropperExists = await JSRuntime.InvokeAsync<bool>("eval", "typeof window.ImageCropper !== 'undefined'");
            if (!cropperExists)
            {
                Logger.LogError("ImageCropper JavaScript module not found - ensure image-cropper.js is loaded");
                _errorMessage = "Error: No se pudo cargar el módulo de recorte. Por favor, recarga la página.";
                StateHasChanged();
                return;
            }

            // Check if Cropper.js library is loaded
            var cropperLibExists = await JSRuntime.InvokeAsync<bool>("eval", "typeof window.Cropper !== 'undefined'");
            if (!cropperLibExists)
            {
                Logger.LogWarning(
                    "Cropper.js library not loaded. " +
                    "Verify local bundle is accessible at: /lib/cropperjs/cropper.min.js " +
                    "If missing, download from: https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.6.2/cropper.min.js " +
                    "and place in wwwroot/lib/cropperjs/");
                _errorMessage = "Error: No se pudo cargar la biblioteca de recorte. Por favor, recarga la página.";
                StateHasChanged();
                return;
            }

            const int maxRetries = 3;
            var retryDelay = 500; // Start with 500ms

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Logger.LogDebug("Initializing Cropper.js for element: {ElementId} (Attempt {Attempt}/{MaxRetries})", 
                        _imageElementId, attempt, maxRetries);

                    var success = await JSRuntime.InvokeAsync<bool>(
                        "ImageCropper.initialize",
                        _imageElementId,
                        new
                        {
                            aspectRatio = 1, // Square for avatar
                            viewMode = 1,
                            dragMode = "move",
                            autoCropArea = 0.9,
                            guides = true,
                            center = true,
                            highlight = true,
                            cropBoxMovable = true,
                            cropBoxResizable = true,
                            responsive = true,
                            modal = true,
                            minContainerWidth = 300,
                            minContainerHeight = 300,
                            minCropBoxWidth = 100,
                            minCropBoxHeight = 100
                        });

                    if (success)
                    {
                        _cropperInitialized = true;
                        Logger.LogInformation("Cropper.js initialized successfully on attempt {Attempt}", attempt);
                        return; // Success, exit method
                    }
                    else
                    {
                        Logger.LogWarning("Cropper.js initialization returned false on attempt {Attempt}/{MaxRetries}", 
                            attempt, maxRetries);

                        // If not last attempt, wait and retry
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(retryDelay);
                            retryDelay *= 2; // Exponential backoff
                        }
                    }
                }
                catch (JSException jsEx)
                {
                    Logger.LogError(jsEx, 
                        "JavaScript error initializing Cropper.js on attempt {Attempt}/{MaxRetries} - Message: {JsMessage}", 
                        attempt, maxRetries, jsEx.Message);

                    // If not last attempt, wait and retry
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelay);
                        retryDelay *= 2; // Exponential backoff
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error initializing Cropper.js on attempt {Attempt}/{MaxRetries} - Type: {ExceptionType}", 
                        attempt, maxRetries, ex.GetType().Name);

                    // If not last attempt, wait and retry
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelay);
                        retryDelay *= 2; // Exponential backoff
                    }
                }
            }

            // All retries failed
            Logger.LogError("Failed to initialize Cropper.js after {MaxRetries} attempts", maxRetries);
            _errorMessage = "Error al inicializar el recortador de imagen. Por favor, intenta con otra imagen o recarga la página.";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Critical error in InitializeCropperAsync - cropper initialization completely failed");
            _errorMessage = $"Error crítico al inicializar el recortador: {ex.Message}";
            StateHasChanged();
        }
    }

    #region Cropper Controls

    private async Task ZoomIn()
    {
        try
        {
            if (!_cropperInitialized) return;
            await JSRuntime.InvokeVoidAsync("ImageCropper.zoom", 0.1);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error zooming in");
        }
    }

    private async Task ZoomOut()
    {
        try
        {
            if (!_cropperInitialized) return;
            await JSRuntime.InvokeVoidAsync("ImageCropper.zoom", -0.1);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error zooming out");
        }
    }

    private async Task RotateLeft()
    {
        try
        {
            if (!_cropperInitialized) return;
            await JSRuntime.InvokeVoidAsync("ImageCropper.rotate", -90);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error rotating left");
        }
    }

    private async Task RotateRight()
    {
        try
        {
            if (!_cropperInitialized) return;
            await JSRuntime.InvokeVoidAsync("ImageCropper.rotate", 90);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error rotating right");
        }
    }

    private async Task Reset()
    {
        try
        {
            if (!_cropperInitialized) return;
            await JSRuntime.InvokeVoidAsync("ImageCropper.reset");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error resetting cropper");
        }
    }

    #endregion

    #region Dialog Actions

    private async Task SaveCroppedImage()
    {
        try
        {
            if (!_cropperInitialized)
            {
                Logger.LogWarning("Attempted to save cropped image but cropper not initialized");
                return;
            }

            _isProcessing = true;
            StateHasChanged();

            Logger.LogInformation(
                "Getting cropped image - Format: {Format}, Quality: {Quality}",
                OutputFormat, Quality);

            // CRÍTICO: Agregar timeout para evitar Blazor circuit disconnect
            // Si el canvas.toDataURL() tarda más de 15 segundos, cancelar
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            string? croppedBase64 = null;

            try
            {
                // Get cropped image as Base64 con timeout
                croppedBase64 = await JSRuntime.InvokeAsync<string>(
                    "ImageCropper.getCroppedImageBase64",
                    cts.Token,
                    OutputFormat,
                    Quality);
            }
            catch (TaskCanceledException)
            {
                Logger.LogError("Timeout getting cropped image (15 seconds exceeded) - image may be too large or cropper is stuck");
                _errorMessage = "Timeout procesando la imagen. Intenta con una imagen más pequeña o recorta menos área.";
                _isProcessing = false;
                StateHasChanged();
                return;
            }

            if (string.IsNullOrEmpty(croppedBase64))
            {
                Logger.LogError("Failed to get cropped image - returned null/empty");
                _errorMessage = "Error al procesar la imagen recortada.";
                _isProcessing = false;
                StateHasChanged();
                return;
            }

            Logger.LogInformation(
                "Cropped image obtained successfully - Size: {SizeKB} KB",
                (croppedBase64.Length / 1024.0).ToString("F2"));

            // CRITICAL FIX: Use EventCallback instead of DialogResult to avoid SignalR payload issues
            Logger.LogInformation("Invoking OnImageCropped EventCallback - Length: {Length} bytes", croppedBase64.Length);

            try
            {
                // DON'T close dialog here - let parent component close it after callback completes successfully
                // This prevents race condition where dialog closes before async operations finish
                await OnImageCropped.InvokeAsync(croppedBase64);

                Logger.LogInformation("OnImageCropped EventCallback invoked successfully - Parent will close dialog after save completes");
            }
            catch (Exception callbackEx)
            {
                Logger.LogError(callbackEx, "CRITICAL: Failed to invoke OnImageCropped callback - Type: {ExceptionType}", callbackEx.GetType().Name);
                throw;
            }
        }
        catch (JSException jsEx)
        {
            Logger.LogError(jsEx, "JavaScript error saving cropped image - Message: {JsMessage}", jsEx.Message);
            _errorMessage = $"Error de JavaScript: {jsEx.Message}";
            _isProcessing = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving cropped image - Type: {ExceptionType}", ex.GetType().Name);
            _errorMessage = $"Error al guardar la imagen: {ex.Message}";
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private void Cancel()
    {
        Logger.LogInformation("Image cropper dialog cancelled by user");
        MudDialog?.Close(DialogResult.Cancel());
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        try
        {
            Logger.LogDebug("Disposing ImageCropperDialog");

            // Destroy Cropper.js instance
            if (_cropperInitialized)
            {
                await JSRuntime.InvokeVoidAsync("ImageCropper.destroy");
            }

            if (_cropperModule != null)
            {
                await _cropperModule.DisposeAsync();
            }

            Logger.LogDebug("ImageCropperDialog disposed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing ImageCropperDialog");
        }
    }

    #endregion
}

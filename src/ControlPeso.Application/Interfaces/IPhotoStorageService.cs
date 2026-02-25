namespace ControlPeso.Application.Interfaces;

/// <summary>
/// Service for managing user photo storage operations.
/// Abstracts file system operations to support different storage strategies (local, cloud, etc.).
/// </summary>
public interface IPhotoStorageService
{
    /// <summary>
    /// Saves a user photo to persistent storage with normalized filename.
    /// Automatically deletes any existing photos for the user before saving the new one.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="fileName">Original filename from upload</param>
    /// <param name="stream">File stream containing photo data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Relative URL path to the saved photo (e.g., "/uploads/avatars/user123_photo.jpg")</returns>
    /// <exception cref="ArgumentException">Thrown if userId is empty or fileName is invalid</exception>
    /// <exception cref="IOException">Thrown if file save operation fails</exception>
    Task<string> SaveUserPhotoAsync(Guid userId, string fileName, Stream stream, CancellationToken ct = default);

    /// <summary>
    /// Deletes all photos associated with a user.
    /// Used during account deletion or when replacing photos.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of files deleted</returns>
    Task<int> DeleteUserPhotosAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a photo exists for the given URL.
    /// </summary>
    /// <param name="photoUrl">Relative URL path to the photo</param>
    /// <returns>True if photo exists, false otherwise</returns>
    bool PhotoExists(string photoUrl);
}

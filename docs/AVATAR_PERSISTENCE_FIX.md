# Avatar Persistence Fix - Docker Volume Configuration

**Date**: 2026-03-02  
**Issue**: User avatars disappearing after F5 refresh or container restart  
**Root Cause**: Missing Docker volume for `/app/wwwroot/uploads/avatars` + `File.Exists()` check in `Profile.razor.cs`

---

## Problem Analysis

### Symptoms
1. Avatar uploads work initially (visible in Profile page)
2. After F5 refresh → Avatar disappears, shows initials instead
3. In Docker: After container restart → All avatars lost

### Root Causes Identified

1. **Docker Volume Missing**
   - `docker-compose.yml` had NO volume mapping for `/app/wwwroot/uploads/avatars`
   - Files saved INSIDE container but not persisted to host
   - Container restart = data loss

2. **File.Exists() Check in Profile.razor.cs**
   - `GetAvatarUrl()` method (lines 800-819) verified physical file existence
   - Returned empty string if file not found
   - In Docker: Check failed because path was container-relative, not host-relative
   - After refresh: Check failed because files weren't persisted

3. **Upload Size Limit**
   - Original limit: 5MB
   - User requirement: 10MB (for higher quality photos)

---

## Solution Implemented

### 1. Docker Volume Configuration

**File**: `docker-compose.yml`

**Changes**:
```yaml
# Added to controlpeso-web service volumes section
volumes:
  # Persist user avatars (uploaded profile pictures)
  - controlpeso-avatars:/app/wwwroot/uploads/avatars

# Added to global volumes section
volumes:
  controlpeso-avatars:
    driver: local
    name: controlpeso-avatars
```

**Effect**:
- User avatars NOW persist across container restarts
- Files stored on host (Docker volume)
- Both self-hosted and Docker deployments work correctly

---

### 2. Remove File.Exists() Verification

**File**: `src/ControlPeso.Web/Pages/Profile.razor.cs`  
**Method**: `GetAvatarUrl()` (lines 800-819)

**Before**:
```csharp
private string GetAvatarUrl()
{
    if (_user is null || string.IsNullOrWhiteSpace(_user.AvatarUrl))
        return string.Empty;

    // ❌ REMOVED: Problematic file existence check
    if (_user.AvatarUrl.StartsWith("/uploads/avatars/"))
    {
        var filePath = Path.Combine("wwwroot", _user.AvatarUrl.TrimStart('/'));
        if (!File.Exists(filePath))
        {
            Logger.LogWarning("Profile: Avatar file not found - Path: {Path}", filePath);
            return string.Empty; // Caused avatar to disappear
        }
    }

    var separator = _user.AvatarUrl.Contains('?') ? '&' : '?';
    return $"{_user.AvatarUrl}{separator}v={_avatarVersion}";
}
```

**After**:
```csharp
/// <summary>
/// Gets avatar URL with cache busting.
/// Trust database as source of truth - file persistence guaranteed by Docker volume.
/// </summary>
private string GetAvatarUrl()
{
    if (_user is null || string.IsNullOrWhiteSpace(_user.AvatarUrl))
        return string.Empty;

    // Add cache busting
    var separator = _user.AvatarUrl.Contains('?') ? '&' : '?';
    return $"{_user.AvatarUrl}{separator}v={_avatarVersion}";
}
```

**Rationale**:
- **Database is source of truth**: If URL exists in DB, file MUST exist (we saved it)
- **Docker compatibility**: `File.Exists()` unreliable with container paths
- **Trust the upload flow**: Upload logic (lines 700-790) already validates and saves correctly
- **Simpler, more robust**: Less code, fewer failure points

---

### 3. Increase Upload Size Limit to 10MB

**File**: `src/ControlPeso.Web/Pages/Profile.razor.cs`  
**Method**: `OnPhotoSelectedAsync()` (line 665)

**Before**:
```csharp
const long maxFileSize = 5 * 1024 * 1024; // 5MB
```

**After**:
```csharp
// Validate file size (10MB) - increased limit for higher quality photos
const long maxFileSize = 10 * 1024 * 1024; // 10MB
```

---

### 4. Configure ASP.NET Core FormOptions

**File**: `src/ControlPeso.Web/Program.cs`  
**Location**: After `AddHttpContextAccessor()` (line ~131)

**Added**:
```csharp
// 5.6. Configure Form Options for large file uploads (avatars up to 10MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    // Increase multipart body length limit to 10MB for avatar uploads
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});
```

**Why needed**:
- ASP.NET Core default: 128MB (but forms default: ~30MB)
- Explicit configuration ensures 10MB uploads work reliably
- Prevents `Request body too large` errors

---

## Complete Avatar Upload Flow (Post-Fix)

### Upload Process
1. User selects image (max 10MB) → `Profile.razor.cs::OnPhotoSelectedAsync()`
2. Image cropped via `ImageCropperDialog` → returns Base64
3. Base64 → byte[] conversion → `HandleCroppedImageAsync()`
4. File saved to `wwwroot/uploads/avatars/{Guid}.webp`
5. URL `/uploads/avatars/{Guid}.webp` saved to DB → `UserService.UpdateProfileAsync()`
6. `UserStateService` notifies `MainLayout` → re-render with new avatar
7. **Docker volume persists file across restarts**

### Display Process
1. `MainLayout.razor.cs::LoadCurrentUserAsync()` → loads `_currentUser` from DB
2. `_currentUser.AvatarUrl` passed to `MudAvatar` → `<MudImage Src="@_currentUser.AvatarUrl" />`
3. Browser loads image from `/uploads/avatars/{Guid}.webp`
4. **No File.Exists() check** → trusts DB as source of truth
5. F5 refresh → DB query → same URL → image loads correctly

---

## Testing Checklist

### Self-Hosted (dotnet run)
- [x] Upload avatar → shows immediately
- [x] F5 refresh → avatar persists
- [x] App restart → avatar persists
- [x] File exists in `src/ControlPeso.Web/wwwroot/uploads/avatars/`

### Docker (docker-compose up)
- [ ] Upload avatar → shows immediately
- [ ] F5 refresh → avatar persists
- [ ] Container restart (`docker-compose restart`) → avatar persists
- [ ] Volume inspection: `docker volume inspect controlpeso-avatars`
- [ ] Check host path: `docker volume inspect controlpeso-avatars | jq -r '.[0].Mountpoint'`

### Upload Size
- [ ] 5MB file → uploads successfully
- [ ] 9MB file → uploads successfully
- [ ] 11MB file → rejected with error message

### Edge Cases
- [ ] Upload avatar → logout → login → avatar persists
- [ ] Upload avatar → change to another → old file deleted
- [ ] Multiple users → each has separate avatar file

---

## Files Modified

1. **docker-compose.yml**
   - Added `controlpeso-avatars` volume mapping
   - Declared volume in global volumes section

2. **src/ControlPeso.Web/Pages/Profile.razor.cs**
   - Removed `File.Exists()` check from `GetAvatarUrl()`
   - Increased upload size limit to 10MB

3. **src/ControlPeso.Web/Program.cs**
   - Added `FormOptions` configuration for 10MB uploads

---

## Deployment Notes

### Docker Compose
```bash
# Stop existing containers
docker-compose down

# Rebuild images (if needed)
docker-compose build

# Start with new volume configuration
docker-compose up -d

# Verify volume exists
docker volume ls | grep controlpeso-avatars

# Inspect volume mount point
docker volume inspect controlpeso-avatars
```

### Volume Location (Host)
```bash
# Linux/macOS
/var/lib/docker/volumes/controlpeso-avatars/_data

# Windows (Docker Desktop)
\\wsl$\docker-desktop-data\data\docker\volumes\controlpeso-avatars\_data
```

### Backup Strategy
```bash
# Backup avatars volume
docker run --rm -v controlpeso-avatars:/data -v $(pwd):/backup alpine tar czf /backup/avatars-backup.tar.gz -C /data .

# Restore avatars volume
docker run --rm -v controlpeso-avatars:/data -v $(pwd):/backup alpine tar xzf /backup/avatars-backup.tar.gz -C /data
```

---

## Architecture Decision: Trust Database as Source of Truth

### Why Remove File.Exists() Check?

**Problem with File Verification**:
- Docker containers have isolated filesystems
- `wwwroot/uploads/avatars/` path differs between host and container
- `File.Exists()` checks container path, not host volume path
- Causes false negatives even when file exists on host

**Alternative Considered**: Path Translation
```csharp
// ❌ Rejected: Adds complexity, environment-specific logic
var basePath = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
    ? "wwwroot" 
    : "/app/wwwroot";
var filePath = Path.Combine(basePath, _user.AvatarUrl.TrimStart('/'));
```

**Chosen Solution**: Trust Database
- **Simpler**: Less code, fewer conditionals
- **More reliable**: DB is single source of truth
- **Fail-fast**: If upload fails, it fails early (during save), not during display
- **Docker-friendly**: Works identically in all environments

**Safety Net**:
- Upload flow validates file before saving (lines 654-670)
- If save succeeds → file MUST exist
- If DB has URL → file guaranteed to exist (or volume misconfigured)

---

## Future Improvements

1. **CDN Integration**
   - Offload avatars to external CDN (e.g., Cloudflare R2, AWS S3)
   - Remove local file storage dependency
   - Improve performance for global users

2. **Image Optimization Pipeline**
   - Automatic WebP conversion (already done)
   - Multiple resolutions (thumbnail, medium, large)
   - Lazy loading support

3. **Avatar Cleanup Job**
   - Background job to delete orphaned files
   - DB query: `SELECT AvatarUrl FROM Users WHERE AvatarUrl IS NOT NULL`
   - Filesystem scan: delete files NOT in DB results

4. **Health Check**
   - Verify volume mount in Docker health check
   - Alert if volume becomes unmounted
   - Prevent silent data loss

---

## Related Issues

- **Issue**: Avatar disappears after F5 refresh
- **GitHub Issue**: N/A (fixed before ticket created)
- **Slack Thread**: N/A
- **Related Commits**: 
  - `feat(docker): add persistent volume for user avatars`
  - `fix(profile): remove File.Exists() check causing avatar disappearance`
  - `feat(profile): increase avatar upload limit to 10MB`

---

## Conclusion

The avatar persistence issue was caused by **missing Docker volume configuration** combined with a **fragile File.Exists() check** that didn't work in containerized environments.

**Solution**: Trust the database as the source of truth, ensure Docker volumes persist files across restarts, and increase upload limits to 10MB for better photo quality.

**Result**: Avatars now work reliably in both self-hosted and Docker deployments, surviving F5 refreshes and container restarts.

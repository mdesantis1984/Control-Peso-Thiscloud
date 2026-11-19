using ControlPeso.Infrastructure.Services;
using ControlPeso.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkiaSharp;

namespace ControlPeso.Web.Tests.Services;

/// <summary>S25: avatars are validated by content, size-limited and re-encoded to WebP on the server.</summary>
public sealed class AvatarUploadServiceTests : IDisposable
{
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), $"avatar-tests-{Guid.NewGuid():N}");
    private readonly string _avatars;
    private readonly AvatarUploadService _sut;

    public AvatarUploadServiceTests()
    {
        _avatars = Path.Combine(_webRoot, "uploads", "avatars");
        Directory.CreateDirectory(_avatars);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.WebRootPath).Returns(_webRoot);

        _sut = new AvatarUploadService(
            new ImageProcessingService(NullLogger<ImageProcessingService>.Instance),
            environment.Object,
            NullLogger<AvatarUploadService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }

    private static byte[] EncodeImage(SKEncodedImageFormat format, int width = 300, int height = 200)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        bitmap.Erase(new SKColor(200, 30, 30));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    private string[] AvatarFiles() => Directory.GetFiles(_avatars);

    // ---------- SaveAsync: accepted ----------

    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task SaveAsync_ValidImage_WritesSquareWebpAtTargetSize(SKEncodedImageFormat format)
    {
        var base64 = Convert.ToBase64String(EncodeImage(format));

        var url = await _sut.SaveAsync($"data:image/png;base64,{base64}");

        url.Should().MatchRegex(@"^/uploads/avatars/[0-9a-f\-]{36}\.webp$");
        var file = Path.Combine(_avatars, Path.GetFileName(url));
        File.Exists(file).Should().BeTrue();
        AvatarFiles().Should().ContainSingle();

        var bytes = await File.ReadAllBytesAsync(file);
        using var codec = SKCodec.Create(SKData.CreateCopy(bytes));
        codec.EncodedFormat.Should().Be(SKEncodedImageFormat.Webp);
        codec.Info.Width.Should().Be(256);
        codec.Info.Height.Should().Be(256);
    }

    [Fact]
    public async Task SaveAsync_RawBase64WithoutDataUrlPrefix_IsAccepted()
    {
        var url = await _sut.SaveAsync(Convert.ToBase64String(EncodeImage(SKEncodedImageFormat.Png)));

        File.Exists(Path.Combine(_avatars, Path.GetFileName(url))).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_DoesNotReuseTheClientSuppliedName_AndGeneratesDistinctNames()
    {
        var base64 = Convert.ToBase64String(EncodeImage(SKEncodedImageFormat.Png));

        var first = await _sut.SaveAsync(base64);
        var second = await _sut.SaveAsync(base64);

        first.Should().NotBe(second);
        AvatarFiles().Should().HaveCount(2);
    }

    // ---------- SaveAsync: rejected, nothing written ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("data:image/png;base64,")]
    public async Task SaveAsync_EmptyInput_IsRejectedAndNothingIsWritten(string? input)
    {
        var act = () => _sut.SaveAsync(input);

        (await act.Should().ThrowAsync<AvatarRejectedException>())
            .Which.Reason.Should().Be(AvatarRejectionReason.Empty);
        AvatarFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_EncodedPayloadAboveTheLimit_IsRejectedBeforeDecodingAndNothingIsWritten()
    {
        // Not valid base64 at all: only a length check can reject it with TooLarge (it is never decoded).
        var input = new string('!', AvatarUploadService.MaxEncodedLength + 4);

        var act = () => _sut.SaveAsync(input);

        (await act.Should().ThrowAsync<AvatarRejectedException>())
            .Which.Reason.Should().Be(AvatarRejectionReason.TooLarge);
        AvatarFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_DecodedPayloadAboveTheLimit_IsRejectedAndNothingIsWritten()
    {
        var bytes = new byte[AvatarUploadService.MaxImageBytes + 1];
        bytes[0] = 0x89;

        var act = () => _sut.SaveAsync(Convert.ToBase64String(bytes));

        (await act.Should().ThrowAsync<AvatarRejectedException>())
            .Which.Reason.Should().Be(AvatarRejectionReason.TooLarge);
        AvatarFiles().Should().BeEmpty();
    }

    [Theory]
    [InlineData("this is not base64 !!!")]
    [InlineData("data:image/png;base64,@@@@")]
    public async Task SaveAsync_InvalidBase64_IsRejectedAndNothingIsWritten(string input)
    {
        var act = () => _sut.SaveAsync(input);

        (await act.Should().ThrowAsync<AvatarRejectedException>())
            .Which.Reason.Should().Be(AvatarRejectionReason.InvalidEncoding);
        AvatarFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_NonImageBytes_AreRejectedAndNothingIsWritten()
    {
        var html = System.Text.Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>");

        var act = () => _sut.SaveAsync($"data:image/webp;base64,{Convert.ToBase64String(html)}");

        (await act.Should().ThrowAsync<AvatarRejectedException>())
            .Which.Reason.Should().Be(AvatarRejectionReason.InvalidImage);
        AvatarFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_TruncatedImage_IsRejectedAndNothingIsWritten()
    {
        var png = EncodeImage(SKEncodedImageFormat.Png);

        var act = () => _sut.SaveAsync(Convert.ToBase64String(png.AsSpan(0, 20).ToArray()));

        (await act.Should().ThrowAsync<AvatarRejectedException>())
            .Which.Reason.Should().Be(AvatarRejectionReason.InvalidImage);
        AvatarFiles().Should().BeEmpty();
    }

    // ---------- TryDeleteLocalAvatar ----------

    [Fact]
    public void TryDeleteLocalAvatar_LocalAvatarInsideTheFolder_IsDeleted()
    {
        var file = Path.Combine(_avatars, "0b6f2a4e-1111-2222-3333-444455556666.webp");
        File.WriteAllText(file, "x");

        var deleted = _sut.TryDeleteLocalAvatar("/uploads/avatars/0b6f2a4e-1111-2222-3333-444455556666.webp");

        deleted.Should().BeTrue();
        File.Exists(file).Should().BeFalse();
    }

    [Fact]
    public void TryDeleteLocalAvatar_MissingFile_ReturnsFalse()
    {
        _sut.TryDeleteLocalAvatar("/uploads/avatars/missing.webp").Should().BeFalse();
    }

    [Theory]
    [InlineData("/uploads/avatars/../../appsettings.json")]
    [InlineData("/uploads/avatars/../../secret.webp")]
    [InlineData("/uploads/avatars/..%2f..%2fsecret.webp")]
    [InlineData("/uploads/avatars/sub/../../../secret.webp")]
    [InlineData("/uploads/avatars/..\\..\\secret.webp")]
    [InlineData("/uploads/avatars/..webp")]
    [InlineData("/uploads/avatars/other.png")]
    [InlineData("/uploads/other/secret.webp")]
    [InlineData("uploads/avatars/secret.webp")]
    [InlineData("https://lh3.googleusercontent.com/a/photo.webp")]
    [InlineData("//evil.example/uploads/avatars/secret.webp")]
    [InlineData("")]
    [InlineData(null)]
    public void TryDeleteLocalAvatar_NonLocalOrEscapingUrl_DeletesNothing(string? url)
    {
        // Sentinels both inside the web root and one level above the avatars folder.
        var secret = Path.Combine(_webRoot, "secret.webp");
        var appSettings = Path.Combine(_webRoot, "appsettings.json");
        var siblingInUploads = Path.Combine(_webRoot, "uploads", "secret.webp");
        File.WriteAllText(secret, "x");
        File.WriteAllText(appSettings, "x");
        File.WriteAllText(siblingInUploads, "x");

        var deleted = _sut.TryDeleteLocalAvatar(url);

        deleted.Should().BeFalse();
        File.Exists(secret).Should().BeTrue();
        File.Exists(appSettings).Should().BeTrue();
        File.Exists(siblingInUploads).Should().BeTrue();
    }

    [Fact]
    public void TryDeleteLocalAvatar_ExternalUrl_DoesNotTouchALocalFileWithTheSameName()
    {
        var file = Path.Combine(_avatars, "photo.webp");
        File.WriteAllText(file, "x");

        _sut.TryDeleteLocalAvatar("https://example.com/uploads/avatars/photo.webp").Should().BeFalse();

        File.Exists(file).Should().BeTrue();
    }

    // ---------- Limits ----------

    [Fact]
    public void Limits_HubMessageSizeAccommodatesTheLargestAcceptedPayload()
    {
        AvatarUploadService.MaxEncodedLength.Should().Be((AvatarUploadService.MaxImageBytes + 2) / 3 * 4);
        AvatarUploadService.MaxHubMessageBytes.Should()
            .BeGreaterThan(AvatarUploadService.MaxEncodedLength + 1024)
            .And.BeLessThanOrEqualTo(16 * 1024 * 1024);
    }
}

using AICreativeArts.Infrastructure;
using AICreativeArts.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AICreativeArts.Tests;

public class ImageStorageTests : IDisposable
{
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
    private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1 };

    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), "aiunmasked-storage-" + Guid.NewGuid().ToString("N"));
    private readonly ImageStorage _storage;

    public ImageStorageTests()
    {
        Directory.CreateDirectory(_webRoot);
        _storage = new ImageStorage(new TestEnvironment(_webRoot),
            Options.Create(new UploadOptions { FolderName = "uploads", MaxFileBytes = 1024 }),
            NullLogger<ImageStorage>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("art.png", true)]
    [InlineData("art.PNG", true)]
    [InlineData("art.jpg", false)] // PNG bytes with a JPG name
    [InlineData("art.exe", false)]
    [InlineData("art", false)]
    public async Task Validate_checks_extension_and_file_signature(string name, bool expectedValid)
    {
        var error = await _storage.ValidateAsync(CreateFile(name, PngHeader));

        Assert.Equal(expectedValid, error is null);
    }

    [Fact]
    public async Task Validate_accepts_real_jpeg_header()
    {
        Assert.Null(await _storage.ValidateAsync(CreateFile("photo.jpeg", JpegHeader)));
    }

    [Fact]
    public async Task Validate_rejects_empty_and_oversized_files()
    {
        Assert.NotNull(await _storage.ValidateAsync(CreateFile("empty.png", Array.Empty<byte>())));
        Assert.Contains("smaller than", await _storage.ValidateAsync(CreateFile("big.png", new byte[2048])));
    }

    [Theory]
    [InlineData("../../etc/passwd.png", "passwd-")]
    [InlineData("Mona Lisa!! (final).JPG", "mona-lisa-final-")]
    [InlineData("???.png", "ai-image-")]
    public void Safe_filename_strips_paths_and_symbols(string original, string expectedPrefix)
    {
        var safe = ImageStorage.CreateSafeFilename(original);

        Assert.StartsWith(expectedPrefix, safe);
        Assert.Equal(Path.GetFileName(safe), safe);
        Assert.Matches("^[a-z0-9-]+\\.[a-z]+$", safe);
    }

    [Fact]
    public async Task Save_then_delete_round_trip()
    {
        var name = await _storage.SaveAsync(CreateFile("round trip.png", PngHeader));
        var path = Path.Combine(_webRoot, "uploads", name);
        Assert.True(File.Exists(path));

        Assert.True(_storage.Delete(name));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Delete_refuses_paths_outside_the_upload_folder()
    {
        var outside = Path.Combine(_webRoot, "keep.txt");
        File.WriteAllText(outside, "important");

        Assert.False(_storage.Delete("../keep.txt"));
        Assert.True(File.Exists(outside));
    }

    private static IFormFile CreateFile(string name, byte[] bytes) =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "ImageFile", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream",
        };

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public TestEnvironment(string webRoot)
        {
            WebRootPath = webRoot;
            ContentRootPath = webRoot;
        }

        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Testing";
    }
}

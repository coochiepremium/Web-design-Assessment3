using System.Text;
using System.Text.RegularExpressions;
using AICreativeArts.Infrastructure;
using Microsoft.Extensions.Options;

namespace AICreativeArts.Services;

public interface IImageStorage
{
    /// <summary>Checks size, extension and file signature. Returns an error message or null when valid.</summary>
    Task<string?> ValidateAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Saves a validated file under wwwroot/uploads and returns the stored filename.</summary>
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Deletes a stored file. Never throws; returns false when the file could not be removed.</summary>
    bool Delete(string? filename);
}

public partial class ImageStorage : IImageStorage
{
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        [".gif"] = new[] { Encoding.ASCII.GetBytes("GIF87a"), Encoding.ASCII.GetBytes("GIF89a") },
        [".webp"] = new[] { Encoding.ASCII.GetBytes("RIFF") },
    };

    public static IReadOnlyCollection<string> AllowedExtensions => Signatures.Keys;

    private readonly string _folder;
    private readonly UploadOptions _options;
    private readonly ILogger<ImageStorage> _logger;

    public ImageStorage(IWebHostEnvironment environment, IOptions<UploadOptions> options, ILogger<ImageStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        _folder = Path.GetFullPath(Path.Combine(environment.WebRootPath, _options.FolderName));
    }

    public async Task<string?> ValidateAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            return "The selected file is empty. Please choose an image.";
        }

        if (file.Length > _options.MaxFileBytes)
        {
            return $"The image is {FormatSize(file.Length)}. Please choose an image smaller than {FormatSize(_options.MaxFileBytes)}.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !Signatures.TryGetValue(extension, out var signatures))
        {
            return "Only JPG, PNG, GIF or WEBP images can be uploaded.";
        }

        var header = new byte[12];
        int read;
        try
        {
            await using var stream = file.OpenReadStream();
            read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not read uploaded file {FileName}", file.FileName);
            return "The file could not be read. Please try uploading it again.";
        }

        var matches = signatures.Any(signature => read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));
        if (matches && extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            matches = read >= 12 && Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
        }

        return matches ? null : "This file is not a valid image (its contents do not match its extension).";
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_folder);
        var filename = CreateSafeFilename(file.FileName);
        var path = Path.Combine(_folder, filename);

        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(target, cancellationToken);
        _logger.LogInformation("Saved uploaded image {Filename} ({Bytes} bytes)", filename, file.Length);
        return filename;
    }

    public bool Delete(string? filename)
    {
        var path = ResolvePath(filename);
        if (path is null || !File.Exists(path))
        {
            return false;
        }

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete image file {Filename}", filename);
            return false;
        }
    }

    /// <summary>
    /// Builds a readable, unique filename such as "golden-canary-3f9a2c1b.jpg". The
    /// user's filename is reduced to safe characters so it can never escape the folder.
    /// </summary>
    public static string CreateSafeFilename(string originalName)
    {
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        var stem = Path.GetFileNameWithoutExtension(originalName).ToLowerInvariant();
        stem = UnsafeCharacters().Replace(stem, "-").Trim('-');
        if (stem.Length > 40)
        {
            stem = stem[..40].Trim('-');
        }

        if (stem.Length == 0)
        {
            stem = "ai-image";
        }

        return $"{stem}-{Guid.NewGuid().ToString("N")[..8]}{extension}";
    }

    private string? ResolvePath(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename) || filename != Path.GetFileName(filename))
        {
            return null;
        }

        var path = Path.GetFullPath(Path.Combine(_folder, filename));
        return path.StartsWith(_folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? path : null;
    }

    private static string FormatSize(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.#} MB" : $"{Math.Max(1, bytes / 1024)} KB";

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex UnsafeCharacters();
}

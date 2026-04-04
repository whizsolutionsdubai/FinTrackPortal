using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FinTrackPortal.API.Services;

public sealed class ProfilePhotoService : IProfilePhotoService
{
    private const int Size = 200;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public ProfilePhotoService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    public async Task<(bool Ok, string UrlOrError)> SaveProfilePhotoAsync(long memberId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return (false, "No file uploaded.");

        var ct = file.ContentType?.ToLowerInvariant() ?? "";
        if (ct is not ("image/jpeg" or "image/jpg" or "image/png" or "image/pjpeg"))
            return (false, "Only JPEG or PNG images are allowed.");

        await using var input = file.OpenReadStream();
        using var image = await Image.LoadAsync(input, cancellationToken);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(Size, Size),
            Mode = ResizeMode.Crop
        }));

        await using var jpegStream = new MemoryStream();
        await image.SaveAsJpegAsync(jpegStream, cancellationToken);
        jpegStream.Position = 0;

        var provider = _config["AttachmentStorage:Provider"]?.Trim() ?? "Azure";
        if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
            return await SaveLocalAsync(memberId, jpegStream, cancellationToken);

        return await SaveAzureAsync(memberId, jpegStream, cancellationToken);
    }

    private async Task<(bool Ok, string UrlOrError)> SaveLocalAsync(long memberId, Stream jpegStream, CancellationToken cancellationToken)
    {
        var configuredPath = _config["LocalStorage:Path"]
            ?? throw new InvalidOperationException("LocalStorage:Path is required when AttachmentStorage:Provider is Local.");
        var basePath = LocalFileStorageService.ResolvePhysicalStoragePath(configuredPath, _env);
        var profilesDir = Path.Combine(basePath, "profiles");
        if (!Directory.Exists(profilesDir))
            Directory.CreateDirectory(profilesDir);

        var fileName = $"{memberId}.jpg";
        var physicalPath = Path.Combine(profilesDir, fileName);
        await using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await jpegStream.CopyToAsync(fs, cancellationToken);
        }

        var publicBase = (_config["LocalStorage:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(publicBase))
            return (false, "LocalStorage:PublicBaseUrl is not configured.");

        var url = $"{publicBase}/profiles/{fileName}";
        return (true, url);
    }

    private async Task<(bool Ok, string UrlOrError)> SaveAzureAsync(long memberId, Stream jpegStream, CancellationToken cancellationToken)
    {
        var connectionString = _config["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is missing.");
        var containerName = _config["AzureStorage:ContainerName"] ?? "attachments";
        var blobName = $"profiles/{memberId}.jpg";

        var client = new BlobContainerClient(connectionString, containerName);
        var blob = client.GetBlobClient(blobName);

        jpegStream.Position = 0;
        await blob.UploadAsync(jpegStream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
        }, cancellationToken);

        return (true, blob.Uri.ToString());
    }

    public async Task TryDeleteByUrlAsync(string? profilePhotoUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profilePhotoUrl))
            return;

        try
        {
            var provider = _config["AttachmentStorage:Provider"]?.Trim() ?? "Azure";
            if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
            {
                var configuredPath = _config["LocalStorage:Path"];
                if (string.IsNullOrWhiteSpace(configuredPath))
                    return;
                var basePath = LocalFileStorageService.ResolvePhysicalStoragePath(configuredPath, _env);
                var uri = new Uri(profilePhotoUrl);
                var name = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrEmpty(name))
                    return;
                var path = Path.Combine(basePath, "profiles", name);
                if (File.Exists(path))
                    File.Delete(path);
            }
            else
            {
                var connectionString = _config["AzureStorage:ConnectionString"];
                var containerName = _config["AzureStorage:ContainerName"] ?? "attachments";
                if (string.IsNullOrEmpty(connectionString))
                    return;
                var uri = new Uri(profilePhotoUrl);
                var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2)
                    return;
                var blobName = string.Join("/", segments.Skip(1));
                var client = new BlobContainerClient(connectionString, containerName);
                await client.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
        }
        catch
        {
            // best-effort delete
        }
    }
}

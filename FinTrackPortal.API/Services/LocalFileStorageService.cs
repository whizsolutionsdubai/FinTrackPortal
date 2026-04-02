namespace FinTrackPortal.API.Services
{
    /// <summary>
    /// Saves attachments under a configured folder and builds public URLs (same DB shape as Azure — FileUrl only).
    /// Pair with <c>UseStaticFiles</c> mapping request path <c>/attachments</c> to the physical folder.
    /// Relative configuration paths are resolved under the app content root.
    /// </summary>
    public class LocalFileStorageService : IAttachmentStorageService
    {
        private readonly string _storagePath;
        private readonly string _publicBaseUrl;

        /// <summary>Resolves <paramref name="configuredPath"/> the same way as upload/delete (for static files middleware).</summary>
        public static string ResolvePhysicalStoragePath(string configuredPath, IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new ArgumentException("Local storage path is empty.", nameof(configuredPath));

            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(env.ContentRootPath, configuredPath));
        }

        public LocalFileStorageService(IConfiguration config, IWebHostEnvironment env)
        {
            var configuredPath = config["LocalStorage:Path"]
                ?? throw new InvalidOperationException("LocalStorage:Path is required when AttachmentStorage:Provider is Local.");
            _storagePath = ResolvePhysicalStoragePath(configuredPath, env);

            _publicBaseUrl = (config["LocalStorage:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(_publicBaseUrl))
                throw new InvalidOperationException("LocalStorage:PublicBaseUrl is required when AttachmentStorage:Provider is Local (e.g. https://api.example.com/attachments).");
        }

        public async Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);

            var extension = Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_storagePath, uniqueName);

            await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return $"{_publicBaseUrl}/{uniqueName}";
        }

        public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return Task.CompletedTask;

            try
            {
                var uri = new Uri(fileUrl);
                var fileName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrEmpty(fileName))
                    return Task.CompletedTask;

                var physicalPath = Path.Combine(_storagePath, fileName);
                if (File.Exists(physicalPath))
                    File.Delete(physicalPath);
            }
            catch (UriFormatException)
            {
                // Ignore malformed URLs from legacy data
            }

            return Task.CompletedTask;
        }
    }
}

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FinTrackPortal.API.Services
{
    /// <summary>
    /// Uploads and deletes files in Azure Blob Storage.
    /// Files are stored with a GUID-based name to avoid collisions;
    /// the returned URL is saved to the database by the repository layer.
    /// </summary>
    public class BlobStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobStorageService(IConfiguration config)
        {
            _connectionString = config["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is missing from configuration.");
            _containerName = config["AzureStorage:ContainerName"] ?? "attachments";
        }

        /// <summary>Upload a file and return its public blob URL.</summary>
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}{extension}";

            var client = new BlobContainerClient(_connectionString, _containerName);
            var blob = client.GetBlobClient(uniqueName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            };

            using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, options);

            return blob.Uri.ToString();
        }

        /// <summary>Delete a blob by its full URL.</summary>
        public async Task DeleteFileAsync(string fileUrl)
        {
            var uri = new Uri(fileUrl);
            var blobName = Path.GetFileName(uri.LocalPath);

            var client = new BlobContainerClient(_connectionString, _containerName);
            await client.GetBlobClient(blobName).DeleteIfExistsAsync();
        }
    }
}

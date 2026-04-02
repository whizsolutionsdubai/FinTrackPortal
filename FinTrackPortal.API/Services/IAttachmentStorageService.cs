namespace FinTrackPortal.API.Services
{
    /// <summary>
    /// Persists uploaded receipt/invoice files (Azure Blob or local disk) and returns a public URL for the database.
    /// </summary>
    public interface IAttachmentStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default);

        Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    }
}

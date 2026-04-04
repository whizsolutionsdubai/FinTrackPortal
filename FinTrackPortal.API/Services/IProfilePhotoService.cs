namespace FinTrackPortal.API.Services;

/// <summary>Resize profile images and persist (local disk or Azure blob).</summary>
public interface IProfilePhotoService
{
    /// <summary>Returns public URL for stored JPEG, or failure message.</summary>
    Task<(bool Ok, string UrlOrError)> SaveProfilePhotoAsync(long memberId, IFormFile file, CancellationToken cancellationToken = default);

    Task TryDeleteByUrlAsync(string? profilePhotoUrl, CancellationToken cancellationToken = default);
}

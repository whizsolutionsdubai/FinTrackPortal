namespace FinTrackPortal.Interfaces;

/// <summary>Security audit trail (Phase 2 hardening).</summary>
public interface IAuditLogRepository
{
    Task WriteAsync(long? memberId, string action, string? ipAddress, string? userAgent, bool success, string? details);
}

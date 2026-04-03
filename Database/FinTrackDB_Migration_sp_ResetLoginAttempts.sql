-- Optional: run if you already applied SecurityPhase2 before sp_ResetLoginAttempts was added.
-- FinShare Phase 1 doc name; delegates to sp_ClearFailedLogins (FailedLoginCount / LockoutUntil).
SET NOCOUNT ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ResetLoginAttempts
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_ClearFailedLogins @UserName = @UserName;
END
GO

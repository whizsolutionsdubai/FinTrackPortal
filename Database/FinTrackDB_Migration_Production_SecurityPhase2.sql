/*
  FinTrack / FinShare — Phase 2 Security Hardening
  Rate limiting + lockout + audit logs + token cleanup are implemented in the API;
  Run this script on FinTrackDB after Phase 1 auth columns exist.

  Reference: FinShare_SecurityHardening_V3.1.pdf, MasterPendingTasks V4 Phase 2
*/

SET NOCOUNT ON;
GO

-- ---------------------------------------------------------------------------
-- 1. Account lockout columns on Users
-- ---------------------------------------------------------------------------
IF COL_LENGTH('dbo.Users', 'FailedLoginCount') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD FailedLoginCount INT NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Users', 'LockoutUntil') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD LockoutUntil DATETIME NULL;
END
GO

-- ---------------------------------------------------------------------------
-- 2. Audit log tables
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.AuditLogs_Archive', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs_Archive (
        Id          BIGINT        NOT NULL,
        MemberId    BIGINT        NULL,
        Action      NVARCHAR(50)  NOT NULL,
        IPAddress   NVARCHAR(50)  NULL,
        UserAgent   NVARCHAR(500) NULL,
        Success     BIT           NOT NULL,
        Details     NVARCHAR(500) NULL,
        CreatedAt   DATETIME      NOT NULL
    );
    CREATE CLUSTERED INDEX IX_AuditLogs_Archive_CreatedAt ON dbo.AuditLogs_Archive(CreatedAt);
END
GO

IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        Id          BIGINT        IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MemberId    BIGINT        NULL,
        Action      NVARCHAR(50)  NOT NULL,
        IPAddress   NVARCHAR(50)  NULL,
        UserAgent   NVARCHAR(500) NULL,
        Success     BIT           NOT NULL,
        Details     NVARCHAR(500) NULL,
        CreatedAt   DATETIME      NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (GETUTCDATE())
    );
    CREATE NONCLUSTERED INDEX IX_AuditLogs_MemberId ON dbo.AuditLogs(MemberId);
    CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAt);
END
GO

-- ---------------------------------------------------------------------------
-- 3. sp_ValidateUser — include lockout fields (login identifier = EmailAddress)
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_ValidateUser
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.MemberId,
           u.PasswordHash,
           u.IsEmailVerified,
           u.LockoutUntil,
           u.FailedLoginCount
    FROM dbo.Users u
    INNER JOIN dbo.Member m ON m.MemberId = u.MemberId
    WHERE u.EmailAddress = @UserName
      AND u.IsActive = 1
      AND m.IsActive = 1;
END
GO

-- ---------------------------------------------------------------------------
-- 4. Failed login / clear lockout (identifier = email used at login)
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_RecordFailedLogin
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET FailedLoginCount = FailedLoginCount + 1,
        LockoutUntil = CASE
            WHEN FailedLoginCount + 1 >= 5 THEN DATEADD(MINUTE, 15, GETUTCDATE())
            ELSE LockoutUntil
        END
    WHERE EmailAddress = @UserName AND IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ClearFailedLogins
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET FailedLoginCount = 0,
        LockoutUntil = NULL
    WHERE EmailAddress = @UserName AND IsActive = 1;
END
GO

-- ---------------------------------------------------------------------------
-- 5. Expired token cleanup
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CleanupExpiredTokens
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET EmailVerifyToken = NULL,
        EmailVerifyExpiry = NULL
    WHERE EmailVerifyExpiry < GETUTCDATE()
      AND IsEmailVerified = 0
      AND IsActive = 1;

    UPDATE dbo.Users
    SET PasswordResetToken = NULL,
        PasswordResetExpiry = NULL
    WHERE PasswordResetExpiry < GETUTCDATE()
      AND IsActive = 1;
END
GO

-- ---------------------------------------------------------------------------
-- 6. Audit log write
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_WriteAuditLog
    @MemberId  BIGINT        = NULL,
    @Action    NVARCHAR(50),
    @IPAddress NVARCHAR(50)  = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @Success   BIT,
    @Details   NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.AuditLogs (MemberId, Action, IPAddress, UserAgent, Success, Details)
    VALUES (@MemberId, @Action, @IPAddress, @UserAgent, @Success, @Details);
END
GO

-- ---------------------------------------------------------------------------
-- 7. Audit retention (90d primary → archive, 1y archive delete)
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_ArchiveAuditLogsRetention
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.AuditLogs_Archive (Id, MemberId, Action, IPAddress, UserAgent, Success, Details, CreatedAt)
    SELECT Id, MemberId, Action, IPAddress, UserAgent, Success, Details, CreatedAt
    FROM dbo.AuditLogs
    WHERE CreatedAt < DATEADD(DAY, -90, GETUTCDATE());

    DELETE FROM dbo.AuditLogs
    WHERE CreatedAt < DATEADD(DAY, -90, GETUTCDATE());

    DELETE FROM dbo.AuditLogs_Archive
    WHERE CreatedAt < DATEADD(YEAR, -1, GETUTCDATE());
END
GO

-- ---------------------------------------------------------------------------
-- 8. Email verification status (resend flow)
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetUserEmailVerificationStatus
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
        u.MemberId,
        m.MemberName,
        u.IsEmailVerified
    FROM dbo.Users u
    INNER JOIN dbo.Member m ON m.MemberId = u.MemberId
    WHERE u.EmailAddress = @Email
      AND u.IsActive = 1
      AND m.IsActive = 1;
END
GO

-- ---------------------------------------------------------------------------
-- 9. Verify email — return MemberId on success for audit
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_VerifyEmail
    @Token NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MemberId BIGINT;
    DECLARE @Expiry DATETIME;

    SELECT @MemberId = MemberId, @Expiry = EmailVerifyExpiry
    FROM dbo.Users
    WHERE EmailVerifyToken = @Token AND IsActive = 1;

    IF @MemberId IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, N'Invalid or expired link' AS Message, CAST(NULL AS BIGINT) AS MemberId;
        RETURN;
    END

    IF @Expiry < GETUTCDATE()
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, N'Invalid or expired link' AS Message, CAST(NULL AS BIGINT) AS MemberId;
        RETURN;
    END

    UPDATE dbo.Users
    SET IsEmailVerified = 1,
        EmailVerifyToken = NULL,
        EmailVerifyExpiry = NULL
    WHERE MemberId = @MemberId AND EmailVerifyToken = @Token;

    SELECT CAST(1 AS BIT) AS Success, N'Email verified successfully' AS Message, @MemberId AS MemberId;
END
GO

-- ---------------------------------------------------------------------------
-- 10. Reset password — return MemberId when a row was updated
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_ResetPassword
    @Token NVARCHAR(200),
    @NewPasswordHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MemberId BIGINT;

    SELECT @MemberId = MemberId
    FROM dbo.Users
    WHERE PasswordResetToken = @Token
      AND PasswordResetExpiry IS NOT NULL
      AND PasswordResetExpiry > GETUTCDATE()
      AND IsActive = 1;

    IF @MemberId IS NULL
    BEGIN
        SELECT 0 AS RowsUpdated, CAST(NULL AS BIGINT) AS MemberId;
        RETURN;
    END

    UPDATE dbo.Users
    SET PasswordHash = @NewPasswordHash,
        PasswordResetToken = NULL,
        PasswordResetExpiry = NULL,
        FailedLoginCount = 0,
        LockoutUntil = NULL
    WHERE MemberId = @MemberId AND PasswordResetToken = @Token;

    SELECT @@ROWCOUNT AS RowsUpdated, @MemberId AS MemberId;
END
GO

-- ---------------------------------------------------------------------------
-- 11. Forgot-password lookup — include MemberId for audit
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetMemberNameByEmail
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) m.MemberName, u.MemberId
    FROM dbo.Users u
    INNER JOIN dbo.Member m ON m.MemberId = u.MemberId
    WHERE u.EmailAddress = @Email AND u.IsActive = 1 AND m.IsActive = 1;
END
GO

-- ---------------------------------------------------------------------------
-- 12. Alias per FinShare Phase 1 naming (LoginAttempts reset = clear lockout)
-- ---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_ResetLoginAttempts
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_ClearFailedLogins @UserName = @UserName;
END
GO

PRINT 'FinTrackDB_Migration_Production_SecurityPhase2.sql completed.';
GO

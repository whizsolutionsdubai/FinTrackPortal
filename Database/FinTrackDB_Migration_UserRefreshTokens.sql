/*
  FinShare Phase 2 — JWT refresh tokens (httpOnly cookie).
  Run after FinTrackDB_Migration_Production_SecurityPhase2.sql.
  Reference: FinShare_ForAbhilash_TrueStatus_v5.pdf
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.UserRefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRefreshTokens (
        TokenId     INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MemberId    BIGINT        NOT NULL,
        Token       NVARCHAR(500) NOT NULL,
        ExpiresAt   DATETIME      NOT NULL,
        IsRevoked   BIT           NOT NULL CONSTRAINT DF_UserRefreshTokens_IsRevoked DEFAULT (0),
        CreatedAt   DATETIME      NOT NULL CONSTRAINT DF_UserRefreshTokens_CreatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT UQ_UserRefreshTokens_Token UNIQUE (Token),
        CONSTRAINT FK_UserRefreshTokens_Member FOREIGN KEY (MemberId) REFERENCES dbo.Member (MemberId)
    );
    CREATE NONCLUSTERED INDEX IX_UserRefreshTokens_MemberId ON dbo.UserRefreshTokens(MemberId);
    CREATE NONCLUSTERED INDEX IX_UserRefreshTokens_ExpiresAt ON dbo.UserRefreshTokens(ExpiresAt);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveRefreshToken
    @MemberId  BIGINT,
    @Token      NVARCHAR(500),
    @ExpiresAt  DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.UserRefreshTokens (MemberId, Token, ExpiresAt)
    VALUES (@MemberId, @Token, @ExpiresAt);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ValidateRefreshToken
    @Token NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) MemberId
    FROM dbo.UserRefreshTokens
    WHERE Token = @Token
      AND IsRevoked = 0
      AND ExpiresAt > GETUTCDATE();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RevokeRefreshToken
    @Token NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UserRefreshTokens
    SET IsRevoked = 1
    WHERE Token = @Token;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RevokeAllUserRefreshTokens
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UserRefreshTokens
    SET IsRevoked = 1
    WHERE MemberId = @MemberId AND IsRevoked = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetUserEmailByMemberId
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) u.EmailAddress
    FROM dbo.Users u
    INNER JOIN dbo.Member m ON m.MemberId = u.MemberId
    WHERE u.MemberId = @MemberId
      AND u.IsActive = 1
      AND m.IsActive = 1;
END
GO

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

    DELETE FROM dbo.UserRefreshTokens
    WHERE ExpiresAt < GETUTCDATE();
END
GO

PRINT 'FinTrackDB_Migration_UserRefreshTokens.sql completed.';
GO

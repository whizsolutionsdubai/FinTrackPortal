/*
    FinShare — PRODUCTION migration: email verification, password reset, strong password (API-side)
    ============================================================================================
    Apply to an existing FinTrackDB after Phase 3 migration (or any DB with dbo.Users + BCrypt).

    • Adds Users columns: IsEmailVerified, EmailVerifyToken, EmailVerifyExpiry,
      PasswordResetToken, PasswordResetExpiry
    • Replaces sp_ValidateUser, sp_GetExpiry, sp_RegisterUser
    • Adds: sp_SaveEmailVerifyToken, sp_VerifyEmail, sp_SavePasswordResetToken, sp_ResetPassword,
      sp_GetMemberNameByEmail

    Before run: BACKUP database. After run: set Email:Enabled + SMTP in appsettings and deploy API.

    Existing users: script sets IsEmailVerified = 1 for all current rows so they can still log in.
    New registrations via API will be IsEmailVerified = 0 until they use verify-email.
*/

SET NOCOUNT ON;
GO

USE [FinTrackDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Columns on Users
IF COL_LENGTH(N'dbo.Users', N'IsEmailVerified') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [IsEmailVerified] [bit] NOT NULL CONSTRAINT [DF_Users_IsEmailVerified] DEFAULT (0);
END
GO

IF COL_LENGTH(N'dbo.Users', N'EmailVerifyToken') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [EmailVerifyToken] [nvarchar](200) NULL;
END
GO

IF COL_LENGTH(N'dbo.Users', N'EmailVerifyExpiry') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [EmailVerifyExpiry] [datetime] NULL;
END
GO

IF COL_LENGTH(N'dbo.Users', N'PasswordResetToken') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [PasswordResetToken] [nvarchar](200) NULL;
END
GO

IF COL_LENGTH(N'dbo.Users', N'PasswordResetExpiry') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [PasswordResetExpiry] [datetime] NULL;
END
GO

/* Allow existing production users to sign in without going through email verification */
UPDATE [dbo].[Users] SET [IsEmailVerified] = 1 WHERE [IsEmailVerified] = 0 AND [IsActive] = 1;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ValidateUser]
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.[MemberId], u.[PasswordHash], u.[IsEmailVerified]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = u.[MemberId]
    WHERE u.[EmailAddress] = @UserName
      AND u.[IsActive] = 1
      AND m.[IsActive] = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpiry]
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [ExpiryDate] FROM [dbo].[Users] WHERE [EmailAddress] = @UserName AND [IsActive] = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_RegisterUser]
    @MemberName NVARCHAR(150),
    @UserName NVARCHAR(50),
    @EmailAddress VARCHAR(255),
    @Mobile VARCHAR(255) = NULL,
    @PasswordHash NVARCHAR(500),
    @CreatedBy NVARCHAR(100),
    @ExpiryDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF EXISTS (
            SELECT 1 FROM [dbo].[Users]
            WHERE EmailAddress = @EmailAddress AND IsActive = 1
        )
        BEGIN
            RAISERROR('Email address already registered.', 16, 1);
            RETURN;
        END

        DECLARE @MemberId BIGINT;

        INSERT INTO [dbo].[Member] (MemberName, CreatedBy, CreatedDate, IsActive)
        VALUES (@MemberName, @CreatedBy, GETDATE(), 1);

        SET @MemberId = SCOPE_IDENTITY();

        INSERT INTO [dbo].[Users]
            (UserName, EmailAddress, Mobile, PasswordHash,
             MemberId, CreatedBy, CreatedDate, IsActive, ExpiryDate,
             IsEmailVerified, EmailVerifyToken, EmailVerifyExpiry, PasswordResetToken, PasswordResetExpiry)
        VALUES
            (@UserName, @EmailAddress, @Mobile, @PasswordHash,
             @MemberId, @CreatedBy, GETDATE(), 1, @ExpiryDate,
             0, NULL, NULL, NULL, NULL);

        COMMIT TRANSACTION;
        SELECT @MemberId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_SaveEmailVerifyToken]
    @Email NVARCHAR(255),
    @Token NVARCHAR(200),
    @ExpiryHours INT = 24
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Users]
    SET [EmailVerifyToken] = @Token,
        [EmailVerifyExpiry] = DATEADD(HOUR, @ExpiryHours, GETUTCDATE())
    WHERE [EmailAddress] = @Email AND [IsActive] = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_VerifyEmail]
    @Token NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MemberId BIGINT;
    DECLARE @Expiry DATETIME;

    SELECT @MemberId = [MemberId], @Expiry = [EmailVerifyExpiry]
    FROM [dbo].[Users]
    WHERE [EmailVerifyToken] = @Token AND [IsActive] = 1;

    IF @MemberId IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS [Success], N'Invalid or expired link' AS [Message];
        RETURN;
    END

    IF @Expiry < GETUTCDATE()
    BEGIN
        SELECT CAST(0 AS BIT) AS [Success], N'Invalid or expired link' AS [Message];
        RETURN;
    END

    UPDATE [dbo].[Users]
    SET [IsEmailVerified] = 1,
        [EmailVerifyToken] = NULL,
        [EmailVerifyExpiry] = NULL
    WHERE [MemberId] = @MemberId AND [EmailVerifyToken] = @Token;

    SELECT CAST(1 AS BIT) AS [Success], N'Email verified successfully' AS [Message];
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_SavePasswordResetToken]
    @Email NVARCHAR(255),
    @ResetToken NVARCHAR(200),
    @ExpiryUTC DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Users]
    SET [PasswordResetToken] = @ResetToken,
        [PasswordResetExpiry] = @ExpiryUTC
    WHERE [EmailAddress] = @Email AND [IsActive] = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ResetPassword]
    @Token NVARCHAR(200),
    @NewPasswordHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Users]
    SET [PasswordHash] = @NewPasswordHash,
        [PasswordResetToken] = NULL,
        [PasswordResetExpiry] = NULL
    WHERE [PasswordResetToken] = @Token
      AND [PasswordResetExpiry] IS NOT NULL
      AND [PasswordResetExpiry] > GETUTCDATE();

    SELECT @@ROWCOUNT AS [RowsUpdated];
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetMemberNameByEmail]
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) m.[MemberName]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = u.[MemberId]
    WHERE u.[EmailAddress] = @Email AND u.[IsActive] = 1 AND m.[IsActive] = 1;
END
GO

PRINT N'Auth enhancements migration completed.';
GO

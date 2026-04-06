-- Aligns notifications storage with FinTrackDB_Schema.sql style:
--   Table dbo.Notifications (singular "Notification" is a reserved keyword)
--   Columns CreatedDate / ModifiedDate / IsActive (same pattern as Member, Expense, BaseEntity)
--
-- Run once on databases that still have dbo.Notification or an older dbo.Notifications shape
-- (e.g. CreatedAt only, no ModifiedDate / IsActive). Safe to re-run: skips steps that already apply.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Notification', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    EXEC sys.sp_rename N'dbo.Notification', N'Notifications', N'OBJECT';
END
GO

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Notifications', N'CreatedAt') IS NOT NULL
       AND COL_LENGTH(N'dbo.Notifications', N'CreatedDate') IS NULL
    BEGIN
        EXEC sys.sp_rename N'dbo.Notifications.CreatedAt', N'CreatedDate', N'COLUMN';
    END

    IF COL_LENGTH(N'dbo.Notifications', N'ModifiedDate') IS NULL
        ALTER TABLE [dbo].[Notifications] ADD [ModifiedDate] [datetime2](7) NULL;

    IF COL_LENGTH(N'dbo.Notifications', N'IsActive') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Notifications] ADD [IsActive] [bit] NOT NULL
            CONSTRAINT [DF_Notifications_IsActive_Align] DEFAULT ((1));
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetNotifications
    @MemberId BIGINT,
    @UnreadOnly BIT = 0,
    @Take INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    IF @Take < 1 OR @Take > 500 SET @Take = 100;

    SELECT TOP (@Take)
        n.NotificationId,
        n.MemberId,
        n.Title,
        n.Body,
        n.NotificationType,
        n.IsRead,
        n.LinkUrl,
        n.CreatedDate,
        n.ModifiedDate
    FROM dbo.Notifications n
    WHERE n.MemberId = @MemberId
      AND n.IsActive = 1
      AND (@UnreadOnly = 0 OR n.IsRead = 0)
    ORDER BY n.CreatedDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_MarkNotificationRead
    @NotificationId BIGINT,
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Notifications
    SET IsRead = 1,
        ModifiedDate = GETUTCDATE()
    WHERE NotificationId = @NotificationId AND MemberId = @MemberId AND IsActive = 1;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_MarkAllNotificationsRead
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Notifications
    SET IsRead = 1,
        ModifiedDate = GETUTCDATE()
    WHERE MemberId = @MemberId AND IsRead = 0 AND IsActive = 1;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateNotification
    @MemberId BIGINT,
    @Title NVARCHAR(200),
    @Body NVARCHAR(2000),
    @NotificationType NVARCHAR(50),
    @LinkUrl NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Notifications (MemberId, Title, Body, NotificationType, LinkUrl)
    VALUES (@MemberId, @Title, @Body, @NotificationType, @LinkUrl);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS NotificationId;
END
GO

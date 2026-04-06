-- =============================================================================
-- FinTrack — Phase 3–5 features: profile, change password, transaction history,
-- notifications, bank details (IBAN), group events.
-- Run against FinTrackDB after prior migrations (Auth, Security Phase 2, Refresh tokens).
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- -----------------------------------------------------------------------------
-- Member: profile photo URL
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.Member', 'ProfilePhotoUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Member ADD ProfilePhotoUrl NVARCHAR(500) NULL;
END
GO

-- -----------------------------------------------------------------------------
-- User profile (Member + Users)
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetUserProfile
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        m.MemberId,
        m.MemberName AS Name,
        u.EmailAddress AS Email,
        u.Mobile AS PhoneNumber,
        m.ProfilePhotoUrl,
        m.CreatedDate AS CreatedAt
    FROM dbo.Member m
    INNER JOIN dbo.Users u ON u.MemberId = m.MemberId
    WHERE m.MemberId = @MemberId
      AND m.IsActive = 1
      AND u.IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateUserProfile
    @MemberId BIGINT,
    @Name NVARCHAR(150),
    @PhoneNumber NVARCHAR(255),
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Member
    SET MemberName = @Name,
        ModifiedDate = GETUTCDATE(),
        ModifiedBy = @ModifiedBy
    WHERE MemberId = @MemberId AND IsActive = 1;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT CAST(0 AS BIT) AS Success;
        RETURN;
    END

    UPDATE dbo.Users
    SET Mobile = @PhoneNumber,
        ModifiedDate = GETUTCDATE(),
        ModifiedBy = @ModifiedBy
    WHERE MemberId = @MemberId AND IsActive = 1;

    SELECT CAST(1 AS BIT) AS Success;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateProfilePhoto
    @MemberId BIGINT,
    @PhotoUrl NVARCHAR(500),
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Member
    SET ProfilePhotoUrl = @PhotoUrl,
        ModifiedDate = GETUTCDATE(),
        ModifiedBy = @ModifiedBy
    WHERE MemberId = @MemberId AND IsActive = 1;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

-- -----------------------------------------------------------------------------
-- Change password (logged-in user)
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetUserAuthByMemberId
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
        u.UserID,
        u.EmailAddress,
        u.PasswordHash
    FROM dbo.Users u
    WHERE u.MemberId = @MemberId AND u.IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateUserPasswordByMemberId
    @MemberId BIGINT,
    @NewPasswordHash NVARCHAR(500),
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET PasswordHash = @NewPasswordHash,
        ModifiedDate = GETUTCDATE(),
        ModifiedBy = @ModifiedBy
    WHERE MemberId = @MemberId AND IsActive = 1;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

-- -----------------------------------------------------------------------------
-- Transaction history (expenses + settlements involving member)
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetMemberTransactionHistory
    @MemberId BIGINT,
    @Skip INT = 0,
    @Take INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    IF @Take < 1 OR @Take > 200 SET @Take = 50;
    IF @Skip < 0 SET @Skip = 0;

    ;WITH Combined AS (
        SELECT
            e.ExpenseId AS RefId,
            N'Expense' AS EntryType,
            ISNULL(e.Description, N'') AS Description,
            e.Amount,
            e.ExpenseDate AS OccurredAt,
            e.GroupId,
            g.GroupName
        FROM dbo.Expense e
        LEFT JOIN dbo.Groups g ON g.GroupId = e.GroupId AND g.IsActive = 1
        WHERE e.IsActive = 1
          AND (
            e.PaidBy = @MemberId
            OR EXISTS (
                SELECT 1 FROM dbo.ExpenseSplit es
                WHERE es.ExpenseId = e.ExpenseId AND es.MemberId = @MemberId AND es.IsActive = 1
            )
          )

        UNION ALL

        SELECT
            CAST(s.SettlementId AS BIGINT) AS RefId,
            N'Settlement' AS EntryType,
            N'Settlement payment' AS Description,
            s.Amount,
            ISNULL(s.SettlementDate, s.CreatedDate) AS OccurredAt,
            s.GroupId,
            g.GroupName
        FROM dbo.Settlement s
        INNER JOIN dbo.Groups g ON g.GroupId = s.GroupId AND g.IsActive = 1
        WHERE s.IsActive = 1
          AND (s.FromMemberId = @MemberId OR s.ToMemberId = @MemberId)
    )
    SELECT RefId, EntryType, Description, Amount, OccurredAt, GroupId, GroupName
    FROM Combined
    ORDER BY OccurredAt DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetMemberTransactionSummary
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ExpenseCount INT = 0;
    DECLARE @SettlementCount INT = 0;

    SELECT @ExpenseCount = COUNT(DISTINCT e.ExpenseId)
    FROM dbo.Expense e
    WHERE e.IsActive = 1
      AND (
        e.PaidBy = @MemberId
        OR EXISTS (
            SELECT 1 FROM dbo.ExpenseSplit es
            WHERE es.ExpenseId = e.ExpenseId AND es.MemberId = @MemberId AND es.IsActive = 1
        )
      );

    SELECT @SettlementCount = COUNT(*)
    FROM dbo.Settlement s
    WHERE s.IsActive = 1
      AND (s.FromMemberId = @MemberId OR s.ToMemberId = @MemberId);

    SELECT @ExpenseCount AS ExpenseEntryCount, @SettlementCount AS SettlementEntryCount,
           (@ExpenseCount + @SettlementCount) AS TotalEntries;
END
GO

-- -----------------------------------------------------------------------------
-- Notifications — dbo.Notifications (singular "Notification" is a reserved keyword).
-- Column style matches FinTrackDB_Schema.sql: CreatedDate / ModifiedDate / IsActive (see BaseEntity).
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications](
        [NotificationId] [bigint] IDENTITY(1,1) NOT NULL,
        [MemberId] [bigint] NOT NULL,
        [Title] [nvarchar](200) NOT NULL,
        [Body] [nvarchar](2000) NULL,
        [NotificationType] [nvarchar](50) NULL,
        [IsRead] [bit] NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT ((0)),
        [LinkUrl] [nvarchar](500) NULL,
        [CreatedDate] [datetime2](7) NOT NULL CONSTRAINT [DF_Notifications_CreatedDate] DEFAULT (GETUTCDATE()),
        [ModifiedDate] [datetime2](7) NULL,
        [IsActive] [bit] NOT NULL CONSTRAINT [DF_Notifications_IsActive] DEFAULT ((1)),
     CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED
    (
        [NotificationId] ASC
    ) ON [PRIMARY]
    ) ON [PRIMARY];

    ALTER TABLE [dbo].[Notifications] WITH CHECK ADD CONSTRAINT [FK_Notifications_Member] FOREIGN KEY([MemberId])
    REFERENCES [dbo].[Member] ([MemberId]);

    CREATE NONCLUSTERED INDEX [IX_Notifications_MemberId] ON [dbo].[Notifications]([MemberId] ASC, [CreatedDate] DESC);
    CREATE NONCLUSTERED INDEX [IX_Notifications_MemberId_IsRead] ON [dbo].[Notifications]([MemberId] ASC, [IsRead] ASC);
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

-- -----------------------------------------------------------------------------
-- Bank details (IBAN) — one row per member
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.MemberBankDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MemberBankDetails (
        BankDetailId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MemberId BIGINT NOT NULL,
        BankName NVARCHAR(100) NOT NULL,
        AccountHolderName NVARCHAR(150) NOT NULL,
        EncryptedIBAN NVARCHAR(500) NOT NULL,
        MaskedIBAN NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MemberBankDetails_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_MemberBankDetails_Member FOREIGN KEY (MemberId) REFERENCES dbo.Member(MemberId),
        CONSTRAINT UQ_MemberBankDetails_MemberId UNIQUE (MemberId)
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveBankDetails
    @MemberId BIGINT,
    @BankName NVARCHAR(100),
    @AccountHolderName NVARCHAR(150),
    @EncryptedIBAN NVARCHAR(500),
    @MaskedIBAN NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.MemberBankDetails WHERE MemberId = @MemberId)
    BEGIN
        UPDATE dbo.MemberBankDetails
        SET BankName = @BankName,
            AccountHolderName = @AccountHolderName,
            EncryptedIBAN = @EncryptedIBAN,
            MaskedIBAN = @MaskedIBAN,
            UpdatedAt = GETUTCDATE()
        WHERE MemberId = @MemberId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.MemberBankDetails (MemberId, BankName, AccountHolderName, EncryptedIBAN, MaskedIBAN)
        VALUES (@MemberId, @BankName, @AccountHolderName, @EncryptedIBAN, @MaskedIBAN);
    END
    SELECT 1 AS Success;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetBankDetails
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        b.BankDetailId,
        b.MemberId,
        b.BankName,
        b.AccountHolderName,
        b.MaskedIBAN,
        b.CreatedAt,
        b.UpdatedAt
    FROM dbo.MemberBankDetails b
    WHERE b.MemberId = @MemberId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteBankDetails
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.MemberBankDetails WHERE MemberId = @MemberId;
    SELECT @@ROWCOUNT AS RowsDeleted;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetMemberBankDetailsForSettlement
    @SettlementId NUMERIC(18,0),
    @RequestingMemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @From BIGINT, @To BIGINT;
    SELECT @From = FromMemberId, @To = ToMemberId
    FROM dbo.Settlement
    WHERE SettlementId = @SettlementId AND IsActive = 1;

    IF @From IS NULL
    BEGIN
        SELECT CAST(NULL AS BIGINT) AS BankDetailId WHERE 1 = 0;
        RETURN;
    END

    IF @RequestingMemberId <> @From
    BEGIN
        SELECT CAST(NULL AS BIGINT) AS BankDetailId WHERE 1 = 0;
        RETURN;
    END

    SELECT
        b.BankDetailId,
        b.MemberId,
        b.BankName,
        b.AccountHolderName,
        b.EncryptedIBAN
    FROM dbo.MemberBankDetails b
    WHERE b.MemberId = @To;
END
GO

-- -----------------------------------------------------------------------------
-- Group events
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.GroupEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GroupEvents (
        EventId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        GroupId BIGINT NOT NULL,
        CreatedByMemberId BIGINT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        EventDate DATETIME2 NOT NULL,
        Location NVARCHAR(300) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_GroupEvents_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME2 NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_GroupEvents_IsDeleted DEFAULT (0),
        CONSTRAINT FK_GroupEvents_Group FOREIGN KEY (GroupId) REFERENCES dbo.Groups(GroupId),
        CONSTRAINT FK_GroupEvents_Member FOREIGN KEY (CreatedByMemberId) REFERENCES dbo.Member(MemberId)
    );
    CREATE NONCLUSTERED INDEX IX_GroupEvents_GroupId ON dbo.GroupEvents(GroupId, EventDate DESC) WHERE IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GroupMemberExists
    @GroupId BIGINT,
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM dbo.GroupMember gm
        WHERE gm.GroupId = @GroupId AND gm.MemberId = @MemberId AND gm.IsActive = 1
    )
        SELECT CAST(1 AS BIT) AS IsMember;
    ELSE
        SELECT CAST(0 AS BIT) AS IsMember;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetGroupEvents
    @GroupId BIGINT,
    @RequestingMemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.GroupMember gm
        WHERE gm.GroupId = @GroupId AND gm.MemberId = @RequestingMemberId AND gm.IsActive = 1
    )
    BEGIN
        SELECT CAST(NULL AS BIGINT) AS EventId WHERE 1 = 0;
        RETURN;
    END

    SELECT
        e.EventId,
        e.GroupId,
        e.CreatedByMemberId,
        e.Title,
        e.Description,
        e.EventDate,
        e.Location,
        e.CreatedAt,
        e.UpdatedAt
    FROM dbo.GroupEvents e
    WHERE e.GroupId = @GroupId AND e.IsDeleted = 0
    ORDER BY e.EventDate ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateGroupEvent
    @GroupId BIGINT,
    @CreatedByMemberId BIGINT,
    @Title NVARCHAR(200),
    @Description NVARCHAR(500),
    @EventDate DATETIME2,
    @Location NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.GroupMember gm
        WHERE gm.GroupId = @GroupId AND gm.MemberId = @CreatedByMemberId AND gm.IsActive = 1
    )
    BEGIN
        SELECT CAST(NULL AS BIGINT) AS EventId;
        RETURN;
    END

    INSERT INTO dbo.GroupEvents (GroupId, CreatedByMemberId, Title, Description, EventDate, Location)
    VALUES (@GroupId, @CreatedByMemberId, @Title, @Description, @EventDate, @Location);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS EventId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateGroupEvent
    @GroupId BIGINT,
    @EventId BIGINT,
    @RequestingMemberId BIGINT,
    @Title NVARCHAR(200),
    @Description NVARCHAR(500),
    @EventDate DATETIME2,
    @Location NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    -- Route includes groupId — must match the event row (prevents cross-group eventId reuse).
    IF NOT EXISTS (
        SELECT 1 FROM dbo.GroupEvents e
        WHERE e.EventId = @EventId
          AND e.GroupId = @GroupId
          AND e.CreatedByMemberId = @RequestingMemberId
          AND e.IsDeleted = 0
    )
    BEGIN
        SELECT CAST(0 AS INT) AS RowsUpdated;
        RETURN;
    END

    UPDATE dbo.GroupEvents
    SET Title = @Title,
        Description = @Description,
        EventDate = @EventDate,
        Location = @Location,
        UpdatedAt = GETUTCDATE()
    WHERE EventId = @EventId AND GroupId = @GroupId;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteGroupEvent
    @GroupId BIGINT,
    @EventId BIGINT,
    @RequestingMemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.GroupEvents e
        WHERE e.EventId = @EventId
          AND e.GroupId = @GroupId
          AND e.CreatedByMemberId = @RequestingMemberId
          AND e.IsDeleted = 0
    )
    BEGIN
        SELECT CAST(0 AS INT) AS RowsUpdated;
        RETURN;
    END

    UPDATE dbo.GroupEvents
    SET IsDeleted = 1, UpdatedAt = GETUTCDATE()
    WHERE EventId = @EventId AND GroupId = @GroupId;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

PRINT N'FinTrackDB_Migration_NewFeatures_Phase3to5.sql completed.';
GO

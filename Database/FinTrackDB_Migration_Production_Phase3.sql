/*
    FinTrack / FinShare — PRODUCTION migration (Phase 3)
    ====================================================
    Apply this script to an EXISTING database. It does NOT drop the database.

    What it does (idempotent where possible):
      • Adds Expense.ExpenseCategory, Expense.ForReference (if missing)
      • Creates dbo.ExpensePayer + FKs + index (if missing)
      • Replaces / creates stored procedures required by the Phase 3 API (BCrypt login,
        personal/office fields, multi-payer, attachment traceability)

    Prerequisites:
      • SQL Server 2016+ (uses CREATE OR ALTER PROCEDURE, DROP PROCEDURE IF EXISTS)
      • Database already has FinTrack tables from a prior release (Expense, ExpenseAttachment,
        Users, Member, ExpenseAccount, etc.)
      • Expense.ExpenseDate column should already exist and be populated (FinTrack schema).
        If any row has NULL ExpenseDate, run the optional backfill block in section 3.

    BEFORE YOU RUN:
      1. Full backup of the production database.
      2. Deploy the new API (BCrypt) immediately after this script — passwords must be BCrypt hashes.
      3. Existing plain-text PasswordHash values will stop working until each user resets password
         or you run a one-time rehash (see section 5 — commented; use only with a controlled process).

    Usage:
      1. In SSMS: change USE [FinTrackDB] to your database name if different.
      2. Execute the entire script once.
      3. Verify: SELECT * FROM sys.procedures WHERE name LIKE 'sp_%' ORDER BY name;

    Pair with: FinTrackDB_Schema.sql (full rebuild for dev/test only — not for production).
*/

SET NOCOUNT ON;
GO

USE [FinTrackDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================
-- 1. Expense — Office / Personal columns (Part E)
-- =============================================

IF COL_LENGTH(N'dbo.Expense', N'ExpenseCategory') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expense]
    ADD [ExpenseCategory] [nvarchar](20) NULL;
END
GO

IF COL_LENGTH(N'dbo.Expense', N'ForReference') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expense]
    ADD [ForReference] [nvarchar](200) NULL;
END
GO

/* Optional: tag existing personal rows (GroupId IS NULL) as Personal for filtering consistency */
UPDATE [dbo].[Expense]
SET [ExpenseCategory] = N'Personal'
WHERE [GroupId] IS NULL
  AND [IsActive] = 1
  AND [ExpenseCategory] IS NULL;
GO

/* Older databases: add ExpenseDate if the column was never created */
IF COL_LENGTH(N'dbo.Expense', N'ExpenseDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expense] ADD [ExpenseDate] [datetime2](7) NULL;
    UPDATE [dbo].[Expense]
    SET [ExpenseDate] = CAST(ISNULL([CreatedDate], GETDATE()) AS DATE)
    WHERE [ExpenseDate] IS NULL;
END
GO

-- =============================================
-- 2. ExpensePayer — Multi-payer (Part D1)
-- =============================================

IF OBJECT_ID(N'[dbo].[ExpensePayer]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ExpensePayer](
        [PayerId] [bigint] IDENTITY(1,1) NOT NULL,
        [ExpenseId] [bigint] NOT NULL,
        [MemberId] [bigint] NOT NULL,
        [AmountPaid] [decimal](10, 2) NOT NULL,
        [CreatedDate] [datetime2](7) NOT NULL CONSTRAINT [DF_ExpensePayer_CreatedDate] DEFAULT (GETDATE()),
        CONSTRAINT [PK_ExpensePayer] PRIMARY KEY CLUSTERED ([PayerId] ASC)
    ) ON [PRIMARY];

    ALTER TABLE [dbo].[ExpensePayer] WITH CHECK
    ADD CONSTRAINT [FK_ExpensePayer_Expense] FOREIGN KEY ([ExpenseId]) REFERENCES [dbo].[Expense]([ExpenseId]);

    ALTER TABLE [dbo].[ExpensePayer] WITH CHECK
    ADD CONSTRAINT [FK_ExpensePayer_Member] FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Member]([MemberId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ExpensePayer_ExpenseId'
      AND object_id = OBJECT_ID(N'[dbo].[ExpensePayer]', N'U')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ExpensePayer_ExpenseId]
    ON [dbo].[ExpensePayer]([ExpenseId] ASC);
END
GO

-- =============================================
-- 3. OPTIONAL — Backfill ExpenseDate (run only if you have NULL ExpenseDate)
-- =============================================
/*
UPDATE [dbo].[Expense]
SET [ExpenseDate] = CAST(ISNULL([CreatedDate], GETDATE()) AS DATE)
WHERE [ExpenseDate] IS NULL;
*/

-- =============================================
-- 4. Stored procedures — align with Phase 3 API
-- =============================================

CREATE OR ALTER PROCEDURE [dbo].[sp_ValidateUser]
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.[MemberId], u.[PasswordHash]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = u.[MemberId]
    WHERE u.[EmailAddress] = @UserName
      AND u.[IsActive] = 1
      AND m.[IsActive] = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_AddExpense]
    @GroupId       BIGINT,
    @Description   NVARCHAR(250),
    @Amount        DECIMAL(18,2),
    @PaidBy        BIGINT,
    @SplitType     NVARCHAR(10) = N'Equal',
    @CreatedBy     NVARCHAR(100),
    @AccountId     BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Expense]
        ([GroupId], [Description], [Amount], [PaidBy], [ExpenseDate], [SplitType], [AccountId], [CreatedBy], [CreatedDate], [IsActive])
    VALUES
        (@GroupId, @Description, @Amount, @PaidBy, CAST(GETDATE() AS DATE), @SplitType, @AccountId, @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateExpense]
    @ExpenseId         BIGINT,
    @Description       NVARCHAR(250) = NULL,
    @Amount            DECIMAL(18,2) = NULL,
    @PaidBy            BIGINT = NULL,
    @SplitType         NVARCHAR(10) = NULL,
    @ModifiedBy        NVARCHAR(100),
    @AccountId         BIGINT = NULL,
    @ExpenseCategory   NVARCHAR(20) = NULL,
    @ForReference      NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Expense]
    SET [Description]     = ISNULL(@Description, [Description]),
        [Amount]          = ISNULL(@Amount, [Amount]),
        [PaidBy]          = ISNULL(@PaidBy, [PaidBy]),
        [SplitType]       = ISNULL(@SplitType, [SplitType]),
        [AccountId]       = ISNULL(@AccountId, [AccountId]),
        [ExpenseCategory] = ISNULL(@ExpenseCategory, [ExpenseCategory]),
        [ForReference]    = ISNULL(@ForReference, [ForReference]),
        [ModifiedBy]      = @ModifiedBy,
        [ModifiedDate]    = GETDATE()
    WHERE [ExpenseId] = @ExpenseId AND [IsActive] = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpensesByGroup]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.[ExpenseId], e.[GroupId], e.[Description], e.[Amount],
        e.[PaidBy], m.[MemberName] AS [PaidByName], e.[SplitType],
        e.[ExpenseDate], e.[ExpenseCategory], e.[ForReference], e.[CreatedDate]
    FROM [dbo].[Expense] e
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = e.[PaidBy]
    WHERE e.[GroupId] = @GroupId AND e.[IsActive] = 1
    ORDER BY e.[CreatedDate] DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_AddPersonalExpense]
    @Description      NVARCHAR(250),
    @Amount           DECIMAL(18,2),
    @MemberId         BIGINT,
    @CreatedBy        NVARCHAR(100),
    @ExpenseDate      DATE = NULL,
    @AccountId        BIGINT = NULL,
    @ExpenseCategory  NVARCHAR(20) = NULL,
    @ForReference     NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Expense]
        ([GroupId], [Description], [Amount], [PaidBy], [ExpenseDate], [SplitType], [AccountId],
         [ExpenseCategory], [ForReference], [CreatedBy], [CreatedDate], [IsActive])
    VALUES
        (NULL, @Description, @Amount, @MemberId,
         ISNULL(@ExpenseDate, CAST(GETDATE() AS DATE)), N'Equal', @AccountId,
         ISNULL(@ExpenseCategory, N'Personal'), @ForReference, @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY() AS [ExpenseId];
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetPersonalExpenses]
    @MemberId BIGINT,
    @Category NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.[ExpenseId],
        e.[GroupId],
        e.[Description],
        e.[Amount],
        e.[PaidBy],
        m.[MemberName] AS [PaidByName],
        e.[SplitType],
        e.[ExpenseDate],
        e.[ExpenseCategory],
        e.[ForReference],
        a.[AccountName],
        e.[CreatedDate]
    FROM [dbo].[Expense] e
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = e.[PaidBy]
    LEFT JOIN [dbo].[ExpenseAccount] a ON e.[AccountId] = a.[AccountId]
    WHERE e.[GroupId] IS NULL AND e.[PaidBy] = @MemberId AND e.[IsActive] = 1
      AND (@Category IS NULL OR e.[ExpenseCategory] = @Category)
    ORDER BY e.[ExpenseDate] DESC, e.[CreatedDate] DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_UpdatePersonalExpense]
    @ExpenseId        BIGINT,
    @MemberId         BIGINT,
    @Description      NVARCHAR(250),
    @Amount           DECIMAL(18,2),
    @ExpenseDate      DATE = NULL,
    @AccountId        BIGINT = NULL,
    @ExpenseCategory  NVARCHAR(20) = NULL,
    @ForReference     NVARCHAR(200) = NULL,
    @ModifiedBy       NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Expense]
    SET [Description]     = @Description,
        [Amount]          = @Amount,
        [ExpenseDate]     = ISNULL(@ExpenseDate, [ExpenseDate]),
        [AccountId]       = ISNULL(@AccountId, [AccountId]),
        [ExpenseCategory] = ISNULL(@ExpenseCategory, [ExpenseCategory]),
        [ForReference]    = ISNULL(@ForReference, [ForReference]),
        [ModifiedBy]      = @ModifiedBy,
        [ModifiedDate]    = GETDATE()
    WHERE [ExpenseId] = @ExpenseId
      AND [GroupId] IS NULL
      AND [PaidBy] = @MemberId
      AND [IsActive] = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_AddExpensePayer]
    @ExpenseId   BIGINT,
    @MemberId    BIGINT,
    @AmountPaid  DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM [dbo].[ExpensePayer]
    WHERE [ExpenseId] = @ExpenseId AND [MemberId] = @MemberId;

    INSERT INTO [dbo].[ExpensePayer] ([ExpenseId], [MemberId], [AmountPaid])
    VALUES (@ExpenseId, @MemberId, @AmountPaid);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS [PayerId];
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpensePayers]
    @ExpenseId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ep.[PayerId], ep.[MemberId], m.[MemberName], ep.[AmountPaid]
    FROM [dbo].[ExpensePayer] ep
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = ep.[MemberId]
    WHERE ep.[ExpenseId] = @ExpenseId
    ORDER BY ep.[AmountPaid] DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpenseAttachments]
    @ExpenseId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ea.[AttachmentId],
        ea.[ExpenseId],
        ea.[FileName],
        ea.[FileUrl],
        ea.[FileType],
        ea.[FileSizeKB],
        ea.[UploadedDate]
    FROM [dbo].[ExpenseAttachment] ea
    WHERE ea.[ExpenseId] = @ExpenseId AND ea.[IsActive] = 1
    ORDER BY ea.[UploadedDate] DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetAttachmentsByMember]
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ea.[AttachmentId],
        ea.[ExpenseId],
        e.[Description] AS [ExpenseDescription],
        e.[Amount] AS [ExpenseAmount],
        e.[ExpenseDate],
        g.[GroupName],
        ea.[FileName],
        ea.[FileUrl],
        ea.[FileType],
        ea.[FileSizeKB],
        ea.[UploadedDate]
    FROM [dbo].[ExpenseAttachment] ea
    INNER JOIN [dbo].[Expense] e ON ea.[ExpenseId] = e.[ExpenseId]
    LEFT JOIN [dbo].[Groups] g ON e.[GroupId] = g.[GroupId]
    WHERE ea.[UploadedBy] = (
            SELECT TOP (1) u.[EmailAddress]
            FROM [dbo].[Users] u
            WHERE u.[MemberId] = @MemberId AND u.[IsActive] = 1
        )
      AND ea.[IsActive] = 1
      AND e.[IsActive] = 1
    ORDER BY ea.[UploadedDate] DESC;
END
GO

-- =============================================
-- 5. OPTIONAL — Force password reset / BCrypt rehash (DO NOT run blindly on production)
-- =============================================
/*
    After deployment, users with non-BCrypt PasswordHash cannot log in.
    Options:
      A) Use your app’s “forgot password” flow once it exists, or
      B) Generate BCrypt hashes offline and UPDATE [Users] SET PasswordHash = N'...' WHERE UserID = ...

    Example (BCrypt for literal password 'TempReset123!' — replace with your own hash):
    -- UPDATE [dbo].[Users] SET [PasswordHash] = N'$2a$12$...' WHERE [EmailAddress] = N'user@example.com';
*/

PRINT N'FinTrack Phase 3 production migration completed.';
GO

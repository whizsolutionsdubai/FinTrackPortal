/*
    FinTrackDB - Database Schema Script
    Generated: 01-Apr-2026

    This script creates the FinTrackDB database, all tables,
    default constraints, foreign keys, and stored procedures.

    Prerequisites:
      - SQL Server 2019+ (compatibility level 160)
      - Run as a user with CREATE DATABASE and sysadmin permissions

    Usage:
      1. Execute this entire script against your SQL Server instance.
      2. Update the connection string in appsettings.json to point to FinTrackDB.
*/

-- =============================================
-- DATABASE
-- =============================================

USE [master]
GO

CREATE DATABASE [FinTrackDB]
GO

ALTER DATABASE [FinTrackDB] SET COMPATIBILITY_LEVEL = 160
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
BEGIN
    EXEC [FinTrackDB].[dbo].[sp_fulltext_database] @action = 'enable'
END
GO

ALTER DATABASE [FinTrackDB] SET RECOVERY FULL
GO
ALTER DATABASE [FinTrackDB] SET MULTI_USER
GO
ALTER DATABASE [FinTrackDB] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [FinTrackDB] SET QUERY_STORE = ON
GO

USE [FinTrackDB]
GO

-- =============================================
-- TABLES
-- =============================================

-- Member (referenced by most other tables)
CREATE TABLE [dbo].[Member](
    [MemberId]     [bigint] IDENTITY(1,1) NOT NULL,
    [MemberName]   [nvarchar](150) NOT NULL,
    [CreatedDate]  [datetime2](7) NULL,
    [ModifiedDate] [datetime2](7) NULL,
    [CreatedBy]    [nvarchar](100) NULL,
    [ModifiedBy]   [nvarchar](100) NULL,
    [IsActive]     [bit] NULL,
    CONSTRAINT [PK__Member__0CF04B18B3D85C83] PRIMARY KEY CLUSTERED ([MemberId] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Member] ADD CONSTRAINT [DF__Member__CreatedD__37A5467C] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Member] ADD CONSTRAINT [DF__Member__IsActive__38996AB5] DEFAULT ((1)) FOR [IsActive]
GO

-- Users
CREATE TABLE [dbo].[Users](
    [UserID]       [bigint] IDENTITY(1,1) NOT NULL,
    [UserName]     [varchar](50) NOT NULL,
    [EmailAddress] [varchar](255) NOT NULL,
    [Mobile]       [varchar](255) NULL,
    [PasswordHash] [nvarchar](500) NULL,
    [MemberId]     [bigint] NOT NULL,
    [CreatedDate]  [datetime2](7) NULL,
    [ModifiedDate] [datetime2](7) NULL,
    [CreatedBy]    [nvarchar](100) NULL,
    [ModifiedBy]   [nvarchar](100) NULL,
    [IsActive]     [bit] NULL,
    [ExpiryDate]   [datetime] NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF_Users_CreatedDate] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF_Users_IsActive] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Users] WITH CHECK ADD CONSTRAINT [FK_Users_Member] FOREIGN KEY([MemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO

-- Groups
CREATE TABLE [dbo].[Groups](
    [GroupId]            [bigint] IDENTITY(1,1) NOT NULL,
    [GroupName]          [nvarchar](150) NOT NULL,
    [CreatedDate]        [datetime2](7) NULL,
    [ModifiedDate]       [datetime2](7) NULL,
    [CreatedBy]          [nvarchar](100) NULL,
    [ModifiedBy]         [nvarchar](100) NULL,
    [IsActive]           [bit] NULL,
    [CreatedByMemberId]  [bigint] NULL,
    [GroupCode]          [nvarchar](10) NULL,
    CONSTRAINT [PK__Group__149AF36A073A1361] PRIMARY KEY CLUSTERED ([GroupId] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Groups] ADD CONSTRAINT [DF__Group__CreatedDa__3B75D760] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Groups] ADD CONSTRAINT [DF__Group__IsActive__3C69FB99] DEFAULT ((1)) FOR [IsActive]
GO

-- GroupMember
CREATE TABLE [dbo].[GroupMember](
    [GroupMemberId]      [bigint] IDENTITY(1,1) NOT NULL,
    [GroupId]            [bigint] NOT NULL,
    [MemberId]           [bigint] NOT NULL,
    [CreatedDate]        [datetime2](7) NULL,
    [ModifiedDate]       [datetime2](7) NULL,
    [CreatedBy]          [nvarchar](100) NULL,
    [ModifiedBy]         [nvarchar](100) NULL,
    [IsActive]           [bit] NULL,
    [CreatedByMemberId]  [numeric](18, 0) NULL,
    [Role]               [nvarchar](20) NULL,
    CONSTRAINT [PK__GroupMem__34481292DD8836D7] PRIMARY KEY CLUSTERED ([GroupMemberId] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[GroupMember] ADD CONSTRAINT [DF__GroupMemb__Creat__3F466844] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[GroupMember] ADD CONSTRAINT [DF__GroupMemb__IsAct__403A8C7D] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[GroupMember] ADD DEFAULT ('Member') FOR [Role]
GO
ALTER TABLE [dbo].[GroupMember] WITH CHECK ADD CONSTRAINT [FK_GroupMember_Group] FOREIGN KEY([GroupId]) REFERENCES [dbo].[Groups] ([GroupId])
GO
ALTER TABLE [dbo].[GroupMember] WITH CHECK ADD CONSTRAINT [FK_GroupMember_Member] FOREIGN KEY([MemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO

-- Expense
CREATE TABLE [dbo].[Expense](
    [ExpenseId]    [bigint] IDENTITY(1,1) NOT NULL,
    [GroupId]      [bigint] NULL,
    [Description]  [nvarchar](2555) NULL,
    [Amount]       [decimal](18, 2) NOT NULL,
    [PaidBy]       [bigint] NOT NULL,
    [ExpenseDate]  [datetime2](7) NOT NULL,
    [CreatedDate]  [datetime2](7) NULL,
    [ModifiedDate] [datetime2](7) NULL,
    [CreatedBy]    [nvarchar](100) NULL,
    [ModifiedBy]   [nvarchar](100) NULL,
    [IsActive]     [bit] NULL,
    [SplitType]    [nvarchar](10) NULL,
    CONSTRAINT [PK__Expense__1445CFD36CB8A42F] PRIMARY KEY CLUSTERED ([ExpenseId] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Expense] ADD CONSTRAINT [DF__Expense__Expense__44FF419A] DEFAULT (GETDATE()) FOR [ExpenseDate]
GO
ALTER TABLE [dbo].[Expense] ADD CONSTRAINT [DF__Expense__Created__45F365D3] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Expense] ADD CONSTRAINT [DF__Expense__IsActiv__46E78A0C] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Expense] ADD DEFAULT ('Equal') FOR [SplitType]
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_Group] FOREIGN KEY([GroupId]) REFERENCES [dbo].[Groups] ([GroupId])
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_Member] FOREIGN KEY([PaidBy]) REFERENCES [dbo].[Member] ([MemberId])
GO

-- ExpenseSplit
CREATE TABLE [dbo].[ExpenseSplit](
    [ExpenseSplitId] [numeric](18, 0) IDENTITY(1,1) NOT NULL,
    [ExpenseId]      [bigint] NOT NULL,
    [MemberId]       [bigint] NOT NULL,
    [ShareAmount]    [decimal](18, 2) NOT NULL,
    [CreatedDate]    [datetime2](7) NULL,
    [ModifiedDate]   [datetime2](7) NULL,
    [CreatedBy]      [nvarchar](100) NULL,
    [ModifiedBy]     [nvarchar](100) NULL,
    [IsActive]       [bit] NULL,
    CONSTRAINT [PK__ExpenseS__E7762EAB5B422C0B] PRIMARY KEY CLUSTERED ([ExpenseSplitId] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ExpenseSplit] ADD CONSTRAINT [DF__ExpenseSp__Creat__4BAC3F29] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[ExpenseSplit] ADD CONSTRAINT [DF__ExpenseSp__IsAct__4CA06362] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[ExpenseSplit] WITH CHECK ADD CONSTRAINT [FK_ExpenseSplit_Expense] FOREIGN KEY([ExpenseId]) REFERENCES [dbo].[Expense] ([ExpenseId])
GO
ALTER TABLE [dbo].[ExpenseSplit] WITH CHECK ADD CONSTRAINT [FK_ExpenseSplit_Member] FOREIGN KEY([MemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO

-- Settlement
CREATE TABLE [dbo].[Settlement](
    [SettlementId]   [numeric](18, 0) IDENTITY(1,1) NOT NULL,
    [GroupId]        [bigint] NOT NULL,
    [FromMemberId]   [bigint] NOT NULL,
    [ToMemberId]     [bigint] NOT NULL,
    [Amount]         [decimal](18, 2) NOT NULL,
    [SettlementDate] [datetime2](7) NULL,
    [CreatedDate]    [datetime2](7) NULL,
    [ModifiedDate]   [datetime2](7) NULL,
    [CreatedBy]      [nvarchar](100) NULL,
    [ModifiedBy]     [nvarchar](100) NULL,
    [IsActive]       [bit] NULL,
    CONSTRAINT [PK__Settleme__7712545A908B103C] PRIMARY KEY CLUSTERED ([SettlementId] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Settlement] ADD CONSTRAINT [DF__Settlemen__Settl__5165187F] DEFAULT (GETDATE()) FOR [SettlementDate]
GO
ALTER TABLE [dbo].[Settlement] ADD CONSTRAINT [DF__Settlemen__Creat__52593CB8] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Settlement] ADD CONSTRAINT [DF__Settlemen__IsAct__534D60F1] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Settlement] WITH CHECK ADD CONSTRAINT [FK_Settlement_FromMember] FOREIGN KEY([FromMemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO
ALTER TABLE [dbo].[Settlement] WITH CHECK ADD CONSTRAINT [FK_Settlement_Group] FOREIGN KEY([GroupId]) REFERENCES [dbo].[Groups] ([GroupId])
GO
ALTER TABLE [dbo].[Settlement] WITH CHECK ADD CONSTRAINT [FK_Settlement_ToMember] FOREIGN KEY([ToMemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO

-- =============================================
-- STORED PROCEDURES
-- =============================================

-- Auth
CREATE OR ALTER PROCEDURE [dbo].[sp_ValidateUser]
    @UserName NVARCHAR(100),
    @PasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.MemberId
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Member] m ON m.MemberId = u.MemberId
    WHERE u.EmailAddress = @UserName
      AND u.PasswordHash = @PasswordHash
      AND u.IsActive = 1
      AND m.IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpiry]
    @UserName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ExpiryDate FROM Users WHERE UserName = @UserName;
END
GO

-- Member
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateMember]
    @MemberName NVARCHAR(150),
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Member] (MemberName, CreatedBy, CreatedDate, IsActive)
    VALUES (@MemberName, @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_EditMember]
    @MemberId BIGINT,
    @MemberName NVARCHAR(150),
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Member]
    SET MemberName = @MemberName,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = GETDATE()
    WHERE MemberId = @MemberId AND IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_DeleteMember]
    @MemberId BIGINT,
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Member]
    SET IsActive = 0,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = GETDATE()
    WHERE MemberId = @MemberId AND IsActive = 1;
END
GO

-- Group
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateGroup]
    @GroupName NVARCHAR(150),
    @GroupCode NVARCHAR(10),
    @CreatedByMemberId BIGINT,
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @GroupId BIGINT;

        INSERT INTO [dbo].[Groups] (GroupName, GroupCode, CreatedByMemberId, CreatedBy, CreatedDate, IsActive)
        VALUES (@GroupName, @GroupCode, @CreatedByMemberId, @CreatedBy, GETDATE(), 1);

        SET @GroupId = SCOPE_IDENTITY();

        INSERT INTO [dbo].[GroupMember] (GroupId, MemberId, Role, CreatedBy, CreatedDate, IsActive)
        VALUES (@GroupId, @CreatedByMemberId, 'Admin', @CreatedBy, GETDATE(), 1);

        COMMIT TRANSACTION;
        SELECT @GroupId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_AddMemberToGroup]
    @GroupId BIGINT,
    @MemberId BIGINT,
    @Role NVARCHAR(20) = 'Member',
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM [dbo].[GroupMember] WHERE GroupId = @GroupId AND MemberId = @MemberId AND IsActive = 1)
    BEGIN
        RAISERROR('Member already exists in this group.', 16, 1);
        RETURN;
    END

    INSERT INTO [dbo].[GroupMember] (GroupId, MemberId, Role, CreatedBy, CreatedDate, IsActive)
    VALUES (@GroupId, @MemberId, @Role, @CreatedBy, GETDATE(), 1);
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetMyGroups]
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        g.GroupId,
        g.GroupName,
        g.GroupCode,
        gm.Role,
        g.CreatedDate
    FROM [dbo].[GroupMember] gm
    INNER JOIN [dbo].[Groups] g ON g.GroupId = gm.GroupId
    WHERE gm.MemberId = @MemberId
      AND gm.IsActive = 1
      AND g.IsActive = 1
    ORDER BY g.CreatedDate DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetGroupMembers]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        m.MemberId,
        m.MemberName,
        gm.Role
    FROM [dbo].[GroupMember] gm
    INNER JOIN [dbo].[Member] m ON m.MemberId = gm.MemberId
    WHERE gm.GroupId = @GroupId
      AND gm.IsActive = 1
      AND m.IsActive = 1
    ORDER BY gm.Role DESC, m.MemberName;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetGroupSummary]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        g.GroupName,
        g.GroupCode,
        m.MemberId,
        m.MemberName AS [Name],
        ISNULL(paid.TotalPaid, 0) AS Paid,
        ISNULL(share.TotalShare, 0) AS Share,
        ISNULL(paid.TotalPaid, 0) - ISNULL(share.TotalShare, 0) AS Net
    FROM [dbo].[GroupMember] gm
    INNER JOIN [dbo].[Member] m ON gm.MemberId = m.MemberId
    INNER JOIN [dbo].[Groups] g ON g.GroupId = gm.GroupId
    LEFT JOIN (
        SELECT PaidBy, SUM(Amount) AS TotalPaid
        FROM [dbo].[Expense]
        WHERE GroupId = @GroupId AND IsActive = 1
        GROUP BY PaidBy
    ) paid ON paid.PaidBy = m.MemberId
    LEFT JOIN (
        SELECT es.MemberId, SUM(es.ShareAmount) AS TotalShare
        FROM [dbo].[ExpenseSplit] es
        INNER JOIN [dbo].[Expense] e ON e.ExpenseId = es.ExpenseId
        WHERE e.GroupId = @GroupId AND e.IsActive = 1
        GROUP BY es.MemberId
    ) share ON share.MemberId = m.MemberId
    WHERE gm.GroupId = @GroupId AND gm.IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_IsMemberOfGroup]
    @GroupId BIGINT,
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM [dbo].[GroupMember]
        WHERE GroupId = @GroupId
          AND MemberId = @MemberId
          AND IsActive = 1
    )
        SELECT 1;
    ELSE
        SELECT 0;
END
GO

-- Expense
CREATE OR ALTER PROCEDURE [dbo].[sp_AddExpense]
    @GroupId BIGINT,
    @Description NVARCHAR(250),
    @Amount DECIMAL(18,2),
    @PaidBy BIGINT,
    @SplitType NVARCHAR(10) = 'Equal',
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Expense] (GroupId, [Description], Amount, PaidBy, SplitType, CreatedBy, CreatedDate, IsActive)
    VALUES (@GroupId, @Description, @Amount, @PaidBy, @SplitType, @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_AddExpenseSplit]
    @ExpenseId BIGINT,
    @MemberId BIGINT,
    @ShareAmount DECIMAL(18,2),
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[ExpenseSplit] (ExpenseId, MemberId, ShareAmount, CreatedBy, CreatedDate, IsActive)
    VALUES (@ExpenseId, @MemberId, @ShareAmount, @CreatedBy, GETDATE(), 1);
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateExpense]
    @ExpenseId BIGINT,
    @Description NVARCHAR(250),
    @Amount DECIMAL(18,2),
    @PaidBy BIGINT,
    @SplitType NVARCHAR(10) = 'Equal',
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Expense]
    SET [Description] = @Description,
        Amount = @Amount,
        PaidBy = @PaidBy,
        SplitType = @SplitType,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = GETDATE()
    WHERE ExpenseId = @ExpenseId AND IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_DeleteExpense]
    @ExpenseId BIGINT,
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[ExpenseSplit]
    SET IsActive = 0
    WHERE ExpenseId = @ExpenseId;

    UPDATE [dbo].[Expense]
    SET IsActive = 0,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = GETDATE()
    WHERE ExpenseId = @ExpenseId AND IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_DeleteExpenseSplits]
    @ExpenseId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM [dbo].[ExpenseSplit]
    WHERE ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpensesByGroup]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.GroupId,
        e.[Description],
        e.Amount,
        e.PaidBy,
        m.MemberName AS PaidByName,
        e.SplitType,
        e.CreatedDate
    FROM [dbo].[Expense] e
    INNER JOIN [dbo].[Member] m ON m.MemberId = e.PaidBy
    WHERE e.GroupId = @GroupId AND e.IsActive = 1
    ORDER BY e.CreatedDate DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_AddPersonalExpense]
    @Description NVARCHAR(250),
    @Amount DECIMAL(18,2),
    @MemberId BIGINT,
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Expense] (GroupId, [Description], Amount, PaidBy, SplitType, CreatedBy, CreatedDate, IsActive)
    VALUES (NULL, @Description, @Amount, @MemberId, 'Equal', @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetPersonalExpenses]
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.GroupId,
        e.[Description],
        e.Amount,
        e.PaidBy,
        m.MemberName AS PaidByName,
        e.SplitType,
        e.CreatedDate
    FROM [dbo].[Expense] e
    INNER JOIN [dbo].[Member] m ON m.MemberId = e.PaidBy
    WHERE e.GroupId IS NULL
      AND e.PaidBy = @MemberId
      AND e.IsActive = 1
    ORDER BY e.CreatedDate DESC;
END
GO

-- Registration
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
             MemberId, CreatedBy, CreatedDate, IsActive, ExpiryDate)
        VALUES
            (@UserName, @EmailAddress, @Mobile, @PasswordHash,
             @MemberId, @CreatedBy, GETDATE(), 1, @ExpiryDate);

        COMMIT TRANSACTION;
        SELECT @MemberId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Settlement
CREATE OR ALTER PROCEDURE [dbo].[sp_RecordSettlement]
    @GroupId BIGINT,
    @FromMemberId BIGINT,
    @ToMemberId BIGINT,
    @Amount DECIMAL(18,2),
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Settlement]
        (GroupId, FromMemberId, ToMemberId, Amount,
         SettlementDate, CreatedBy, CreatedDate, IsActive)
    VALUES
        (@GroupId, @FromMemberId, @ToMemberId, @Amount,
         GETDATE(), @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetSettlementsByGroup]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        s.SettlementId,
        s.GroupId,
        s.FromMemberId,
        mf.MemberName AS FromMemberName,
        s.ToMemberId,
        mt.MemberName AS ToMemberName,
        s.Amount,
        s.SettlementDate,
        s.CreatedDate
    FROM [dbo].[Settlement] s
    INNER JOIN [dbo].[Member] mf ON mf.MemberId = s.FromMemberId
    INNER JOIN [dbo].[Member] mt ON mt.MemberId = s.ToMemberId
    WHERE s.GroupId = @GroupId AND s.IsActive = 1
    ORDER BY s.SettlementDate DESC;
END
GO

USE [master]
GO
ALTER DATABASE [FinTrackDB] SET READ_WRITE
GO

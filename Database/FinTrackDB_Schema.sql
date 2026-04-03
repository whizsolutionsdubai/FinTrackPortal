/*
    FinTrackDB — Complete Database Script (Production Mirror)
    =========================================================
    FinShare Expense Splitting & Settlement Platform
    Version : 2.0
    Date    : 02-Apr-2026
    Authors : Joseph Xavier & Abhilash Thomas

    Exported from production and cleaned for portability.
    Includes: 14 tables, 43 stored procedures, seed + sample data (demo password FinShare1!Demo; re-run script for fresh DB).

    WARNING: This script DROPS and RECREATES the database from scratch.
    All existing data will be lost. Use only for fresh installs or dev/test.

    Prerequisites: SQL Server 2016 or later (uses DROP PROCEDURE IF EXISTS).

    Usage:
      1. SSMS → connect to your instance → New Query → paste ENTIRE file → Execute (F5)
      2. Do not run only the procedure section unless FinTrackDB already exists with tables.
      3. Update the connection string in appsettings.json

    Production (existing DB, no drop): use FinTrackDB_Migration_Production_Phase3.sql instead.
*/

USE [master]
GO

-- Drop existing database if it exists (WARNING: destroys all data)
IF DB_ID('FinTrackDB') IS NOT NULL
BEGIN
    ALTER DATABASE [FinTrackDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [FinTrackDB];
END
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

ALTER DATABASE [FinTrackDB] SET ANSI_NULL_DEFAULT OFF
GO
ALTER DATABASE [FinTrackDB] SET ANSI_NULLS OFF
GO
ALTER DATABASE [FinTrackDB] SET ANSI_PADDING OFF
GO
ALTER DATABASE [FinTrackDB] SET ANSI_WARNINGS OFF
GO
ALTER DATABASE [FinTrackDB] SET ARITHABORT OFF
GO
ALTER DATABASE [FinTrackDB] SET AUTO_CLOSE OFF
GO
ALTER DATABASE [FinTrackDB] SET AUTO_SHRINK OFF
GO
ALTER DATABASE [FinTrackDB] SET AUTO_UPDATE_STATISTICS ON
GO
ALTER DATABASE [FinTrackDB] SET CURSOR_CLOSE_ON_COMMIT OFF
GO
ALTER DATABASE [FinTrackDB] SET CURSOR_DEFAULT GLOBAL
GO
ALTER DATABASE [FinTrackDB] SET CONCAT_NULL_YIELDS_NULL OFF
GO
ALTER DATABASE [FinTrackDB] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [FinTrackDB] SET QUOTED_IDENTIFIER OFF
GO
ALTER DATABASE [FinTrackDB] SET RECURSIVE_TRIGGERS OFF
GO
ALTER DATABASE [FinTrackDB] SET ENABLE_BROKER
GO
ALTER DATABASE [FinTrackDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO
ALTER DATABASE [FinTrackDB] SET DATE_CORRELATION_OPTIMIZATION OFF
GO
ALTER DATABASE [FinTrackDB] SET TRUSTWORTHY OFF
GO
ALTER DATABASE [FinTrackDB] SET ALLOW_SNAPSHOT_ISOLATION OFF
GO
ALTER DATABASE [FinTrackDB] SET PARAMETERIZATION SIMPLE
GO
ALTER DATABASE [FinTrackDB] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [FinTrackDB] SET HONOR_BROKER_PRIORITY OFF
GO
ALTER DATABASE [FinTrackDB] SET RECOVERY SIMPLE
GO
ALTER DATABASE [FinTrackDB] SET MULTI_USER
GO
ALTER DATABASE [FinTrackDB] SET PAGE_VERIFY CHECKSUM
GO
ALTER DATABASE [FinTrackDB] SET DB_CHAINING OFF
GO
ALTER DATABASE [FinTrackDB] SET TARGET_RECOVERY_TIME = 60 SECONDS
GO
ALTER DATABASE [FinTrackDB] SET DELAYED_DURABILITY = DISABLED
GO
ALTER DATABASE [FinTrackDB] SET ACCELERATED_DATABASE_RECOVERY = OFF
GO
ALTER DATABASE [FinTrackDB] SET QUERY_STORE = ON
GO
ALTER DATABASE [FinTrackDB] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO

USE [FinTrackDB]
GO

-- =============================================
-- TABLES
-- =============================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Member](
	[MemberId] [bigint] IDENTITY(1,1) NOT NULL,
	[MemberName] [nvarchar](150) NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK__Member__0CF04B18B3D85C83] PRIMARY KEY CLUSTERED
(
	[MemberId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Users](
	[UserID] [bigint] IDENTITY(1,1) NOT NULL,
	[UserName] [varchar](50) NOT NULL,
	[EmailAddress] [varchar](255) NOT NULL,
	[Mobile] [varchar](255) NULL,
	[PasswordHash] [nvarchar](500) NULL,
	[MemberId] [bigint] NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NULL,
	[ExpiryDate] [datetime] NULL,
	[IsEmailVerified] [bit] NOT NULL DEFAULT ((0)),
	[EmailVerifyToken] [nvarchar](200) NULL,
	[EmailVerifyExpiry] [datetime] NULL,
	[PasswordResetToken] [nvarchar](200) NULL,
	[PasswordResetExpiry] [datetime] NULL,
	[FailedLoginCount] [int] NOT NULL DEFAULT ((0)),
	[LockoutUntil] [datetime] NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED
(
	[UserID] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[AuditLogs_Archive](
	[Id] [bigint] NOT NULL,
	[MemberId] [bigint] NULL,
	[Action] [nvarchar](50) NOT NULL,
	[IPAddress] [nvarchar](50) NULL,
	[UserAgent] [nvarchar](500) NULL,
	[Success] [bit] NOT NULL,
	[Details] [nvarchar](500) NULL,
	[CreatedAt] [datetime] NOT NULL
) ON [PRIMARY]
GO

CREATE CLUSTERED INDEX [IX_AuditLogs_Archive_CreatedAt] ON [dbo].[AuditLogs_Archive]([CreatedAt] ASC)
GO

CREATE TABLE [dbo].[AuditLogs](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[MemberId] [bigint] NULL,
	[Action] [nvarchar](50) NOT NULL,
	[IPAddress] [nvarchar](50) NULL,
	[UserAgent] [nvarchar](500) NULL,
	[Success] [bit] NOT NULL,
	[Details] [nvarchar](500) NULL,
	[CreatedAt] [datetime] NOT NULL DEFAULT (GETUTCDATE()),
 CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogs_MemberId] ON [dbo].[AuditLogs]([MemberId] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogs_CreatedAt] ON [dbo].[AuditLogs]([CreatedAt] ASC)
GO

CREATE TABLE [dbo].[Groups](
	[GroupId] [bigint] IDENTITY(1,1) NOT NULL,
	[GroupName] [nvarchar](150) NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NULL,
	[CreatedByMemberId] [bigint] NULL,
	[GroupCode] [nvarchar](10) NULL,
 CONSTRAINT [PK__Group__149AF36A073A1361] PRIMARY KEY CLUSTERED
(
	[GroupId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[GroupMember](
	[GroupMemberId] [bigint] IDENTITY(1,1) NOT NULL,
	[GroupId] [bigint] NOT NULL,
	[MemberId] [bigint] NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NULL,
	[CreatedByMemberId] [numeric](18, 0) NULL,
	[Role] [nvarchar](20) NULL,
 CONSTRAINT [PK__GroupMem__34481292DD8836D7] PRIMARY KEY CLUSTERED
(
	[GroupMemberId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[SubscriptionPlan](
	[PlanId] [int] IDENTITY(1,1) NOT NULL,
	[PlanName] [nvarchar](50) NOT NULL,
	[MonthlyPrice] [decimal](10, 2) NOT NULL,
	[YearlyPrice] [decimal](10, 2) NOT NULL,
	[MaxGroups] [int] NOT NULL,
	[MaxMembersPerGroup] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
 PRIMARY KEY CLUSTERED
(
	[PlanId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[UserSubscription](
	[SubscriptionId] [bigint] IDENTITY(1,1) NOT NULL,
	[MemberId] [bigint] NOT NULL,
	[PlanId] [int] NOT NULL,
	[BillingCycle] [nvarchar](10) NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[ExpiryDate] [datetime] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[PaymentRef] [nvarchar](200) NULL,
	[CreatedDate] [datetime] NOT NULL,
 PRIMARY KEY CLUSTERED
(
	[SubscriptionId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Organisation](
	[OrgId] [bigint] IDENTITY(1,1) NOT NULL,
	[OrgName] [nvarchar](200) NOT NULL,
	[AdminUserId] [bigint] NOT NULL,
	[OrgCode] [nvarchar](20) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
 PRIMARY KEY CLUSTERED
(
	[OrgId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[CostCenter](
	[CostCenterId] [bigint] IDENTITY(1,1) NOT NULL,
	[OrgId] [bigint] NOT NULL,
	[CostCenterName] [nvarchar](100) NOT NULL,
	[CostCenterCode] [nvarchar](20) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
 PRIMARY KEY CLUSTERED
(
	[CostCenterId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ExpenseAccount](
	[AccountId] [bigint] IDENTITY(1,1) NOT NULL,
	[UserId] [bigint] NOT NULL,
	[AccountName] [nvarchar](100) NOT NULL,
	[AccountColor] [nvarchar](7) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
 PRIMARY KEY CLUSTERED
(
	[AccountId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Expense](
	[ExpenseId] [bigint] IDENTITY(1,1) NOT NULL,
	[GroupId] [bigint] NULL,
	[Description] [nvarchar](2555) NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[PaidBy] [bigint] NOT NULL,
	[ExpenseDate] [datetime2](7) NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NULL,
	[SplitType] [nvarchar](10) NULL,
	[AccountId] [bigint] NULL,
	[CostCenterId] [bigint] NULL,
	[ExpenseCategory] [nvarchar](20) NULL,
	[ForReference] [nvarchar](200) NULL,
 CONSTRAINT [PK__Expense__1445CFD36CB8A42F] PRIMARY KEY CLUSTERED
(
	[ExpenseId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ExpenseSplit](
	[ExpenseSplitId] [numeric](18, 0) IDENTITY(1,1) NOT NULL,
	[ExpenseId] [bigint] NOT NULL,
	[MemberId] [bigint] NOT NULL,
	[ShareAmount] [decimal](18, 2) NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK__ExpenseS__E7762EAB5B422C0B] PRIMARY KEY CLUSTERED
(
	[ExpenseSplitId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Settlement](
	[SettlementId] [numeric](18, 0) IDENTITY(1,1) NOT NULL,
	[GroupId] [bigint] NOT NULL,
	[FromMemberId] [bigint] NOT NULL,
	[ToMemberId] [bigint] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[SettlementDate] [datetime2](7) NULL,
	[CreatedDate] [datetime2](7) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK__Settleme__7712545A908B103C] PRIMARY KEY CLUSTERED
(
	[SettlementId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ExpenseAttachment](
	[AttachmentId] [bigint] IDENTITY(1,1) NOT NULL,
	[ExpenseId] [bigint] NOT NULL,
	[FileName] [nvarchar](255) NOT NULL,
	[FileUrl] [nvarchar](500) NOT NULL,
	[FileType] [nvarchar](10) NOT NULL,
	[FileSizeKB] [int] NULL,
	[UploadedBy] [nvarchar](100) NOT NULL,
	[UploadedDate] [datetime] NOT NULL,
	[IsActive] [bit] NOT NULL,
 PRIMARY KEY CLUSTERED
(
	[AttachmentId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ExpensePayer](
	[PayerId] [bigint] IDENTITY(1,1) NOT NULL,
	[ExpenseId] [bigint] NOT NULL,
	[MemberId] [bigint] NOT NULL,
	[AmountPaid] [decimal](10, 2) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL DEFAULT GETDATE(),
 CONSTRAINT [PK_ExpensePayer] PRIMARY KEY CLUSTERED
(
	[PayerId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ExpensePayer] WITH CHECK ADD CONSTRAINT [FK_ExpensePayer_Expense] FOREIGN KEY([ExpenseId])
REFERENCES [dbo].[Expense] ([ExpenseId])
GO

ALTER TABLE [dbo].[ExpensePayer] WITH CHECK ADD CONSTRAINT [FK_ExpensePayer_Member] FOREIGN KEY([MemberId])
REFERENCES [dbo].[Member] ([MemberId])
GO

CREATE NONCLUSTERED INDEX [IX_ExpensePayer_ExpenseId] ON [dbo].[ExpensePayer]([ExpenseId] ASC)
GO

-- =============================================
-- SAMPLE DATA
-- =============================================

SET IDENTITY_INSERT [dbo].[Member] ON
GO
INSERT [dbo].[Member] ([MemberId], [MemberName], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (1, N'Abhilash Thomas', CAST(N'2026-03-12T00:00:00.0000000' AS DateTime2), CAST(N'2026-03-12T00:00:00.0000000' AS DateTime2), N'admin', NULL, 1)
GO
INSERT [dbo].[Member] ([MemberId], [MemberName], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (2, N'Joseph X', CAST(N'2026-04-01T06:26:20.2000000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[Member] ([MemberId], [MemberName], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (3, N'Salman Khan', CAST(N'2026-04-01T06:26:44.3033333' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[Member] ([MemberId], [MemberName], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (4, N'Thomas Jacob', CAST(N'2026-04-01T12:12:51.9366667' AS DateTime2), NULL, N'thomas@sys.com', NULL, 1)
GO
SET IDENTITY_INSERT [dbo].[Member] OFF
GO

SET IDENTITY_INSERT [dbo].[Users] ON
GO
INSERT [dbo].[Users] ([UserID], [UserName], [EmailAddress], [Mobile], [PasswordHash], [MemberId], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [ExpiryDate], [IsEmailVerified], [EmailVerifyToken], [EmailVerifyExpiry], [PasswordResetToken], [PasswordResetExpiry]) VALUES (1, N'abhilash2006@gmail.com', N'abhilash2006@gmail.com', N'0505743855', N'$2a$12$KD.b74KekP18zgv5eYNbkeZojx4Rrc33dJMvwqH5vVAdiaE4KMn9u', 1, CAST(N'2026-03-12T00:00:00.0000000' AS DateTime2), NULL, N'admin', NULL, 1, CAST(N'2028-12-12T00:00:00.000' AS DateTime), 1, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[Users] ([UserID], [UserName], [EmailAddress], [Mobile], [PasswordHash], [MemberId], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [ExpiryDate], [IsEmailVerified], [EmailVerifyToken], [EmailVerifyExpiry], [PasswordResetToken], [PasswordResetExpiry]) VALUES (2, N'thomas@sys.com', N'thomas@sys.com', N'050785748', N'$2a$12$KD.b74KekP18zgv5eYNbkeZojx4Rrc33dJMvwqH5vVAdiaE4KMn9u', 4, CAST(N'2026-04-01T12:12:51.9400000' AS DateTime2), NULL, N'thomas@sys.com', NULL, 1, CAST(N'2027-04-01T19:12:51.930' AS DateTime), 1, NULL, NULL, NULL, NULL)
GO
SET IDENTITY_INSERT [dbo].[Users] OFF
GO

SET IDENTITY_INSERT [dbo].[Groups] ON
GO
INSERT [dbo].[Groups] ([GroupId], [GroupName], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [CreatedByMemberId], [GroupCode]) VALUES (1, N'Dubai Friends', CAST(N'2026-04-01T06:27:01.3400000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, 1, N'DF-3BB3')
GO
SET IDENTITY_INSERT [dbo].[Groups] OFF
GO

SET IDENTITY_INSERT [dbo].[GroupMember] ON
GO
INSERT [dbo].[GroupMember] ([GroupMemberId], [GroupId], [MemberId], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [CreatedByMemberId], [Role]) VALUES (1, 1, 1, CAST(N'2026-04-01T06:27:01.3400000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, NULL, N'Admin')
GO
INSERT [dbo].[GroupMember] ([GroupMemberId], [GroupId], [MemberId], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [CreatedByMemberId], [Role]) VALUES (2, 1, 2, CAST(N'2026-04-01T06:28:16.6066667' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, NULL, N'Member')
GO
INSERT [dbo].[GroupMember] ([GroupMemberId], [GroupId], [MemberId], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [CreatedByMemberId], [Role]) VALUES (3, 1, 3, CAST(N'2026-04-01T06:28:29.9800000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, NULL, N'Member')
GO
SET IDENTITY_INSERT [dbo].[GroupMember] OFF
GO

SET IDENTITY_INSERT [dbo].[SubscriptionPlan] ON
GO
INSERT [dbo].[SubscriptionPlan] ([PlanId], [PlanName], [MonthlyPrice], [YearlyPrice], [MaxGroups], [MaxMembersPerGroup], [IsActive], [CreatedDate]) VALUES (1, N'Free', CAST(0.00 AS Decimal(10, 2)), CAST(0.00 AS Decimal(10, 2)), -1, -1, 1, CAST(N'2026-04-02T07:12:51.783' AS DateTime))
GO
INSERT [dbo].[SubscriptionPlan] ([PlanId], [PlanName], [MonthlyPrice], [YearlyPrice], [MaxGroups], [MaxMembersPerGroup], [IsActive], [CreatedDate]) VALUES (2, N'Premium', CAST(15.00 AS Decimal(10, 2)), CAST(130.00 AS Decimal(10, 2)), -1, -1, 1, CAST(N'2026-04-02T07:12:51.783' AS DateTime))
GO
SET IDENTITY_INSERT [dbo].[SubscriptionPlan] OFF
GO

SET IDENTITY_INSERT [dbo].[Expense] ON
GO
INSERT [dbo].[Expense] ([ExpenseId], [GroupId], [Description], [Amount], [PaidBy], [ExpenseDate], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [SplitType], [AccountId], [CostCenterId], [ExpenseCategory], [ForReference]) VALUES (1, 1, N'Dinner', CAST(300.00 AS Decimal(18, 2)), 1, CAST(N'2026-04-01T06:29:37.8500000' AS DateTime2), CAST(N'2026-04-01T06:29:37.8500000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, N'Equal', NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[Expense] ([ExpenseId], [GroupId], [Description], [Amount], [PaidBy], [ExpenseDate], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [SplitType], [AccountId], [CostCenterId], [ExpenseCategory], [ForReference]) VALUES (2, 1, N'Lunch', CAST(300.00 AS Decimal(18, 2)), 1, CAST(N'2026-04-01T06:29:54.4033333' AS DateTime2), CAST(N'2026-04-01T06:29:54.4033333' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, N'Equal', NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[Expense] ([ExpenseId], [GroupId], [Description], [Amount], [PaidBy], [ExpenseDate], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [SplitType], [AccountId], [CostCenterId], [ExpenseCategory], [ForReference]) VALUES (3, 1, N'Brakefast', CAST(300.00 AS Decimal(18, 2)), 1, CAST(N'2026-04-01T06:30:07.7366667' AS DateTime2), CAST(N'2026-04-01T06:30:07.7366667' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, N'Equal', NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[Expense] ([ExpenseId], [GroupId], [Description], [Amount], [PaidBy], [ExpenseDate], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [SplitType], [AccountId], [CostCenterId], [ExpenseCategory], [ForReference]) VALUES (4, NULL, N'Coffee', CAST(15.50 AS Decimal(18, 2)), 1, CAST(N'2026-04-01T06:31:04.5966667' AS DateTime2), CAST(N'2026-04-01T06:31:04.5966667' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, N'Equal', NULL, NULL, N'Personal', NULL)
GO
INSERT [dbo].[Expense] ([ExpenseId], [GroupId], [Description], [Amount], [PaidBy], [ExpenseDate], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive], [SplitType], [AccountId], [CostCenterId], [ExpenseCategory], [ForReference]) VALUES (5, NULL, N'Bread', CAST(25.50 AS Decimal(18, 2)), 1, CAST(N'2026-04-01T06:31:52.8400000' AS DateTime2), CAST(N'2026-04-01T06:31:52.8400000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1, N'Equal', NULL, NULL, N'Personal', NULL)
GO
SET IDENTITY_INSERT [dbo].[Expense] OFF
GO

SET IDENTITY_INSERT [dbo].[ExpenseSplit] ON
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(1 AS Numeric(18, 0)), 1, 1, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:29:37.8600000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(2 AS Numeric(18, 0)), 1, 2, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:29:37.8600000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(3 AS Numeric(18, 0)), 1, 3, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:29:37.8600000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(4 AS Numeric(18, 0)), 2, 1, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:29:54.4100000' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(5 AS Numeric(18, 0)), 2, 2, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:29:54.4133333' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(6 AS Numeric(18, 0)), 2, 3, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:29:54.4166667' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(7 AS Numeric(18, 0)), 3, 1, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:30:07.7366667' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(8 AS Numeric(18, 0)), 3, 2, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:30:07.7366667' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
INSERT [dbo].[ExpenseSplit] ([ExpenseSplitId], [ExpenseId], [MemberId], [ShareAmount], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy], [IsActive]) VALUES (CAST(9 AS Numeric(18, 0)), 3, 3, CAST(100.00 AS Decimal(18, 2)), CAST(N'2026-04-01T06:30:07.7366667' AS DateTime2), NULL, N'abhilash2006@gmail.com', NULL, 1)
GO
SET IDENTITY_INSERT [dbo].[ExpenseSplit] OFF
GO

-- =============================================
-- INDEXES & UNIQUE CONSTRAINTS
-- =============================================

SET ANSI_PADDING ON
GO
ALTER TABLE [dbo].[Organisation] ADD UNIQUE NONCLUSTERED ([OrgCode] ASC) ON [PRIMARY]
GO

-- =============================================
-- DEFAULT CONSTRAINTS
-- =============================================

ALTER TABLE [dbo].[Member] ADD CONSTRAINT [DF__Member__CreatedD__37A5467C] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Member] ADD CONSTRAINT [DF__Member__IsActive__38996AB5] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF_Users_CreatedDate] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF_Users_IsActive] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Groups] ADD CONSTRAINT [DF__Group__CreatedDa__3B75D760] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Groups] ADD CONSTRAINT [DF__Group__IsActive__3C69FB99] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[GroupMember] ADD CONSTRAINT [DF__GroupMemb__Creat__3F466844] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[GroupMember] ADD CONSTRAINT [DF__GroupMemb__IsAct__403A8C7D] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[GroupMember] ADD DEFAULT ('Member') FOR [Role]
GO
ALTER TABLE [dbo].[Expense] ADD CONSTRAINT [DF__Expense__Expense__44FF419A] DEFAULT (GETDATE()) FOR [ExpenseDate]
GO
ALTER TABLE [dbo].[Expense] ADD CONSTRAINT [DF__Expense__Created__45F365D3] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Expense] ADD CONSTRAINT [DF__Expense__IsActiv__46E78A0C] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Expense] ADD DEFAULT ('Equal') FOR [SplitType]
GO
ALTER TABLE [dbo].[ExpenseSplit] ADD CONSTRAINT [DF__ExpenseSp__Creat__4BAC3F29] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[ExpenseSplit] ADD CONSTRAINT [DF__ExpenseSp__IsAct__4CA06362] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Settlement] ADD CONSTRAINT [DF__Settlemen__Settl__5165187F] DEFAULT (GETDATE()) FOR [SettlementDate]
GO
ALTER TABLE [dbo].[Settlement] ADD CONSTRAINT [DF__Settlemen__Creat__52593CB8] DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Settlement] ADD CONSTRAINT [DF__Settlemen__IsAct__534D60F1] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[SubscriptionPlan] ADD DEFAULT ((0)) FOR [MonthlyPrice]
GO
ALTER TABLE [dbo].[SubscriptionPlan] ADD DEFAULT ((0)) FOR [YearlyPrice]
GO
ALTER TABLE [dbo].[SubscriptionPlan] ADD DEFAULT ((3)) FOR [MaxGroups]
GO
ALTER TABLE [dbo].[SubscriptionPlan] ADD DEFAULT ((5)) FOR [MaxMembersPerGroup]
GO
ALTER TABLE [dbo].[SubscriptionPlan] ADD DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[SubscriptionPlan] ADD DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[UserSubscription] ADD DEFAULT ('monthly') FOR [BillingCycle]
GO
ALTER TABLE [dbo].[UserSubscription] ADD DEFAULT (GETDATE()) FOR [StartDate]
GO
ALTER TABLE [dbo].[UserSubscription] ADD DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[UserSubscription] ADD DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[CostCenter] ADD DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[CostCenter] ADD DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[ExpenseAccount] ADD DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[ExpenseAccount] ADD DEFAULT (GETDATE()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[ExpenseAttachment] ADD DEFAULT (GETDATE()) FOR [UploadedDate]
GO
ALTER TABLE [dbo].[ExpenseAttachment] ADD DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Organisation] ADD DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Organisation] ADD DEFAULT (GETDATE()) FOR [CreatedDate]
GO

-- =============================================
-- FOREIGN KEY CONSTRAINTS
-- =============================================

ALTER TABLE [dbo].[Users] WITH CHECK ADD CONSTRAINT [FK_Users_Member] FOREIGN KEY([MemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Member]
GO
ALTER TABLE [dbo].[GroupMember] WITH CHECK ADD CONSTRAINT [FK_GroupMember_Group] FOREIGN KEY([GroupId]) REFERENCES [dbo].[Groups] ([GroupId])
GO
ALTER TABLE [dbo].[GroupMember] CHECK CONSTRAINT [FK_GroupMember_Group]
GO
ALTER TABLE [dbo].[GroupMember] WITH CHECK ADD CONSTRAINT [FK_GroupMember_Member] FOREIGN KEY([MemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO
ALTER TABLE [dbo].[GroupMember] CHECK CONSTRAINT [FK_GroupMember_Member]
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_Group] FOREIGN KEY([GroupId]) REFERENCES [dbo].[Groups] ([GroupId])
GO
ALTER TABLE [dbo].[Expense] CHECK CONSTRAINT [FK_Expense_Group]
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_Member] FOREIGN KEY([PaidBy]) REFERENCES [dbo].[Member] ([MemberId])
GO
ALTER TABLE [dbo].[Expense] CHECK CONSTRAINT [FK_Expense_Member]
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_Account] FOREIGN KEY([AccountId]) REFERENCES [dbo].[ExpenseAccount] ([AccountId])
GO
ALTER TABLE [dbo].[Expense] CHECK CONSTRAINT [FK_Expense_Account]
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_CostCenter] FOREIGN KEY([CostCenterId]) REFERENCES [dbo].[CostCenter] ([CostCenterId])
GO
ALTER TABLE [dbo].[Expense] CHECK CONSTRAINT [FK_Expense_CostCenter]
GO
ALTER TABLE [dbo].[ExpenseSplit] WITH CHECK ADD CONSTRAINT [FK_ExpenseSplit_Expense] FOREIGN KEY([ExpenseId]) REFERENCES [dbo].[Expense] ([ExpenseId])
GO
ALTER TABLE [dbo].[ExpenseSplit] CHECK CONSTRAINT [FK_ExpenseSplit_Expense]
GO
ALTER TABLE [dbo].[ExpenseSplit] WITH CHECK ADD CONSTRAINT [FK_ExpenseSplit_Member] FOREIGN KEY([MemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO
ALTER TABLE [dbo].[ExpenseSplit] CHECK CONSTRAINT [FK_ExpenseSplit_Member]
GO
ALTER TABLE [dbo].[Settlement] WITH CHECK ADD CONSTRAINT [FK_Settlement_Group] FOREIGN KEY([GroupId]) REFERENCES [dbo].[Groups] ([GroupId])
GO
ALTER TABLE [dbo].[Settlement] CHECK CONSTRAINT [FK_Settlement_Group]
GO
ALTER TABLE [dbo].[Settlement] WITH CHECK ADD CONSTRAINT [FK_Settlement_FromMember] FOREIGN KEY([FromMemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO
ALTER TABLE [dbo].[Settlement] CHECK CONSTRAINT [FK_Settlement_FromMember]
GO
ALTER TABLE [dbo].[Settlement] WITH CHECK ADD CONSTRAINT [FK_Settlement_ToMember] FOREIGN KEY([ToMemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO
ALTER TABLE [dbo].[Settlement] CHECK CONSTRAINT [FK_Settlement_ToMember]
GO
ALTER TABLE [dbo].[ExpenseAttachment] WITH CHECK ADD FOREIGN KEY([ExpenseId]) REFERENCES [dbo].[Expense] ([ExpenseId])
GO
ALTER TABLE [dbo].[CostCenter] WITH CHECK ADD FOREIGN KEY([OrgId]) REFERENCES [dbo].[Organisation] ([OrgId])
GO
ALTER TABLE [dbo].[UserSubscription] WITH CHECK ADD FOREIGN KEY([MemberId]) REFERENCES [dbo].[Member] ([MemberId])
GO
ALTER TABLE [dbo].[UserSubscription] WITH CHECK ADD FOREIGN KEY([PlanId]) REFERENCES [dbo].[SubscriptionPlan] ([PlanId])
GO

-- =============================================
-- STORED PROCEDURES  (34 total)
-- =============================================
-- Use plain CREATE (not CREATE OR ALTER): some SSMS / engine builds mis-handle
-- CREATE OR ALTER and raise Msg 208 naming the procedure itself.
USE [FinTrackDB]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* Explicit drops (no dynamic SQL) — avoids tool/parser edge cases */
DROP PROCEDURE IF EXISTS [dbo].[sp_ArchiveAuditLogsRetention];
DROP PROCEDURE IF EXISTS [dbo].[sp_AddExpense];
DROP PROCEDURE IF EXISTS [dbo].[sp_AddExpenseAttachment];
DROP PROCEDURE IF EXISTS [dbo].[sp_AddExpensePayer];
DROP PROCEDURE IF EXISTS [dbo].[sp_AddExpenseSplit];
DROP PROCEDURE IF EXISTS [dbo].[sp_AddMemberToGroup];
DROP PROCEDURE IF EXISTS [dbo].[sp_AddPersonalExpense];
DROP PROCEDURE IF EXISTS [dbo].[sp_CleanupExpiredTokens];
DROP PROCEDURE IF EXISTS [dbo].[sp_ClearFailedLogins];
DROP PROCEDURE IF EXISTS [dbo].[sp_CancelUserSubscription];
DROP PROCEDURE IF EXISTS [dbo].[sp_CheckUserLimit];
DROP PROCEDURE IF EXISTS [dbo].[sp_CreateAccount];
DROP PROCEDURE IF EXISTS [dbo].[sp_CreateGroup];
DROP PROCEDURE IF EXISTS [dbo].[sp_CreateMember];
DROP PROCEDURE IF EXISTS [dbo].[sp_CreateUserSubscription];
DROP PROCEDURE IF EXISTS [dbo].[sp_DeleteAccount];
DROP PROCEDURE IF EXISTS [dbo].[sp_DeleteExpense];
DROP PROCEDURE IF EXISTS [dbo].[sp_DeleteExpenseAttachment];
DROP PROCEDURE IF EXISTS [dbo].[sp_DeleteExpenseSplits];
DROP PROCEDURE IF EXISTS [dbo].[sp_DeleteMember];
DROP PROCEDURE IF EXISTS [dbo].[sp_EditMember];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetUserEmailVerificationStatus];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetExpiry];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetMemberNameByEmail];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetAttachmentsByMember];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetExpenseAttachments];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetExpensePayers];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetExpensesByGroup];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetGroupMembers];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetGroupSummary];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetMyGroups];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetPersonalExpenses];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetSettlementsByGroup];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetSubscriptionPlans];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetUserAccounts];
DROP PROCEDURE IF EXISTS [dbo].[sp_GetUserSubscription];
DROP PROCEDURE IF EXISTS [dbo].[sp_IsMemberOfGroup];
DROP PROCEDURE IF EXISTS [dbo].[sp_MoveExpense];
DROP PROCEDURE IF EXISTS [dbo].[sp_RecordSettlement];
DROP PROCEDURE IF EXISTS [dbo].[sp_RecordFailedLogin];
DROP PROCEDURE IF EXISTS [dbo].[sp_RegisterUser];
DROP PROCEDURE IF EXISTS [dbo].[sp_ResetLoginAttempts];
DROP PROCEDURE IF EXISTS [dbo].[sp_ResetPassword];
DROP PROCEDURE IF EXISTS [dbo].[sp_SaveEmailVerifyToken];
DROP PROCEDURE IF EXISTS [dbo].[sp_SavePasswordResetToken];
DROP PROCEDURE IF EXISTS [dbo].[sp_UpdateExpense];
DROP PROCEDURE IF EXISTS [dbo].[sp_UpdatePersonalExpense];
DROP PROCEDURE IF EXISTS [dbo].[sp_ValidateUser];
DROP PROCEDURE IF EXISTS [dbo].[sp_VerifyEmail];
DROP PROCEDURE IF EXISTS [dbo].[sp_WriteAuditLog];
GO

-- Auth -----------------------------------------------

CREATE PROCEDURE [dbo].[sp_ValidateUser]
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.[MemberId], u.[PasswordHash], u.[IsEmailVerified], u.[LockoutUntil], u.[FailedLoginCount]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = u.[MemberId]
    WHERE u.[EmailAddress] = @UserName
      AND u.[IsActive] = 1
      AND m.[IsActive] = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_GetExpiry]
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [ExpiryDate] FROM [dbo].[Users] WHERE [EmailAddress] = @UserName AND [IsActive] = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_SaveEmailVerifyToken]
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

CREATE PROCEDURE [dbo].[sp_VerifyEmail]
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
        SELECT CAST(0 AS BIT) AS [Success], N'Invalid or expired link' AS [Message], CAST(NULL AS BIGINT) AS [MemberId];
        RETURN;
    END

    IF @Expiry < GETUTCDATE()
    BEGIN
        SELECT CAST(0 AS BIT) AS [Success], N'Invalid or expired link' AS [Message], CAST(NULL AS BIGINT) AS [MemberId];
        RETURN;
    END

    UPDATE [dbo].[Users]
    SET [IsEmailVerified] = 1,
        [EmailVerifyToken] = NULL,
        [EmailVerifyExpiry] = NULL
    WHERE [MemberId] = @MemberId AND [EmailVerifyToken] = @Token;

    SELECT CAST(1 AS BIT) AS [Success], N'Email verified successfully' AS [Message], @MemberId AS [MemberId];
END
GO

CREATE PROCEDURE [dbo].[sp_SavePasswordResetToken]
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

CREATE PROCEDURE [dbo].[sp_ResetPassword]
    @Token NVARCHAR(200),
    @NewPasswordHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MemberId BIGINT;

    SELECT @MemberId = [MemberId]
    FROM [dbo].[Users]
    WHERE [PasswordResetToken] = @Token
      AND [PasswordResetExpiry] IS NOT NULL
      AND [PasswordResetExpiry] > GETUTCDATE()
      AND [IsActive] = 1;

    IF @MemberId IS NULL
    BEGIN
        SELECT 0 AS [RowsUpdated], CAST(NULL AS BIGINT) AS [MemberId];
        RETURN;
    END

    UPDATE [dbo].[Users]
    SET [PasswordHash] = @NewPasswordHash,
        [PasswordResetToken] = NULL,
        [PasswordResetExpiry] = NULL,
        [FailedLoginCount] = 0,
        [LockoutUntil] = NULL
    WHERE [MemberId] = @MemberId AND [PasswordResetToken] = @Token;

    SELECT @@ROWCOUNT AS [RowsUpdated], @MemberId AS [MemberId];
END
GO

CREATE PROCEDURE [dbo].[sp_GetMemberNameByEmail]
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) m.[MemberName], u.[MemberId]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = u.[MemberId]
    WHERE u.[EmailAddress] = @Email AND u.[IsActive] = 1 AND m.[IsActive] = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_RecordFailedLogin]
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Users]
    SET [FailedLoginCount] = [FailedLoginCount] + 1,
        [LockoutUntil] = CASE
            WHEN [FailedLoginCount] + 1 >= 5 THEN DATEADD(MINUTE, 15, GETUTCDATE())
            ELSE [LockoutUntil]
        END
    WHERE [EmailAddress] = @UserName AND [IsActive] = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_ClearFailedLogins]
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Users]
    SET [FailedLoginCount] = 0,
        [LockoutUntil] = NULL
    WHERE [EmailAddress] = @UserName AND [IsActive] = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_ResetLoginAttempts]
    @UserName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    EXEC [dbo].[sp_ClearFailedLogins] @UserName = @UserName;
END
GO

CREATE PROCEDURE [dbo].[sp_CleanupExpiredTokens]
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Users]
    SET [EmailVerifyToken] = NULL,
        [EmailVerifyExpiry] = NULL
    WHERE [EmailVerifyExpiry] < GETUTCDATE()
      AND [IsEmailVerified] = 0
      AND [IsActive] = 1;

    UPDATE [dbo].[Users]
    SET [PasswordResetToken] = NULL,
        [PasswordResetExpiry] = NULL
    WHERE [PasswordResetExpiry] < GETUTCDATE()
      AND [IsActive] = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_WriteAuditLog]
    @MemberId BIGINT = NULL,
    @Action NVARCHAR(50),
    @IPAddress NVARCHAR(50) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @Success BIT,
    @Details NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[AuditLogs] ([MemberId], [Action], [IPAddress], [UserAgent], [Success], [Details])
    VALUES (@MemberId, @Action, @IPAddress, @UserAgent, @Success, @Details);
END
GO

CREATE PROCEDURE [dbo].[sp_ArchiveAuditLogsRetention]
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[AuditLogs_Archive] ([Id], [MemberId], [Action], [IPAddress], [UserAgent], [Success], [Details], [CreatedAt])
    SELECT [Id], [MemberId], [Action], [IPAddress], [UserAgent], [Success], [Details], [CreatedAt]
    FROM [dbo].[AuditLogs]
    WHERE [CreatedAt] < DATEADD(DAY, -90, GETUTCDATE());

    DELETE FROM [dbo].[AuditLogs]
    WHERE [CreatedAt] < DATEADD(DAY, -90, GETUTCDATE());

    DELETE FROM [dbo].[AuditLogs_Archive]
    WHERE [CreatedAt] < DATEADD(YEAR, -1, GETUTCDATE());
END
GO

CREATE PROCEDURE [dbo].[sp_GetUserEmailVerificationStatus]
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) u.[MemberId], m.[MemberName], u.[IsEmailVerified]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = u.[MemberId]
    WHERE u.[EmailAddress] = @Email AND u.[IsActive] = 1 AND m.[IsActive] = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_RegisterUser]
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

-- Member -----------------------------------------------

CREATE PROCEDURE [dbo].[sp_CreateMember]
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

CREATE PROCEDURE [dbo].[sp_EditMember]
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

CREATE PROCEDURE [dbo].[sp_DeleteMember]
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

-- Group -----------------------------------------------

CREATE PROCEDURE [dbo].[sp_CreateGroup]
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

CREATE PROCEDURE [dbo].[sp_AddMemberToGroup]
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

CREATE PROCEDURE [dbo].[sp_GetMyGroups]
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT g.GroupId, g.GroupName, g.GroupCode, gm.Role, g.CreatedDate
    FROM [dbo].[GroupMember] gm
    INNER JOIN [dbo].[Groups] g ON g.GroupId = gm.GroupId
    WHERE gm.MemberId = @MemberId
      AND gm.IsActive = 1
      AND g.IsActive = 1
    ORDER BY g.CreatedDate DESC;
END
GO

CREATE PROCEDURE [dbo].[sp_GetGroupMembers]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.MemberId, m.MemberName, gm.Role
    FROM [dbo].[GroupMember] gm
    INNER JOIN [dbo].[Member] m ON m.MemberId = gm.MemberId
    WHERE gm.GroupId = @GroupId
      AND gm.IsActive = 1
      AND m.IsActive = 1
    ORDER BY gm.Role DESC, m.MemberName;
END
GO

CREATE PROCEDURE [dbo].[sp_GetGroupSummary]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        g.GroupName, g.GroupCode, m.MemberId,
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

CREATE PROCEDURE [dbo].[sp_IsMemberOfGroup]
    @GroupId BIGINT,
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM [dbo].[GroupMember]
        WHERE GroupId = @GroupId AND MemberId = @MemberId AND IsActive = 1
    )
        SELECT 1;
    ELSE
        SELECT 0;
END
GO

-- Expense -----------------------------------------------

CREATE PROCEDURE [dbo].[sp_AddExpense]
    @GroupId    BIGINT,
    @Description NVARCHAR(250),
    @Amount     DECIMAL(18,2),
    @PaidBy     BIGINT,
    @SplitType  NVARCHAR(10) = 'Equal',
    @CreatedBy  NVARCHAR(100),
    @AccountId  BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Expense] (GroupId, [Description], Amount, PaidBy, ExpenseDate, SplitType, AccountId, CreatedBy, CreatedDate, IsActive)
    VALUES (@GroupId, @Description, @Amount, @PaidBy, CAST(GETDATE() AS DATE), @SplitType, @AccountId, @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE PROCEDURE [dbo].[sp_AddExpenseSplit]
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

CREATE PROCEDURE [dbo].[sp_UpdateExpense]
    @ExpenseId   BIGINT,
    @Description NVARCHAR(250) = NULL,
    @Amount      DECIMAL(18,2) = NULL,
    @PaidBy      BIGINT = NULL,
    @SplitType   NVARCHAR(10) = NULL,
    @ModifiedBy  NVARCHAR(100),
    @AccountId   BIGINT = NULL,
    @ExpenseCategory NVARCHAR(20) = NULL,
    @ForReference NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Expense]
    SET [Description] = ISNULL(@Description, [Description]),
        Amount        = ISNULL(@Amount, Amount),
        PaidBy        = ISNULL(@PaidBy, PaidBy),
        SplitType     = ISNULL(@SplitType, SplitType),
        AccountId     = ISNULL(@AccountId, AccountId),
        ExpenseCategory = ISNULL(@ExpenseCategory, ExpenseCategory),
        ForReference  = ISNULL(@ForReference, ForReference),
        ModifiedBy    = @ModifiedBy,
        ModifiedDate  = GETDATE()
    WHERE ExpenseId = @ExpenseId AND IsActive = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_DeleteExpense]
    @ExpenseId BIGINT,
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[ExpenseSplit] SET IsActive = 0 WHERE ExpenseId = @ExpenseId;
    UPDATE [dbo].[Expense]
    SET IsActive = 0, ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
    WHERE ExpenseId = @ExpenseId AND IsActive = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_DeleteExpenseSplits]
    @ExpenseId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM [dbo].[ExpenseSplit] WHERE ExpenseId = @ExpenseId;
END
GO

CREATE PROCEDURE [dbo].[sp_GetExpensesByGroup]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.ExpenseId, e.GroupId, e.[Description], e.Amount,
           e.PaidBy, m.MemberName AS PaidByName, e.SplitType,
           e.ExpenseDate, e.ExpenseCategory, e.ForReference, e.CreatedDate
    FROM [dbo].[Expense] e
    INNER JOIN [dbo].[Member] m ON m.MemberId = e.PaidBy
    WHERE e.GroupId = @GroupId AND e.IsActive = 1
    ORDER BY e.CreatedDate DESC;
END
GO

CREATE PROCEDURE [dbo].[sp_AddPersonalExpense]
    @Description NVARCHAR(250),
    @Amount DECIMAL(18,2),
    @MemberId BIGINT,
    @CreatedBy NVARCHAR(100),
    @ExpenseDate DATE = NULL,
    @AccountId BIGINT = NULL,
    @ExpenseCategory NVARCHAR(20) = NULL,
    @ForReference NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Expense]
        (GroupId, [Description], Amount, PaidBy, ExpenseDate, SplitType, AccountId,
         ExpenseCategory, ForReference, CreatedBy, CreatedDate, IsActive)
    VALUES
        (NULL, @Description, @Amount, @MemberId,
         ISNULL(@ExpenseDate, CAST(GETDATE() AS DATE)), 'Equal', @AccountId,
         ISNULL(@ExpenseCategory, N'Personal'), @ForReference, @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

CREATE PROCEDURE [dbo].[sp_GetPersonalExpenses]
    @MemberId BIGINT,
    @Category NVARCHAR(20) = NULL
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
        e.ExpenseDate,
        e.ExpenseCategory,
        e.ForReference,
        a.AccountName,
        e.CreatedDate
    FROM [dbo].[Expense] e
    INNER JOIN [dbo].[Member] m ON m.MemberId = e.PaidBy
    LEFT JOIN [dbo].[ExpenseAccount] a ON e.AccountId = a.AccountId
    WHERE e.GroupId IS NULL AND e.PaidBy = @MemberId AND e.IsActive = 1
      AND (@Category IS NULL OR e.ExpenseCategory = @Category)
    ORDER BY e.ExpenseDate DESC, e.CreatedDate DESC;
END
GO

CREATE PROCEDURE [dbo].[sp_UpdatePersonalExpense]
    @ExpenseId BIGINT,
    @MemberId BIGINT,
    @Description NVARCHAR(250),
    @Amount DECIMAL(18,2),
    @ExpenseDate DATE = NULL,
    @AccountId BIGINT = NULL,
    @ExpenseCategory NVARCHAR(20) = NULL,
    @ForReference NVARCHAR(200) = NULL,
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Expense]
    SET [Description] = @Description,
        Amount = @Amount,
        ExpenseDate = ISNULL(@ExpenseDate, ExpenseDate),
        AccountId = ISNULL(@AccountId, AccountId),
        ExpenseCategory = ISNULL(@ExpenseCategory, ExpenseCategory),
        ForReference = ISNULL(@ForReference, ForReference),
        ModifiedBy = @ModifiedBy,
        ModifiedDate = GETDATE()
    WHERE ExpenseId = @ExpenseId
      AND GroupId IS NULL
      AND PaidBy = @MemberId
      AND IsActive = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_MoveExpense]
    @ExpenseId  BIGINT,
    @NewGroupId BIGINT,
    @ModifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Expense]
    SET GroupId = @NewGroupId, ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE()
    WHERE ExpenseId = @ExpenseId AND IsActive = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_AddExpensePayer]
    @ExpenseId BIGINT,
    @MemberId BIGINT,
    @AmountPaid DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM [dbo].[ExpensePayer]
    WHERE ExpenseId = @ExpenseId AND MemberId = @MemberId;

    INSERT INTO [dbo].[ExpensePayer] (ExpenseId, MemberId, AmountPaid)
    VALUES (@ExpenseId, @MemberId, @AmountPaid);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS PayerId;
END
GO

CREATE PROCEDURE [dbo].[sp_GetExpensePayers]
    @ExpenseId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ep.PayerId, ep.MemberId, m.MemberName, ep.AmountPaid
    FROM [dbo].[ExpensePayer] ep
    INNER JOIN [dbo].[Member] m ON m.MemberId = ep.MemberId
    WHERE ep.ExpenseId = @ExpenseId
    ORDER BY ep.AmountPaid DESC;
END
GO

-- Settlement -----------------------------------------------

CREATE PROCEDURE [dbo].[sp_RecordSettlement]
    @GroupId BIGINT,
    @FromMemberId BIGINT,
    @ToMemberId BIGINT,
    @Amount DECIMAL(18,2),
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Settlement]
        (GroupId, FromMemberId, ToMemberId, Amount, SettlementDate, CreatedBy, CreatedDate, IsActive)
    VALUES
        (@GroupId, @FromMemberId, @ToMemberId, @Amount, GETDATE(), @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE PROCEDURE [dbo].[sp_GetSettlementsByGroup]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.SettlementId, s.GroupId,
           s.FromMemberId, mf.MemberName AS FromMemberName,
           s.ToMemberId, mt.MemberName AS ToMemberName,
           s.Amount, s.SettlementDate, s.CreatedDate
    FROM [dbo].[Settlement] s
    INNER JOIN [dbo].[Member] mf ON mf.MemberId = s.FromMemberId
    INNER JOIN [dbo].[Member] mt ON mt.MemberId = s.ToMemberId
    WHERE s.GroupId = @GroupId AND s.IsActive = 1
    ORDER BY s.SettlementDate DESC;
END
GO

-- Subscription -----------------------------------------------

CREATE PROCEDURE [dbo].[sp_GetSubscriptionPlans]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PlanId, PlanName, MonthlyPrice, YearlyPrice, MaxGroups, MaxMembersPerGroup
    FROM [dbo].[SubscriptionPlan]
    WHERE IsActive = 1
    ORDER BY MonthlyPrice ASC;
END
GO

CREATE PROCEDURE [dbo].[sp_GetUserSubscription]
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1
        us.SubscriptionId, sp.PlanName, sp.MaxGroups, sp.MaxMembersPerGroup,
        us.BillingCycle, us.StartDate, us.ExpiryDate, us.IsActive
    FROM [dbo].[UserSubscription] us
    INNER JOIN [dbo].[SubscriptionPlan] sp ON us.PlanId = sp.PlanId
    WHERE us.MemberId = @MemberId AND us.IsActive = 1 AND us.ExpiryDate >= GETDATE()
    ORDER BY us.ExpiryDate DESC;
END
GO

CREATE PROCEDURE [dbo].[sp_CreateUserSubscription]
    @MemberId     BIGINT,
    @PlanId       INT,
    @BillingCycle NVARCHAR(10),
    @PaymentRef   NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[UserSubscription] SET IsActive = 0 WHERE MemberId = @MemberId AND IsActive = 1;

    DECLARE @ExpiryDate DATETIME;
    IF @BillingCycle = 'yearly'
        SET @ExpiryDate = DATEADD(YEAR, 1, GETDATE());
    ELSE
        SET @ExpiryDate = DATEADD(MONTH, 1, GETDATE());

    INSERT INTO [dbo].[UserSubscription]
        (MemberId, PlanId, BillingCycle, StartDate, ExpiryDate, IsActive, PaymentRef)
    VALUES
        (@MemberId, @PlanId, @BillingCycle, GETDATE(), @ExpiryDate, 1, @PaymentRef);
END
GO

CREATE PROCEDURE [dbo].[sp_CancelUserSubscription]
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[UserSubscription] SET IsActive = 0 WHERE MemberId = @MemberId AND IsActive = 1;
END
GO

CREATE PROCEDURE [dbo].[sp_CheckUserLimit]
    @MemberId  BIGINT,
    @CheckType NVARCHAR(20),
    @GroupId   BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MaxGroups INT, @MaxMembers INT;

    SELECT TOP 1 @MaxGroups = sp.MaxGroups, @MaxMembers = sp.MaxMembersPerGroup
    FROM [dbo].[UserSubscription] us
    INNER JOIN [dbo].[SubscriptionPlan] sp ON us.PlanId = sp.PlanId
    WHERE us.MemberId = @MemberId AND us.IsActive = 1 AND us.ExpiryDate >= GETDATE();

    IF @MaxGroups IS NULL
    BEGIN
        SELECT TOP 1 @MaxGroups = MaxGroups, @MaxMembers = MaxMembersPerGroup
        FROM [dbo].[SubscriptionPlan]
        WHERE PlanName = 'Free' AND IsActive = 1;
    END

    IF (@CheckType = 'group' AND @MaxGroups = -1) OR (@CheckType = 'member' AND @MaxMembers = -1)
    BEGIN
        SELECT 1 AS IsAllowed; RETURN;
    END

    IF @CheckType = 'group'
    BEGIN
        DECLARE @GroupCount INT;
        SELECT @GroupCount = COUNT(*) FROM [dbo].[Groups] WHERE CreatedByMemberId = @MemberId AND IsActive = 1;
        SELECT CASE WHEN @GroupCount < @MaxGroups THEN 1 ELSE 0 END AS IsAllowed;
        RETURN;
    END

    IF @CheckType = 'member' AND @GroupId IS NOT NULL
    BEGIN
        DECLARE @MemberCount INT;
        SELECT @MemberCount = COUNT(*) FROM [dbo].[GroupMember] WHERE GroupId = @GroupId AND IsActive = 1;
        SELECT CASE WHEN @MemberCount < @MaxMembers THEN 1 ELSE 0 END AS IsAllowed;
        RETURN;
    END

    SELECT 1 AS IsAllowed;
END
GO

-- Account Tags -----------------------------------------------

CREATE PROCEDURE [dbo].[sp_GetUserAccounts]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AccountId, AccountName, AccountColor
    FROM [dbo].[ExpenseAccount]
    WHERE UserId = @UserId AND IsActive = 1
    ORDER BY AccountName ASC;
END
GO

CREATE PROCEDURE [dbo].[sp_CreateAccount]
    @UserId       BIGINT,
    @AccountName  NVARCHAR(100),
    @AccountColor NVARCHAR(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[ExpenseAccount] (UserId, AccountName, AccountColor)
    VALUES (@UserId, @AccountName, @AccountColor);
    SELECT SCOPE_IDENTITY() AS AccountId;
END
GO

CREATE PROCEDURE [dbo].[sp_DeleteAccount]
    @AccountId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[ExpenseAccount] SET IsActive = 0 WHERE AccountId = @AccountId;
END
GO

-- Attachments -----------------------------------------------

CREATE PROCEDURE [dbo].[sp_AddExpenseAttachment]
    @ExpenseId  BIGINT,
    @FileName   NVARCHAR(255),
    @FileUrl    NVARCHAR(500),
    @FileType   NVARCHAR(10),
    @FileSizeKB INT = NULL,
    @UploadedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[ExpenseAttachment]
        (ExpenseId, FileName, FileUrl, FileType, FileSizeKB, UploadedBy)
    VALUES
        (@ExpenseId, @FileName, @FileUrl, @FileType, @FileSizeKB, @UploadedBy);
    SELECT SCOPE_IDENTITY() AS AttachmentId;
END
GO

CREATE PROCEDURE [dbo].[sp_GetExpenseAttachments]
    @ExpenseId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AttachmentId, ExpenseId, FileName, FileUrl, FileType, FileSizeKB, UploadedDate
    FROM [dbo].[ExpenseAttachment]
    WHERE ExpenseId = @ExpenseId AND IsActive = 1
    ORDER BY UploadedDate DESC;
END
GO

CREATE PROCEDURE [dbo].[sp_GetAttachmentsByMember]
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ea.AttachmentId,
        ea.ExpenseId,
        e.[Description] AS ExpenseDescription,
        e.Amount AS ExpenseAmount,
        e.ExpenseDate,
        g.GroupName,
        ea.FileName,
        ea.FileUrl,
        ea.FileType,
        ea.FileSizeKB,
        ea.UploadedDate
    FROM [dbo].[ExpenseAttachment] ea
    INNER JOIN [dbo].[Expense] e ON ea.ExpenseId = e.ExpenseId
    LEFT JOIN [dbo].[Groups] g ON e.GroupId = g.GroupId
    WHERE ea.UploadedBy = (
            SELECT TOP 1 u.EmailAddress
            FROM [dbo].[Users] u
            WHERE u.MemberId = @MemberId AND u.IsActive = 1
        )
      AND ea.IsActive = 1
      AND e.IsActive = 1
    ORDER BY ea.UploadedDate DESC;
END
GO

CREATE PROCEDURE [dbo].[sp_DeleteExpenseAttachment]
    @AttachmentId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[ExpenseAttachment] SET IsActive = 0 WHERE AttachmentId = @AttachmentId;
END
GO

-- =============================================
-- DONE
-- =============================================

USE [master]
GO
ALTER DATABASE [FinTrackDB] SET READ_WRITE
GO

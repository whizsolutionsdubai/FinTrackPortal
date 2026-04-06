/*
  FinShare backend v6 follow-up.
  Applies additive fixes from FinShare_ForAbhilash_BackendChanges.pdf / CompleteActions.pdf.

  Includes:
    1) Expense.CurrencyCode column + backfill.
    2) CurrencyCode updates across expense stored procedures.
    3) Notifications mark-all-read stored procedure.
*/

SET NOCOUNT ON;
GO

-- 1) Expense CurrencyCode
IF COL_LENGTH(N'dbo.Expense', N'CurrencyCode') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expense]
    ADD [CurrencyCode] [nvarchar](3) NULL
        CONSTRAINT [DF_Expense_CurrencyCode_v6] DEFAULT (N'AED');
END
GO

UPDATE [dbo].[Expense]
SET [CurrencyCode] = N'AED'
WHERE [CurrencyCode] IS NULL;
GO

ALTER TABLE [dbo].[Expense]
ALTER COLUMN [CurrencyCode] [nvarchar](3) NOT NULL;
GO

-- 2) Expense procedures with CurrencyCode
CREATE OR ALTER PROCEDURE [dbo].[sp_AddExpense]
    @GroupId       BIGINT,
    @Description   NVARCHAR(250),
    @Amount        DECIMAL(18,2),
    @CurrencyCode  NVARCHAR(3) = N'AED',
    @PaidBy        BIGINT,
    @SplitType     NVARCHAR(10) = N'Equal',
    @CreatedBy     NVARCHAR(100),
    @AccountId     BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Expense]
        ([GroupId], [Description], [Amount], [CurrencyCode], [PaidBy], [ExpenseDate], [SplitType], [AccountId], [CreatedBy], [CreatedDate], [IsActive])
    VALUES
        (@GroupId, @Description, @Amount, ISNULL(@CurrencyCode, N'AED'), @PaidBy, CAST(GETDATE() AS DATE), @SplitType, @AccountId, @CreatedBy, GETDATE(), 1);
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateExpense]
    @ExpenseId         BIGINT,
    @Description       NVARCHAR(250) = NULL,
    @Amount            DECIMAL(18,2) = NULL,
    @CurrencyCode      NVARCHAR(3) = NULL,
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
        [CurrencyCode]    = ISNULL(@CurrencyCode, [CurrencyCode]),
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
        e.[ExpenseId], e.[GroupId], e.[Description], e.[Amount], e.[CurrencyCode],
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
    @CurrencyCode     NVARCHAR(3) = N'AED',
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
        ([GroupId], [Description], [Amount], [CurrencyCode], [PaidBy], [ExpenseDate], [SplitType], [AccountId],
         [ExpenseCategory], [ForReference], [CreatedBy], [CreatedDate], [IsActive])
    VALUES
        (NULL, @Description, @Amount, ISNULL(@CurrencyCode, N'AED'), @MemberId,
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
        e.[CurrencyCode],
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
    @CurrencyCode     NVARCHAR(3) = NULL,
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
        [CurrencyCode]    = ISNULL(@CurrencyCode, [CurrencyCode]),
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

-- 3) Notifications mark-all-read
CREATE OR ALTER PROCEDURE [dbo].[sp_MarkAllNotificationsRead]
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Notifications]
    SET [IsRead] = 1,
        [ModifiedDate] = GETUTCDATE()
    WHERE [MemberId] = @MemberId
      AND [IsRead] = 0
      AND [IsActive] = 1;

    SELECT @@ROWCOUNT AS [RowsUpdated];
END
GO

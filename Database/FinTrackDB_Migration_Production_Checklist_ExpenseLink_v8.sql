/*
  FinShare checklist/event expense link + multi-currency reference amount.
  Implements Sections 9 and 10 from FinShare_Checklist_Complete.pdf.
*/

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.Expense', N'EventId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expense] ADD [EventId] [bigint] NULL;
    ALTER TABLE [dbo].[Expense] WITH CHECK
        ADD CONSTRAINT [FK_Expense_GroupEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [dbo].[GroupEvents]([EventId]);
END
GO

IF COL_LENGTH(N'dbo.Expense', N'ChecklistItemId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expense] ADD [ChecklistItemId] [bigint] NULL;
    ALTER TABLE [dbo].[Expense] WITH CHECK
        ADD CONSTRAINT [FK_Expense_EventChecklistItems_ChecklistItemId] FOREIGN KEY ([ChecklistItemId]) REFERENCES [dbo].[EventChecklistItems]([ChecklistItemId]);
END
GO

IF COL_LENGTH(N'dbo.Expense', N'AmountOriginal') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expense] ADD [AmountOriginal] [decimal](18,2) NULL;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_AddExpense]
    @GroupId BIGINT,
    @Description NVARCHAR(250),
    @Amount DECIMAL(18,2),
    @CurrencyCode NVARCHAR(3) = N'AED',
    @AmountOriginal DECIMAL(18,2) = NULL,
    @PaidBy BIGINT,
    @SplitType NVARCHAR(10) = N'Equal',
    @CreatedBy NVARCHAR(100),
    @AccountId BIGINT = NULL,
    @EventId BIGINT = NULL,
    @ChecklistItemId BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ChecklistItemId IS NOT NULL AND @EventId IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.EventChecklistItems
            WHERE ChecklistItemId = @ChecklistItemId
              AND EventId = @EventId
        )
        BEGIN
            SELECT CAST(-1 AS BIGINT) AS Result;
            RETURN;
        END
    END

    IF (@CurrencyCode IS NOT NULL AND @CurrencyCode <> N'' AND UPPER(@CurrencyCode) <> N'AED') AND @AmountOriginal IS NULL
    BEGIN
        SELECT CAST(-1 AS BIGINT) AS Result;
        RETURN;
    END

    IF @AmountOriginal IS NOT NULL AND (@CurrencyCode IS NULL OR @CurrencyCode = N'')
    BEGIN
        SELECT CAST(-1 AS BIGINT) AS Result;
        RETURN;
    END

    IF @AmountOriginal IS NOT NULL AND @AmountOriginal <= 0
    BEGIN
        SELECT CAST(-1 AS BIGINT) AS Result;
        RETURN;
    END

    INSERT INTO [dbo].[Expense]
        ([GroupId], [Description], [Amount], [CurrencyCode], [AmountOriginal], [PaidBy], [ExpenseDate], [SplitType], [AccountId], [EventId], [ChecklistItemId], [CreatedBy], [CreatedDate], [IsActive])
    VALUES
        (@GroupId, @Description, @Amount, ISNULL(@CurrencyCode, N'AED'), @AmountOriginal, @PaidBy, CAST(GETDATE() AS DATE), @SplitType, @AccountId, @EventId, @ChecklistItemId, @CreatedBy, GETDATE(), 1);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpensesByGroup]
    @GroupId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.[ExpenseId], e.[GroupId], e.[Description], e.[Amount], e.[CurrencyCode], e.[AmountOriginal],
        e.[PaidBy], m.[MemberName] AS [PaidByName], e.[SplitType],
        e.[ExpenseDate], e.[ExpenseCategory], e.[ForReference], e.[EventId], e.[ChecklistItemId], ci.[ItemName] AS [ChecklistItemName], e.[CreatedDate]
    FROM [dbo].[Expense] e
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = e.[PaidBy]
    LEFT JOIN [dbo].[EventChecklistItems] ci ON ci.[ChecklistItemId] = e.[ChecklistItemId]
    WHERE e.[GroupId] = @GroupId AND e.[IsActive] = 1
    ORDER BY e.[CreatedDate] DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetEventExpenses]
    @GroupId BIGINT,
    @EventId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.[ExpenseId],
        e.[Description],
        e.[Amount],
        e.[AmountOriginal],
        e.[CurrencyCode],
        e.[PaidBy] AS [PaidBy],
        m.[MemberName] AS [PaidByName],
        e.[ChecklistItemId],
        ci.[ItemName] AS [ChecklistItemName],
        e.[CreatedDate]
    FROM [dbo].[Expense] e
    INNER JOIN [dbo].[Member] m ON m.[MemberId] = e.[PaidBy]
    LEFT JOIN [dbo].[EventChecklistItems] ci ON ci.[ChecklistItemId] = e.[ChecklistItemId]
    WHERE e.[GroupId] = @GroupId
      AND e.[EventId] = @EventId
      AND e.[IsActive] = 1
    ORDER BY e.[CreatedDate] DESC;
END
GO

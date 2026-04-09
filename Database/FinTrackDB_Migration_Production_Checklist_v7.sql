/*
  FinShare checklist module (volunteer model + advanced mode).
  Based on FinShare_Checklist_Complete.pdf.
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.EventChecklistItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventChecklistItems (
        ChecklistItemId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EventId BIGINT NOT NULL,
        ItemName NVARCHAR(300) NOT NULL,
        QuantityNeeded INT NOT NULL CONSTRAINT DF_EventChecklistItems_QuantityNeeded DEFAULT (1),
        QuantityUnit NVARCHAR(50) NULL,
        SuggestedMemberId BIGINT NULL,
        IsMandatory BIT NOT NULL CONSTRAINT DF_EventChecklistItems_IsMandatory DEFAULT (0),
        AssignedMemberId BIGINT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_EventChecklistItems_Status DEFAULT (N'Open'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EventChecklistItems_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_EventChecklistItems_Event FOREIGN KEY (EventId) REFERENCES dbo.GroupEvents(EventId),
        CONSTRAINT FK_EventChecklistItems_SuggestedMember FOREIGN KEY (SuggestedMemberId) REFERENCES dbo.Member(MemberId),
        CONSTRAINT FK_EventChecklistItems_AssignedMember FOREIGN KEY (AssignedMemberId) REFERENCES dbo.Member(MemberId)
    );
END
GO

IF OBJECT_ID(N'dbo.EventChecklistClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventChecklistClaims
    (
        ClaimId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ChecklistItemId BIGINT NOT NULL,
        MemberId BIGINT NOT NULL,
        QuantityClaimed INT NOT NULL CONSTRAINT DF_EventChecklistClaims_QuantityClaimed DEFAULT (1),
        ClaimedDate DATETIME2 NOT NULL CONSTRAINT DF_EventChecklistClaims_ClaimedDate DEFAULT (GETUTCDATE()),
        IsWithdrawn BIT NOT NULL CONSTRAINT DF_EventChecklistClaims_IsWithdrawn DEFAULT (0),
        WithdrawnDate DATETIME2 NULL,
        CONSTRAINT UQ_Claim_Item_Member UNIQUE (ChecklistItemId, MemberId),
        CONSTRAINT FK_EventChecklistClaims_Item FOREIGN KEY (ChecklistItemId) REFERENCES dbo.EventChecklistItems(ChecklistItemId) ON DELETE CASCADE,
        CONSTRAINT FK_EventChecklistClaims_Member FOREIGN KEY (MemberId) REFERENCES dbo.Member(MemberId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_EventChecklistClaims_ItemId_Withdrawn'
      AND object_id = OBJECT_ID(N'dbo.EventChecklistClaims', N'U')
)
BEGIN
    CREATE INDEX IX_EventChecklistClaims_ItemId_Withdrawn
    ON dbo.EventChecklistClaims (ChecklistItemId, IsWithdrawn)
    INCLUDE (MemberId, QuantityClaimed);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddChecklistItem
    @EventId BIGINT,
    @ItemName NVARCHAR(300),
    @QuantityNeeded INT = 1,
    @QuantityUnit NVARCHAR(50) = NULL,
    @SuggestedMemberId BIGINT = NULL,
    @IsMandatory BIT = 0,
    @AssignedMemberId BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.EventChecklistItems
    (EventId, ItemName, QuantityNeeded, QuantityUnit, SuggestedMemberId, IsMandatory, AssignedMemberId, Status)
    VALUES
    (@EventId, @ItemName, @QuantityNeeded, @QuantityUnit, @SuggestedMemberId, @IsMandatory, @AssignedMemberId, N'Open');

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS ChecklistItemId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateChecklistItem
    @ChecklistItemId BIGINT,
    @ItemName NVARCHAR(300) = NULL,
    @QuantityNeeded INT = NULL,
    @QuantityUnit NVARCHAR(50) = NULL,
    @SuggestedMemberId BIGINT = NULL,
    @IsMandatory BIT = NULL,
    @AssignedMemberId BIGINT = NULL,
    @Status NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status = N'Completed'
    BEGIN
        DECLARE @CurrentStatus NVARCHAR(30);
        SELECT @CurrentStatus = Status FROM dbo.EventChecklistItems WHERE ChecklistItemId = @ChecklistItemId;
        IF @CurrentStatus <> N'Fully Taken'
        BEGIN
            SELECT -1 AS Result, N'Item must be Fully Taken before marking as Completed' AS Message;
            RETURN;
        END
    END

    UPDATE dbo.EventChecklistItems
    SET ItemName = ISNULL(@ItemName, ItemName),
        QuantityNeeded = ISNULL(@QuantityNeeded, QuantityNeeded),
        QuantityUnit = ISNULL(@QuantityUnit, QuantityUnit),
        SuggestedMemberId = ISNULL(@SuggestedMemberId, SuggestedMemberId),
        IsMandatory = ISNULL(@IsMandatory, IsMandatory),
        AssignedMemberId = ISNULL(@AssignedMemberId, AssignedMemberId),
        Status = ISNULL(@Status, Status),
        UpdatedAt = GETUTCDATE()
    WHERE ChecklistItemId = @ChecklistItemId;

    SELECT 1 AS Result, N'Updated successfully' AS Message;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteChecklistItem
    @ChecklistItemId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.EventChecklistItems WHERE ChecklistItemId = @ChecklistItemId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetEventChecklist
    @EventId BIGINT,
    @RequestingMemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ci.ChecklistItemId,
        ci.EventId,
        ci.ItemName,
        ci.QuantityNeeded,
        ci.QuantityUnit,
        ci.SuggestedMemberId,
        sm.MemberName AS SuggestedMemberName,
        ci.IsMandatory,
        ci.AssignedMemberId,
        am.MemberName AS AssignedMemberName,
        ci.Status,
        ci.CreatedAt,
        ISNULL(SUM(CASE WHEN cl.IsWithdrawn = 0 THEN cl.QuantityClaimed END), 0) AS TotalQuantityClaimed,
        ci.QuantityNeeded - ISNULL(SUM(CASE WHEN cl.IsWithdrawn = 0 THEN cl.QuantityClaimed END), 0) AS QuantityRemaining,
        MAX(CASE WHEN cl.MemberId = @RequestingMemberId AND cl.IsWithdrawn = 0 THEN 1 ELSE 0 END) AS IsClaimedByMe,
        ISNULL(MAX(CASE WHEN cl.MemberId = @RequestingMemberId AND cl.IsWithdrawn = 0 THEN cl.QuantityClaimed END), 0) AS MyClaimedQuantity
    FROM dbo.EventChecklistItems ci
    LEFT JOIN dbo.Member sm ON sm.MemberId = ci.SuggestedMemberId
    LEFT JOIN dbo.Member am ON am.MemberId = ci.AssignedMemberId
    LEFT JOIN dbo.EventChecklistClaims cl ON cl.ChecklistItemId = ci.ChecklistItemId
    WHERE ci.EventId = @EventId
    GROUP BY
        ci.ChecklistItemId, ci.EventId, ci.ItemName, ci.QuantityNeeded,
        ci.QuantityUnit, ci.SuggestedMemberId, sm.MemberName, ci.IsMandatory,
        ci.AssignedMemberId, am.MemberName, ci.Status, ci.CreatedAt
    ORDER BY ci.CreatedAt;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetEventChecklistClaims
    @EventId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.ClaimId,
        c.ChecklistItemId,
        c.MemberId,
        m.MemberName AS MemberName,
        c.QuantityClaimed,
        c.ClaimedDate
    FROM dbo.EventChecklistClaims c
    JOIN dbo.Member m ON m.MemberId = c.MemberId
    JOIN dbo.EventChecklistItems ci ON ci.ChecklistItemId = c.ChecklistItemId
    WHERE ci.EventId = @EventId
      AND c.IsWithdrawn = 0
    ORDER BY c.ChecklistItemId, c.ClaimedDate;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ClaimChecklistItem
    @ChecklistItemId BIGINT,
    @MemberId BIGINT,
    @QuantityClaimed INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @QuantityNeeded INT, @TotalClaimed INT;

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;
    BEGIN TRY
        SELECT @QuantityNeeded = QuantityNeeded
        FROM dbo.EventChecklistItems WITH (UPDLOCK, ROWLOCK)
        WHERE ChecklistItemId = @ChecklistItemId;

        SELECT @TotalClaimed = ISNULL(SUM(QuantityClaimed), 0)
        FROM dbo.EventChecklistClaims
        WHERE ChecklistItemId = @ChecklistItemId AND IsWithdrawn = 0;

        IF (@TotalClaimed + @QuantityClaimed) > @QuantityNeeded
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT -1 AS Result, N'Quantity exceeds available' AS Message;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM dbo.EventChecklistClaims WHERE ChecklistItemId = @ChecklistItemId AND MemberId = @MemberId)
        BEGIN
            UPDATE dbo.EventChecklistClaims
            SET IsWithdrawn = 0,
                WithdrawnDate = NULL,
                QuantityClaimed = @QuantityClaimed,
                ClaimedDate = GETUTCDATE()
            WHERE ChecklistItemId = @ChecklistItemId AND MemberId = @MemberId;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.EventChecklistClaims (ChecklistItemId, MemberId, QuantityClaimed)
            VALUES (@ChecklistItemId, @MemberId, @QuantityClaimed);
        END

        DECLARE @NewTotal INT;
        SELECT @NewTotal = ISNULL(SUM(QuantityClaimed), 0)
        FROM dbo.EventChecklistClaims
        WHERE ChecklistItemId = @ChecklistItemId AND IsWithdrawn = 0;

        UPDATE dbo.EventChecklistItems
        SET Status = CASE
                WHEN @NewTotal = 0 THEN N'Open'
                WHEN @NewTotal >= @QuantityNeeded THEN N'Fully Taken'
                ELSE N'Partially Taken'
            END,
            UpdatedAt = GETUTCDATE()
        WHERE ChecklistItemId = @ChecklistItemId;

        COMMIT TRANSACTION;
        SELECT 1 AS Result, N'Claimed successfully' AS Message;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT -1 AS Result, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WithdrawChecklistClaim
    @ChecklistItemId BIGINT,
    @MemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.EventChecklistClaims
    SET IsWithdrawn = 1,
        WithdrawnDate = GETUTCDATE()
    WHERE ChecklistItemId = @ChecklistItemId
      AND MemberId = @MemberId
      AND IsWithdrawn = 0;

    DECLARE @QuantityNeeded INT, @NewTotal INT;
    SELECT @QuantityNeeded = QuantityNeeded FROM dbo.EventChecklistItems WHERE ChecklistItemId = @ChecklistItemId;
    SELECT @NewTotal = ISNULL(SUM(QuantityClaimed), 0)
    FROM dbo.EventChecklistClaims
    WHERE ChecklistItemId = @ChecklistItemId AND IsWithdrawn = 0;

    UPDATE dbo.EventChecklistItems
    SET Status = CASE
            WHEN @NewTotal = 0 THEN N'Open'
            WHEN @NewTotal >= @QuantityNeeded THEN N'Fully Taken'
            ELSE N'Partially Taken'
        END,
        UpdatedAt = GETUTCDATE()
    WHERE ChecklistItemId = @ChecklistItemId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_MarkChecklistItemCompleted
    @GroupId BIGINT,
    @EventId BIGINT,
    @ChecklistItemId BIGINT,
    @RequestingMemberId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.GroupEvents ge
        WHERE ge.EventId = @EventId
          AND ge.GroupId = @GroupId
          AND ge.CreatedByMemberId = @RequestingMemberId
          AND ge.IsDeleted = 0
    )
    BEGIN
        SELECT -1 AS Result, N'Only the event organizer can mark checklist items as completed.' AS Message;
        RETURN;
    END

    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = ci.Status
    FROM dbo.EventChecklistItems ci
    WHERE ci.ChecklistItemId = @ChecklistItemId
      AND ci.EventId = @EventId;

    IF @CurrentStatus IS NULL
    BEGIN
        SELECT -1 AS Result, N'Checklist item not found for this event.' AS Message;
        RETURN;
    END

    IF @CurrentStatus <> N'Fully Taken'
    BEGIN
        SELECT -1 AS Result, N'Item must be Fully Taken before marking as Completed.' AS Message;
        RETURN;
    END

    UPDATE dbo.EventChecklistItems
    SET Status = N'Completed',
        UpdatedAt = GETUTCDATE()
    WHERE ChecklistItemId = @ChecklistItemId
      AND EventId = @EventId;

    SELECT 1 AS Result, N'Completed successfully' AS Message;
END
GO

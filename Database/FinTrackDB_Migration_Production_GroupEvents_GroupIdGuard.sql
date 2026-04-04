-- =============================================================================
-- If you already ran FinTrackDB_Migration_NewFeatures_Phase3to5.sql BEFORE
-- sp_UpdateGroupEvent / sp_DeleteGroupEvent included @GroupId, run this script
-- to align stored procedures with the API (route groupId must match the event).
-- Safe to run multiple times (CREATE OR ALTER).
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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

PRINT N'GroupEvents GroupId guard on update/delete applied.';
GO

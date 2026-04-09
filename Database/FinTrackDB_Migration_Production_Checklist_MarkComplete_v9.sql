/*
  Checklist endpoint support:
  PUT /api/Group/{groupId}/events/{eventId}/checklist/{itemId}/complete

  Rules:
  - Only event organizer (GroupEvents.CreatedByMemberId) can complete.
  - Checklist item must belong to the event.
  - Item can be completed only when current status is 'Fully Taken'.
*/

SET NOCOUNT ON;
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

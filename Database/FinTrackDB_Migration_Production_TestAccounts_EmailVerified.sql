/*
  Urgent test-account unblock referenced in FinShare_ForAbhilash_CompleteActions.pdf.

  Note:
    The PDF mentions dbo.Members with EmailAddress/IsEmailVerified, but this solution
    stores those fields in dbo.Users. This script applies the equivalent fix safely.
*/

SET NOCOUNT ON;
GO

UPDATE [dbo].[Users]
SET [IsEmailVerified] = 1
WHERE [EmailAddress] IN (N'joseph@whizsolutions.net', N'josephpx@gmail.com');
GO

SELECT [EmailAddress], [IsEmailVerified], [ExpiryDate]
FROM [dbo].[Users]
WHERE [EmailAddress] IN (N'joseph@whizsolutions.net', N'josephpx@gmail.com');
GO

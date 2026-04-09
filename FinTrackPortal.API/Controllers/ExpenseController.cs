using FinTrackPortal.API.Extensions;
using FinTrackPortal.API.Services;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers
{
    /// <summary>
    /// Group and personal expense endpoints — add, edit, delete, list, move.
    /// Also exposes account tag management and receipt/invoice attachment uploads.
    /// All endpoints require JWT authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        private readonly IGroupService _groupService;
        private readonly IAttachmentStorageService _attachmentStorage;
        private readonly INotificationService _notifications;
        private readonly IAuditLogRepository _audit;

        public ExpenseController(
            IExpenseService expenseService,
            IGroupService groupService,
            IAttachmentStorageService attachmentStorage,
            INotificationService notifications,
            IAuditLogRepository audit)
        {
            _expenseService = expenseService;
            _groupService = groupService;
            _attachmentStorage = attachmentStorage;
            _notifications = notifications;
            _audit = audit;
        }

        /// <summary>
        /// POST /api/Expense/add — Add a group expense with equal or custom split.
        /// Validates that the caller, payer, and all split members belong to the group.
        /// For "Custom" splits, CustomAmounts must match Members count and sum to Amount.
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddExpenseRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var createdBy = User.GetEmail();
            var memberId = User.GetMemberId();

            var memberCheck = await _groupService.IsMemberOfGroupAsync(request.GroupId, memberId);
            if (!memberCheck.IsSuccess || !memberCheck.Data)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "You must belong to the group to add an expense."));

            var paidByCheck = await _groupService.IsMemberOfGroupAsync(request.GroupId, request.PaidBy);
            if (!paidByCheck.IsSuccess || !paidByCheck.Data)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "PaidBy must be a valid member of the group."));

            foreach (var m in request.Members)
            {
                var check = await _groupService.IsMemberOfGroupAsync(request.GroupId, m);
                if (!check.IsSuccess || !check.Data)
                    return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", $"Member {m} does not belong to the group."));
            }

            if (string.Equals(request.SplitType, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                if (request.CustomAmounts == null || request.CustomAmounts.Count != request.Members.Count)
                    return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "CustomAmounts must match the number of members for Custom split."));

                if (request.CustomAmounts.Sum() != request.Amount)
                    return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "CustomAmounts must sum up to the total Amount."));
            }

            if (request.ChecklistItemId.HasValue && !request.EventId.HasValue)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "EventId is required when ChecklistItemId is provided."));

            if (!string.IsNullOrWhiteSpace(request.CurrencyCode) &&
                !string.Equals(request.CurrencyCode, "AED", StringComparison.OrdinalIgnoreCase) &&
                !request.AmountOriginal.HasValue)
            {
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "AmountOriginal is required for non-AED currency."));
            }

            if (request.AmountOriginal.HasValue && request.AmountOriginal.Value <= 0)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "AmountOriginal must be greater than zero."));

            var result = await _expenseService.AddExpenseAsync(
                request.GroupId,
                request.Description,
                request.Amount,
                request.CurrencyCode,
                request.AmountOriginal,
                request.PaidBy,
                request.SplitType,
                request.Members,
                request.CustomAmounts,
                createdBy,
                request.EventId,
                request.ChecklistItemId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to add expense", result.ErrorMessage!));

            var members = await _groupService.GetGroupMembersAsync(request.GroupId);
            if (members.IsSuccess && members.Data != null)
            {
                try
                {
                    foreach (var member in members.Data.Where(m => m.MemberId != memberId))
                    {
                        await _notifications.CreateAsync(
                            member.MemberId,
                            "New Expense Added",
                            $"{createdBy} added '{request.Description}' - {request.Amount:N2} {request.CurrencyCode}",
                            "expense",
                            $"/group/{request.GroupId}");
                    }
                }
                catch
                {
                    // Notification delivery should never block expense creation.
                }
            }

            if (request.ChecklistItemId.HasValue)
            {
                await _audit.WriteAsync(
                    memberId,
                    AuditChecklistActions.ChecklistExpenseLinked,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    true,
                    $"ExpenseId={result.Data}, ChecklistItemId={request.ChecklistItemId}, EventId={request.EventId}");
            }

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                expenseId = result.Data,
                amount = request.Amount,
                paidBy = request.PaidBy
            }, "Expense added successfully"));
        }

        /// <summary>
        /// PUT /api/Expense/edit — Update expense details and rebuild all splits in a transaction.
        /// </summary>
        [HttpPut("edit")]
        public async Task<IActionResult> Edit([FromBody] EditExpenseRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var modifiedBy = User.GetEmail();

            if (string.Equals(request.SplitType, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                if (request.CustomAmounts == null || request.CustomAmounts.Count != request.Members.Count)
                    return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "CustomAmounts must match the number of members for Custom split."));

                if (request.CustomAmounts.Sum() != request.Amount)
                    return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", "CustomAmounts must sum up to the total Amount."));
            }

            var result = await _expenseService.EditExpenseAsync(
                request.ExpenseId,
                request.Description,
                request.Amount,
                request.CurrencyCode,
                request.PaidBy,
                request.SplitType,
                request.Members,
                request.CustomAmounts,
                modifiedBy,
                request.ExpenseCategory,
                request.ForReference);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to update expense", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                expenseId = request.ExpenseId
            }, "Expense updated successfully"));
        }

        /// <summary>DELETE /api/Expense/delete/{expenseId} — Soft-delete an expense and its splits.</summary>
        [HttpDelete("delete/{expenseId}")]
        public async Task<IActionResult> Delete(long expenseId)
        {
            var modifiedBy = User.GetEmail();

            var result = await _expenseService.DeleteExpenseAsync(expenseId, modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to delete expense", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                expenseId
            }, "Expense deleted successfully"));
        }

        /// <summary>GET /api/Expense/group/{groupId} — List all active expenses for a group.</summary>
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetByGroup(long groupId)
        {
            var result = await _expenseService.GetExpensesByGroupAsync(groupId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get expenses", result.ErrorMessage!));

            return Ok(ApiResponse<List<ExpenseResponse>>.SuccessResponse(result.Data!, "Expenses retrieved successfully"));
        }

        /// <summary>POST /api/Expense/personal — Add a personal (non-group) expense for the logged-in user.</summary>
        [HttpPost("personal")]
        public async Task<IActionResult> AddPersonal([FromBody] AddPersonalExpenseRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var memberId = User.GetMemberId();
            var createdBy = User.GetEmail();

            var result = await _expenseService.AddPersonalExpenseAsync(
                request.Description,
                request.Amount,
                request.CurrencyCode,
                memberId,
                createdBy,
                request.ExpenseDate,
                request.AccountId,
                request.ExpenseCategory,
                request.ForReference);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to add personal expense", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                expenseId = result.Data,
                amount = request.Amount
            }, "Personal expense added successfully"));
        }

        /// <summary>GET /api/Expense/personal — List personal/office expenses; optional category filter (Personal, Office).</summary>
        [HttpGet("personal")]
        public async Task<IActionResult> GetPersonal([FromQuery] string? category)
        {
            var memberId = User.GetMemberId();

            var result = await _expenseService.GetPersonalExpensesAsync(memberId, category);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get personal expenses", result.ErrorMessage!));

            return Ok(ApiResponse<List<ExpenseResponse>>.SuccessResponse(result.Data!, "Personal expenses retrieved successfully"));
        }

        /// <summary>PUT /api/Expense/personal/edit — Update a personal or office expense for the logged-in user.</summary>
        [HttpPut("personal/edit")]
        public async Task<IActionResult> EditPersonal([FromBody] EditPersonalExpenseRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var memberId = User.GetMemberId();
            var modifiedBy = User.GetEmail();

            var result = await _expenseService.UpdatePersonalExpenseAsync(
                request.ExpenseId,
                memberId,
                request.Description,
                request.Amount,
                request.CurrencyCode,
                request.ExpenseDate,
                request.AccountId,
                request.ExpenseCategory,
                request.ForReference,
                modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to update personal expense", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new { expenseId = request.ExpenseId }, "Personal expense updated successfully"));
        }

        /// <summary>
        /// POST /api/Expense/move — Move an expense to a different group.
        /// Splits remain unchanged; edit the expense after moving if needed.
        /// </summary>
        [HttpPost("move")]
        public async Task<IActionResult> Move([FromBody] MoveExpenseRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var modifiedBy = User.GetEmail();

            var result = await _expenseService.MoveExpenseAsync(request.ExpenseId, request.NewGroupId, modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to move expense", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                expenseId = request.ExpenseId,
                newGroupId = request.NewGroupId
            }, "Expense moved successfully"));
        }

        // ================================================================
        // Account Tags
        // ================================================================

        /// <summary>GET /api/Expense/accounts/{userId} — List all expense account labels for a user.</summary>
        [HttpGet("accounts/{userId}")]
        public async Task<IActionResult> GetAccounts(long userId)
        {
            var result = await _expenseService.GetUserAccountsAsync(userId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get accounts", result.ErrorMessage!));

            return Ok(ApiResponse<List<ExpenseAccountResponse>>.SuccessResponse(result.Data!, "Accounts retrieved successfully"));
        }

        /// <summary>POST /api/Expense/accounts — Create a new account label.</summary>
        [HttpPost("accounts")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var result = await _expenseService.CreateAccountAsync(request.UserId, request.AccountName, request.AccountColor);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to create account", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new { accountId = result.Data }, "Account created successfully"));
        }

        /// <summary>DELETE /api/Expense/accounts/{accountId} — Soft-delete an account label.</summary>
        [HttpDelete("accounts/{accountId}")]
        public async Task<IActionResult> DeleteAccount(long accountId)
        {
            var result = await _expenseService.DeleteAccountAsync(accountId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to delete account", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new { accountId }, "Account removed successfully"));
        }

        // ================================================================
        // Receipt / Invoice Attachments
        // ================================================================

        /// <summary>GET /api/Expense/attachments/my — All receipts uploaded by the current user with expense context.</summary>
        [HttpGet("attachments/my")]
        public async Task<IActionResult> GetMyAttachments()
        {
            var memberId = User.GetMemberId();
            var result = await _expenseService.GetAllAttachmentsAsync(memberId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to retrieve attachments", result.ErrorMessage!));

            return Ok(ApiResponse<List<MemberAttachmentResponse>>.SuccessResponse(result.Data!, "Attachments retrieved successfully"));
        }

        /// <summary>POST /api/Expense/payer — Record how much a member paid toward an expense (multi-payer).</summary>
        [HttpPost("payer")]
        public async Task<IActionResult> AddExpensePayer([FromBody] AddExpensePayerRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed"));

            if (request.AmountPaid <= 0)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Amount paid must be greater than zero."));

            var result = await _expenseService.AddExpensePayerAsync(request);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to record payer", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new { payerId = result.Data }, "Payer recorded successfully."));
        }

        /// <summary>GET /api/Expense/{expenseId}/payers — List who paid how much for an expense.</summary>
        [HttpGet("{expenseId:long}/payers")]
        public async Task<IActionResult> GetExpensePayers(long expenseId)
        {
            var result = await _expenseService.GetExpensePayersAsync(expenseId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get payers", result.ErrorMessage!));

            return Ok(ApiResponse<List<ExpensePayerResponse>>.SuccessResponse(result.Data!, "Payers retrieved successfully."));
        }

        /// <summary>
        /// POST /api/Expense/{expenseId}/attachment — Upload a receipt or invoice (JPG, PNG, PDF).
        /// Storage backend is selected by <c>AttachmentStorage:Provider</c> (Azure or Local).
        /// </summary>
        [HttpPost("{expenseId}/attachment")]
        [RequestSizeLimit(10_485_760)]
        public async Task<IActionResult> UploadAttachment(long expenseId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<object?>.ErrorResponse("No file provided"));

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!Array.Exists(allowed, e => e == ext))
                return BadRequest(ApiResponse<object?>.ErrorResponse("Only JPG, PNG and PDF files are allowed."));

            var uploadedBy = User.GetEmail();
            var fileUrl = await _attachmentStorage.UploadFileAsync(file);
            var fileType = ext == ".pdf" ? "pdf" : "image";
            var sizeKB = (int)(file.Length / 1024);

            var result = await _expenseService.AddAttachmentAsync(expenseId, file.FileName, fileUrl, fileType, sizeKB, uploadedBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to save attachment", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                attachmentId = result.Data,
                fileUrl
            }, "Attachment uploaded successfully"));
        }

        /// <summary>GET /api/Expense/{expenseId}/attachments — List all attachments for an expense.</summary>
        [HttpGet("{expenseId}/attachments")]
        public async Task<IActionResult> GetAttachments(long expenseId)
        {
            var result = await _expenseService.GetAttachmentsAsync(expenseId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get attachments", result.ErrorMessage!));

            return Ok(ApiResponse<List<AttachmentResponse>>.SuccessResponse(result.Data!, "Attachments retrieved successfully"));
        }

        /// <summary>DELETE /api/Expense/attachment/{attachmentId} — Soft-delete an attachment record.</summary>
        [HttpDelete("attachment/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(long attachmentId)
        {
            var result = await _expenseService.DeleteAttachmentAsync(attachmentId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to delete attachment", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new { attachmentId }, "Attachment removed successfully"));
        }
    }
}

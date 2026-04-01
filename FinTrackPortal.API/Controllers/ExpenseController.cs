using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers
{
    /// <summary>
    /// Group and personal expense endpoints — add, edit, delete, list.
    /// Group expense operations validate membership before proceeding.
    /// All endpoints require JWT authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        private readonly IGroupService _groupService;

        public ExpenseController(IExpenseService expenseService, IGroupService groupService)
        {
            _expenseService = expenseService;
            _groupService = groupService;
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

            var result = await _expenseService.AddExpenseAsync(
                request.GroupId,
                request.Description,
                request.Amount,
                request.PaidBy,
                request.SplitType,
                request.Members,
                request.CustomAmounts,
                createdBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to add expense", result.ErrorMessage!));

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
                request.PaidBy,
                request.SplitType,
                request.Members,
                request.CustomAmounts,
                modifiedBy);

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
                memberId,
                createdBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to add personal expense", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                expenseId = result.Data,
                amount = request.Amount
            }, "Personal expense added successfully"));
        }

        /// <summary>GET /api/Expense/personal — List personal expenses for the logged-in user.</summary>
        [HttpGet("personal")]
        public async Task<IActionResult> GetPersonal()
        {
            var memberId = User.GetMemberId();

            var result = await _expenseService.GetPersonalExpensesAsync(memberId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get personal expenses", result.ErrorMessage!));

            return Ok(ApiResponse<List<ExpenseResponse>>.SuccessResponse(result.Data!, "Personal expenses retrieved successfully"));
        }
    }
}

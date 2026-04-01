using FinTrackPortal.API.Extensions;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddExpenseRequest request)
        {
            var createdBy = User.GetEmail();

            var result = await _expenseService.AddExpenseAsync(
                request.GroupId,
                request.Description,
                request.Amount,
                request.PaidBy,
                request.Members,
                createdBy);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { expenseId = result.Data });
        }

        [HttpPut("edit")]
        public async Task<IActionResult> Edit([FromBody] EditExpenseRequest request)
        {
            var modifiedBy = User.GetEmail();

            var result = await _expenseService.EditExpenseAsync(
                request.ExpenseId,
                request.Description,
                request.Amount,
                request.PaidBy,
                request.Members,
                modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { message = "Expense updated successfully" });
        }

        [HttpDelete("delete/{expenseId}")]
        public async Task<IActionResult> Delete(long expenseId)
        {
            var modifiedBy = User.GetEmail();

            var result = await _expenseService.DeleteExpenseAsync(expenseId, modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { message = "Expense deleted successfully" });
        }
    }
}

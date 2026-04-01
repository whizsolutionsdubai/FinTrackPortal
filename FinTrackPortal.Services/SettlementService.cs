using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    /// <summary>
    /// Settlement business logic. Record/history delegate to the repository.
    /// Suggested settlements are calculated here using a greedy debtor-creditor algorithm.
    /// </summary>
    public class SettlementService : ISettlementService
    {
        private readonly ISettlementRepository _repository;
        private readonly IGroupRepository _groupRepository;

        public SettlementService(
            ISettlementRepository repository,
            IGroupRepository groupRepository)
        {
            _repository = repository;
            _groupRepository = groupRepository;
        }

        public Task<OperationResult<long>> RecordSettlementAsync(
            long groupId, long fromMemberId, long toMemberId,
            decimal amount, string createdBy)
            => _repository.RecordSettlementAsync(groupId, fromMemberId, toMemberId, amount, createdBy);

        public Task<OperationResult<List<SettlementResponse>>> GetSettlementsByGroupAsync(long groupId)
            => _repository.GetSettlementsByGroupAsync(groupId);

        public async Task<OperationResult<List<SuggestedSettlementResponse>>> GetSuggestedSettlementsAsync(long groupId)
        {
            var summaryResult = await _groupRepository.GetGroupSummaryAsync(groupId);
            if (!summaryResult.IsSuccess)
                return OperationResult<List<SuggestedSettlementResponse>>.Failure(summaryResult.ErrorMessage!);

            var members = summaryResult.Data!.Members;

            var debtors = members
                .Where(m => m.Net < 0)
                .Select(m => new { m.MemberId, m.Name, Amount = -m.Net })
                .ToList();

            var creditors = members
                .Where(m => m.Net > 0)
                .Select(m => new { m.MemberId, m.Name, Amount = m.Net })
                .ToList();

            var suggestions = new List<SuggestedSettlementResponse>();
            int d = 0, c = 0;
            decimal[] debtAmounts = debtors.Select(x => x.Amount).ToArray();
            decimal[] creditAmounts = creditors.Select(x => x.Amount).ToArray();

            while (d < debtors.Count && c < creditors.Count)
            {
                decimal pay = Math.Min(debtAmounts[d], creditAmounts[c]);
                suggestions.Add(new SuggestedSettlementResponse
                {
                    FromMemberId = debtors[d].MemberId,
                    FromMemberName = debtors[d].Name,
                    ToMemberId = creditors[c].MemberId,
                    ToMemberName = creditors[c].Name,
                    Amount = Math.Round(pay, 2)
                });

                debtAmounts[d] -= pay;
                creditAmounts[c] -= pay;

                if (debtAmounts[d] == 0) d++;
                if (creditAmounts[c] == 0) c++;
            }

            return OperationResult<List<SuggestedSettlementResponse>>.Success(suggestions);
        }
    }
}

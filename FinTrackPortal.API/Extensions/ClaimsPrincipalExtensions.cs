using System.Security.Claims;

namespace FinTrackPortal.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static long GetMemberId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("MemberId")
                ?? throw new UnauthorizedAccessException("MemberId claim is missing from the token.");
            return long.Parse(claim.Value);
        }

        public static string GetEmail(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.Email)
                ?? throw new UnauthorizedAccessException("Email claim is missing from the token.");
            return claim.Value;
        }
    }
}

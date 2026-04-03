namespace FinTrackPortal.Common;

/// <summary>Values for <c>AuditLogs.Action</c> on authentication flows (FinShare security spec).</summary>
public static class AuditAuthActions
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string LoginBlocked = "LOGIN_BLOCKED";
    public const string Register = "REGISTER";
    public const string EmailVerified = "EMAIL_VERIFIED";
    public const string EmailVerifyFailed = "EMAIL_VERIFY_FAILED";
    public const string ForgotPassword = "FORGOT_PASSWORD";
    public const string PasswordReset = "PASSWORD_RESET";
    public const string PasswordResetFailed = "PASSWORD_RESET_FAILED";
}

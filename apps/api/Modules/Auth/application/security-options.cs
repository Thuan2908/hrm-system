namespace Hrm.Modules.Auth.Application;

public sealed class SecurityOptions
{
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int AdminLockYears { get; set; } = 10;
    public int ActiveSessionTimeoutMinutes { get; set; } = 3;
    public int BcryptWorkFactor { get; set; } = 12;
}

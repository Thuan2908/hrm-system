using Hrm.Contracts;

namespace Hrm.UnitTests;

public sealed class SecurityPolicyAndPermissionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1")]
    [InlineData("12")]
    public void PasswordPolicyShouldRejectPasswordsShorterThan3Characters(string? password)
    {
        var isValid = PasswordPolicy.IsValid(password, out var errorMessage);

        Assert.False(isValid);
        Assert.Equal("Mật khẩu phải có ít nhất 3 ký tự.", errorMessage);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678")]
    [InlineData("Password123!")]
    [InlineData("VeryLongAndSecurePassword2026")]
    public void PasswordPolicyShouldAcceptPasswordsWithAtLeast3Characters(string password)
    {
        var isValid = PasswordPolicy.IsValid(password, out var errorMessage);

        Assert.True(isValid);
        Assert.Empty(errorMessage);
    }

    [Fact]
    public void PasswordPolicyConstantsShouldHaveExpectedValues()
    {
        Assert.Equal(3, PasswordPolicy.MinLength);
        Assert.Equal(12, PasswordPolicy.BcryptWorkFactor);
    }

    [Fact]
    public void PermissionCodesShouldContainAttendanceTeamRead()
    {
        Assert.Equal("attendance.team.read", PermissionCodes.AttendanceTeamRead);
        Assert.Contains(PermissionCodes.AttendanceTeamRead, PermissionCodes.All);
        Assert.Contains(PermissionCodes.Catalog, c => c.Code == PermissionCodes.AttendanceTeamRead);
    }

    [Fact]
    public void PermissionCodesExpandWithAliasesShouldExpandAttendanceTeamReadAndAttApprove()
    {
        var fromNewCode = PermissionCodes.ExpandWithAliases([PermissionCodes.AttendanceTeamRead]);
        Assert.Contains(PermissionCodes.AttendanceTeamRead, fromNewCode);
        Assert.Contains("ATT_APPROVE", fromNewCode);
        Assert.Contains("attendance.team.approve", fromNewCode);

        var fromLegacyCode = PermissionCodes.ExpandWithAliases(["ATT_APPROVE"]);
        Assert.Contains(PermissionCodes.AttendanceTeamRead, fromLegacyCode);
        Assert.Contains("ATT_APPROVE", fromLegacyCode);
        Assert.Contains("attendance.team.approve", fromLegacyCode);
    }

    [Fact]
    public void PermissionCodesCanonicalizeShouldMapLegacyAliasesToAttendanceTeamRead()
    {
        Assert.Equal(PermissionCodes.AttendanceTeamRead, PermissionCodes.Canonicalize("ATT_APPROVE"));
        Assert.Equal(PermissionCodes.AttendanceTeamRead, PermissionCodes.Canonicalize("attendance.team.approve"));
        Assert.Equal(PermissionCodes.AttendanceTeamRead, PermissionCodes.Canonicalize(PermissionCodes.AttendanceTeamRead));
    }
}

using Hrm.Contracts;

namespace Hrm.UnitTests;

public sealed class LoginContractTests
{
    [Fact]
    public void LoginRequestWithDefaultDeviceIdIsNull()
    {
        var request = new LoginRequest("nv_saigon", "123456");

        Assert.Equal("nv_saigon", request.UserName);
        Assert.Equal("123456", request.Password);
        Assert.Null(request.DeviceId);
    }

    [Fact]
    public void LoginRequestWithExplicitDeviceIdHasValue()
    {
        var request = new LoginRequest("nv_saigon", "123456", "device-12345");

        Assert.Equal("device-12345", request.DeviceId);
    }

    [Fact]
    public void HeartbeatRequestRecordPropertiesWorkCorrectly()
    {
        var req1 = new HeartbeatRequest();
        var req2 = new HeartbeatRequest("dev-99");

        Assert.Null(req1.DeviceId);
        Assert.Equal("dev-99", req2.DeviceId);
    }

    [Fact]
    public void UpdateProfileRequestValidatesCorrectly()
    {
        var req = new UpdateProfileRequest("Nguyễn Văn A");
        Assert.Equal("Nguyễn Văn A", req.FullName);
        Assert.Null(req.Phone);
        Assert.Null(req.Email);

        var req2 = new UpdateProfileRequest(
            "Nguyễn Văn B",
            Phone: "0901234567",
            Email: "b@saigonretail.vn",
            Address: "123 Lê Lợi, Q1",
            DateOfBirth: new DateOnly(1995, 5, 20),
            Gender: "Male");

        Assert.Equal("Nguyễn Văn B", req2.FullName);
        Assert.Equal("0901234567", req2.Phone);
        Assert.Equal("b@saigonretail.vn", req2.Email);
        Assert.Equal("123 Lê Lợi, Q1", req2.Address);
        Assert.Equal(new DateOnly(1995, 5, 20), req2.DateOfBirth);
        Assert.Equal("Male", req2.Gender);
    }

    [Fact]
    public void ChangePasswordRequestValidatesCorrectly()
    {
        var req = new ChangePasswordRequest("OldPass123", "NewPass456");
        Assert.Equal("OldPass123", req.CurrentPassword);
        Assert.Equal("NewPass456", req.NewPassword);
    }

    [Fact]
    public void EmployeeProfileDtoStoresAllFieldsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = new EmployeeProfileDto(
            EmployeeId: 10,
            EmployeeCode: "EMP-010",
            FullName: "Trần Thị C",
            UserName: "nv_c",
            RoleName: "EMPLOYEE",
            DepartmentName: "Phòng Công nghệ",
            DepartmentCode: "IT",
            PositionName: "Senior Developer",
            DateOfBirth: new DateOnly(1992, 8, 15),
            Gender: "Female",
            Phone: "0988776655",
            Email: "c@saigonretail.vn",
            Address: "456 Nguyễn Huệ",
            EducationLevel: "Đại học",
            BaseSalary: 25000000m,
            JoinDate: new DateOnly(2022, 1, 10),
            HireDate: new DateOnly(2021, 11, 1),
            Status: "ACTIVE",
            CreatedAt: now);

        Assert.Equal(10, profile.EmployeeId);
        Assert.Equal("EMP-010", profile.EmployeeCode);
        Assert.Equal("Trần Thị C", profile.FullName);
        Assert.Equal("IT", profile.DepartmentCode);
        Assert.Equal("Senior Developer", profile.PositionName);
        Assert.Equal(25000000m, profile.BaseSalary);
        Assert.Equal("ACTIVE", profile.Status);
    }

    [Fact]
    public void AccountStatusDtoRepresentsAccessCorrectly()
    {
        var activeStatus = new AccountStatusDto(
            IsActive: true,
            IsLocked: false,
            IsResigned: false,
            CanAccess: true,
            Reason: "active",
            Message: "OK");
        Assert.True(activeStatus.CanAccess);

        var lockedStatus = new AccountStatusDto(
            IsActive: true,
            IsLocked: true,
            IsResigned: false,
            CanAccess: false,
            Reason: "locked",
            Message: "Tài khoản bị khóa");
        Assert.False(lockedStatus.CanAccess);
        Assert.Equal("locked", lockedStatus.Reason);

        var resignedStatus = new AccountStatusDto(
            IsActive: false,
            IsLocked: true,
            IsResigned: true,
            CanAccess: false,
            Reason: "resigned",
            Message: "Nhân viên thôi việc");
        Assert.False(resignedStatus.CanAccess);
        Assert.True(resignedStatus.IsResigned);
    }
}

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
    }

    [Fact]
    public void ChangePasswordRequestValidatesCorrectly()
    {
        var req = new ChangePasswordRequest("OldPass123", "NewPass456");
        Assert.Equal("OldPass123", req.CurrentPassword);
        Assert.Equal("NewPass456", req.NewPassword);
    }
}

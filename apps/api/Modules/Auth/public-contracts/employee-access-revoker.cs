namespace Hrm.Modules.Auth.Contracts;

public interface IEmployeeAccessRevoker
{
    Task RevokeAsync(long employeeId, long actorUserId, CancellationToken cancellationToken);
}

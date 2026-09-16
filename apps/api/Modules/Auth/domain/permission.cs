namespace Hrm.Modules.Auth.Domain;

public sealed class PermissionDefinition
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
}

public sealed class RolePermissionGrant
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public ApplicationRole Role { get; set; } = null!;
    public PermissionDefinition Permission { get; set; } = null!;
}

using Hrm.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<ApplicationRole> Roles => Set<ApplicationRole>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PermissionDefinition> Permissions => Set<PermissionDefinition>();
    public DbSet<RolePermissionGrant> RolePermissions => Set<RolePermissionGrant>();
    public DbSet<UserSecurityState> UserSecurityStates => Set<UserSecurityState>();
    public DbSet<UserAccountMetadata> UserAccountMetadata => Set<UserAccountMetadata>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();
    public DbSet<UserActiveSession> ActiveSessions => Set<UserActiveSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder;
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("user_id").ValueGeneratedNever();
            entity.Property(user => user.EmployeeId).HasColumnName("emp_id");
            entity.Property(user => user.RoleId).HasColumnName("role_id");
            entity.Property(user => user.UserName).HasColumnName("username");
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash");
            entity.Property(user => user.IsActive).HasColumnName("is_active");
            entity.Property(user => user.LastLoginAt).HasColumnName("last_login");
            entity.HasIndex(user => user.UserName).IsUnique();
            entity.HasOne(user => user.Role).WithMany().HasForeignKey(user => user.RoleId);
            entity.HasOne(user => user.Employee).WithMany().HasForeignKey(user => user.EmployeeId);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("role_id").ValueGeneratedNever();
            entity.Property(role => role.Name).HasColumnName("role_name");
            entity.Property(role => role.Description).HasColumnName("description");
        });

        builder.Entity<Position>(entity =>
        {
            entity.ToTable("positions");
            entity.HasKey(position => position.Id);
            entity.Property(position => position.Id).HasColumnName("position_id").ValueGeneratedNever();
            entity.Property(position => position.Code).HasColumnName("position_code");
            entity.Property(position => position.Name).HasColumnName("position_name");
            entity.Property(position => position.Title).HasColumnName("title");
        });

        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(employee => employee.Id);
            entity.Property(employee => employee.Id).HasColumnName("emp_id").ValueGeneratedNever();
            entity.Property(employee => employee.DepartmentId).HasColumnName("dept_id");
            entity.Property(employee => employee.PositionId).HasColumnName("position_id");
            entity.Property(employee => employee.Code).HasColumnName("emp_code");
            entity.Property(employee => employee.FullName).HasColumnName("full_name");
            entity.Property(employee => employee.DateOfBirth).HasColumnName("dob");
            entity.Property(employee => employee.Gender).HasColumnName("gender");
            entity.Property(employee => employee.Phone).HasColumnName("phone");
            entity.Property(employee => employee.Email).HasColumnName("email");
            entity.Property(employee => employee.Address).HasColumnName("address");
            entity.Property(employee => employee.EducationLevel).HasColumnName("education_level");
            entity.Property(employee => employee.BaseSalary).HasColumnName("base_salary");
            entity.Property(employee => employee.JoinDate).HasColumnName("join_date");
            entity.Property(employee => employee.HireDate).HasColumnName("hire_date");
            entity.Property(employee => employee.Status).HasColumnName("status");
            entity.Property(employee => employee.CreatedAt).HasColumnName("created_at");
            entity.HasOne(employee => employee.Department).WithMany().HasForeignKey(employee => employee.DepartmentId);
            entity.HasOne(employee => employee.Position).WithMany().HasForeignKey(employee => employee.PositionId).IsRequired(false);
        });

        builder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(department => department.Id);
            entity.Property(department => department.Id).HasColumnName("dept_id").ValueGeneratedNever();
            entity.Property(department => department.Code).HasColumnName("dept_code");
            entity.Property(department => department.Name).HasColumnName("dept_name");
        });

        builder.Entity<PermissionDefinition>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Id).HasColumnName("perm_id").ValueGeneratedNever();
            entity.Property(permission => permission.Code).HasColumnName("permission_code");
            entity.Property(permission => permission.Description).HasColumnName("description");
        });

        builder.Entity<RolePermissionGrant>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(grant => new { grant.RoleId, grant.PermissionId });
            entity.Property(grant => grant.RoleId).HasColumnName("role_id");
            entity.Property(grant => grant.PermissionId).HasColumnName("perm_id");
            entity.HasOne(grant => grant.Role).WithMany().HasForeignKey(grant => grant.RoleId);
            entity.HasOne(grant => grant.Permission).WithMany().HasForeignKey(grant => grant.PermissionId);
        });

        builder.Entity<UserSecurityState>(entity =>
        {
            entity.ToTable("user_security_states");
            entity.HasKey(state => state.UserId);
            entity.Property(state => state.UserId).HasColumnName("user_id").ValueGeneratedNever();
            entity.Property(state => state.FailedAccessCount).HasColumnName("failed_access_count");
            entity.Property(state => state.LockoutEnd).HasColumnName("lockout_end");
            entity.HasOne(state => state.User).WithOne(user => user.SecurityState)
                .HasForeignKey<UserSecurityState>(state => state.UserId);
        });

        builder.Entity<UserAccountMetadata>(entity =>
        {
            entity.ToTable("user_account_metadata");
            entity.HasKey(metadata => metadata.UserId);
            entity.Property(metadata => metadata.UserId).HasColumnName("user_id").ValueGeneratedNever();
            entity.Property(metadata => metadata.CreatedAt).HasColumnName("created_at");
            entity.HasOne(metadata => metadata.User).WithOne(user => user.Metadata)
                .HasForeignKey<UserAccountMetadata>(metadata => metadata.UserId);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).HasColumnName("id");
            entity.Property(token => token.UserId).HasColumnName("user_id");
            entity.Property(token => token.TokenHash).HasColumnName("token_hash");
            entity.Property(token => token.ExpiresAt).HasColumnName("expires_at");
            entity.Property(token => token.CreatedAt).HasColumnName("created_at");
            entity.Property(token => token.CreatedByIp).HasColumnName("created_by_ip");
            entity.Property(token => token.RevokedAt).HasColumnName("revoked_at");
            entity.Property(token => token.RevokedByIp).HasColumnName("revoked_by_ip");
            entity.Property(token => token.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash");
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasOne(token => token.User).WithMany().HasForeignKey(token => token.UserId);
        });

        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id).HasColumnName("id");
            entity.Property(entry => entry.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(entry => entry.Action).HasColumnName("action");
            entity.Property(entry => entry.EntityType).HasColumnName("entity_type");
            entity.Property(entry => entry.EntityId).HasColumnName("entity_id");
            entity.Property(entry => entry.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb");
            entity.Property(entry => entry.AfterJson).HasColumnName("after_json").HasColumnType("jsonb");
            entity.Property(entry => entry.CreatedAt).HasColumnName("created_at");
        });

        builder.Entity<UserActiveSession>(entity =>
        {
            entity.ToTable("user_active_sessions");
            entity.HasKey(session => session.UserId);
            entity.Property(session => session.UserId).HasColumnName("user_id").ValueGeneratedNever();
            entity.Property(session => session.DeviceId).HasColumnName("device_id");
            entity.Property(session => session.IpAddress).HasColumnName("ip_address");
            entity.Property(session => session.UserAgent).HasColumnName("user_agent");
            entity.Property(session => session.CreatedAt).HasColumnName("created_at");
            entity.Property(session => session.LastSeenAt).HasColumnName("last_seen_at");
            entity.HasOne(session => session.User).WithOne().HasForeignKey<UserActiveSession>(session => session.UserId);
        });
    }
}

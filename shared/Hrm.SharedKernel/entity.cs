namespace Hrm.SharedKernel;

public abstract class Entity
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; private init; }
}

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity(Guid id, DateTimeOffset createdAt)
        : base(id)
    {
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
    }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    protected void MarkUpdated(DateTimeOffset updatedAt) =>
        UpdatedAt = updatedAt.ToUniversalTime();
}

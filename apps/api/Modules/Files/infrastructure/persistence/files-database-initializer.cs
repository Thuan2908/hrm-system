using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Files.Infrastructure.Persistence;

public interface IFilesDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class FilesDatabaseInitializer(FilesDbContext dbContext) : IFilesDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7246012030)", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS file_attachments (
                id uuid PRIMARY KEY,
                file_name varchar(255) NOT NULL,
                content_type varchar(128) NOT NULL,
                file_size_bytes bigint NOT NULL,
                storage_path varchar(512) NOT NULL,
                module varchar(64) NOT NULL,
                reference_id varchar(128) NULL,
                uploaded_by_user_id bigint NOT NULL,
                created_at timestamp with time zone NOT NULL,
                is_deleted boolean NOT NULL DEFAULT false,
                deleted_at timestamp with time zone NULL
            );

            CREATE INDEX IF NOT EXISTS ix_file_attachments_module_ref
                ON file_attachments(module, reference_id);

            CREATE INDEX IF NOT EXISTS ix_file_attachments_uploaded_by
                ON file_attachments(uploaded_by_user_id);

            CREATE INDEX IF NOT EXISTS ix_file_attachments_created_at
                ON file_attachments(created_at);

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_fileattachments_users'
                ) THEN
                    ALTER TABLE file_attachments
                    ADD CONSTRAINT fk_fileattachments_users
                    FOREIGN KEY (uploaded_by_user_id) REFERENCES users(user_id) ON DELETE RESTRICT;
                END IF;
            END $$;
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}

using Hrm.Modules.Files.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Files.Infrastructure.Persistence;

public sealed class FilesDbContext(DbContextOptions<FilesDbContext> options) : DbContext(options)
{
    public DbSet<FileAttachment> Files => Set<FileAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileAttachment>(entity =>
        {
            entity.ToTable("file_attachments");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.FileName).HasMaxLength(255).IsRequired();
            entity.Property(f => f.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(f => f.StoragePath).HasMaxLength(512).IsRequired();
            entity.Property(f => f.Module).HasMaxLength(64).IsRequired();
            entity.Property(f => f.ReferenceId).HasMaxLength(128);

            entity.HasIndex(f => new { f.Module, f.ReferenceId });
            entity.HasIndex(f => f.UploadedByUserId);
            entity.HasIndex(f => f.CreatedAt);
        });
    }
}

using AICreativeArts.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AICreativeArts.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AIImage> AIImages => Set<AIImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AIImage>(entity =>
        {
            entity.ToTable("AIImages");
            entity.Property(i => i.Prompt).HasMaxLength(AIImageRules.PromptMaxLength);
            entity.Property(i => i.ImageGenerator).HasMaxLength(AIImageRules.GeneratorMaxLength);
            entity.Property(i => i.Filename).HasMaxLength(AIImageRules.FilenameMaxLength);

            // The gallery is always listed by popularity, so index the sort column.
            entity.HasIndex(i => i.Like);
        });
    }
}

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

    /// <summary>One row per (login, image) like, enforcing "one like per image per login".</summary>
    public DbSet<ImageLike> ImageLikes => Set<ImageLike>();

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

        builder.Entity<ImageLike>(entity =>
        {
            entity.ToTable("ImageLikes");
            entity.HasIndex(l => new { l.LoginId, l.AIImageId }).IsUnique();
            entity.HasOne(l => l.AIImage)
                .WithMany()
                .HasForeignKey(l => l.AIImageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

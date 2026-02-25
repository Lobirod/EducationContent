using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationContentService.Infrastructure.Postgres.Configurations;

public static class Index
{
    public const string TITLE = "ix_lessons_title";
}

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.OwnsOne(x => x.Title, b =>
        {
            b.Property(x => x.Value).HasColumnName("title");

            b.HasIndex(x => x.Value).IsUnique().HasDatabaseName(Index.TITLE);
        });

        builder.Property(x => x.Description)
            .HasConversion(
                v => v.Value,
                v => Description.Create(v).Value)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(l => l.IsDeleted)
            .HasDefaultValue(false)
            .HasColumnName("is_deleted");

        builder.Property(l => l.DeletedAt)
            .IsRequired(false)
            .HasColumnName("deletion_date");

        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())")
            .HasColumnName("created_at");

        builder.Property(l => l.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())")
            .HasColumnName("updated_at");

        /*builder.Property(l => l.VideoId)
            .IsRequired(false)
            .HasColumnName("video_id");

        builder.Property(l => l.PreviewId)
            .IsRequired(false)
            .HasColumnName("preview_id");*/
        
        builder.HasQueryFilter(l => !l.IsDeleted);
        
    }
}
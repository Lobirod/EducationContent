using System.Text.Json;
using FileService.Domain;
using FileService.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");
        builder.HasKey(x => x.Id);

        builder.HasDiscriminator<string>("asset_type")
            .HasValue<VideoAsset>("video")
            .HasValue<PreviewAsset>("preview");

        builder.OwnsOne(m => m.MediaData, mb =>
        {
            mb.ToJson("media_data");

            mb.OwnsOne(md => md.ContentType, cb =>
            {
                cb.Property(x => x.Category).HasConversion<string>().HasColumnName("category");
                cb.Property(x => x.Value).HasColumnName("value");
            });

            mb.OwnsOne(md => md.FileName, fb =>
            {
                fb.Property(x => x.Extension).HasColumnName("extension");
                fb.Property(x => x.Name).HasColumnName("name");
                fb.Property(x => x.Value).HasColumnName("value");
            });

            mb.Property(md => md.Size).HasColumnName("size");
            mb.Property(md => md.ExpectedChunksCount).HasColumnName("expected_chunks_count");
        });

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Status).HasConversion<string>();

        builder.Property(x => x.AssetType).HasConversion<string>();
        // builder.Property(x => x.OwnerId).HasColumnName("owner_id");
        // builder.Property(x => x.OwnerType).HasColumnName("owner_type");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Property(x => x.Key)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<StorageKey>(v, (JsonSerializerOptions?)null)!)
            .HasColumnName("key")
            .HasColumnType("jsonb");

        builder.Property(x => x.RawKey)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<StorageKey>(v, (JsonSerializerOptions?)null)!)
            .HasColumnName("raw_key")
            .HasColumnType("jsonb");

        builder.HasIndex(x => new
        {
            x.Status,
            x.CreatedAt,
        });
    }
}

public class VideoAssetConfiguration : IEntityTypeConfiguration<VideoAsset>
{
    public void Configure(EntityTypeBuilder<VideoAsset> builder)
    {
        builder.OwnsOne(va => va.Metadata, mb =>
        {
            mb.ToJson("metadata");
            mb.Property(m => m.Duration).HasColumnName("duration");
            mb.Property(m => m.Width).HasColumnName("width");
            mb.Property(m => m.Height).HasColumnName("height");
        });

        builder.Navigation(va => va.Metadata).IsRequired(false);
    }
}
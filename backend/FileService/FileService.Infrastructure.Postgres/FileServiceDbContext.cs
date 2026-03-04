using FileService.Core;
using FileService.Domain.Assets;
using FileService.Domain.MediaProcessing;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Postgres;

public class FileServiceDbContext : DbContext, IReadDbContext
{
    public FileServiceDbContext(DbContextOptions<FileServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    
    public DbSet<VideoProcess> VideoProcesses => Set<VideoProcess>();

    public IQueryable<MediaAsset> MediaAssetsQuery => MediaAssets.AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileServiceDbContext).Assembly);
    }
}

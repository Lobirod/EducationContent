using EducationContentService.Core.Database;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.ModuleItems;
using EducationContentService.Domain.Modules;
using Microsoft.EntityFrameworkCore;

namespace EducationContentService.Infrastructure.Postgres;

public class EducationDbContext: DbContext, IEducationReadDbContext
{
    public EducationDbContext(DbContextOptions<EducationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Lesson> Lessons => Set<Lesson>();
    
    /*public DbSet<Module> Modules => Set<Module>();
    
    public DbSet<ModuleItem> ModuleItems => Set<ModuleItem>();*/

    public IQueryable<Lesson> LessonsQuery => Lessons.AsNoTracking().AsQueryable();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EducationDbContext).Assembly);
    }

}
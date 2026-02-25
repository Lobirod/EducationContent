using EducationContentService.Core.Database;

namespace EducationContentService.Infrastructure.Postgres;

public class TransactionManager : ITransactionManager
{
    private readonly EducationDbContext _dbContext;

    public TransactionManager(EducationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

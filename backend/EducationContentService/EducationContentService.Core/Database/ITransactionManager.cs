namespace EducationContentService.Core.Database;

public interface ITransactionManager
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using System.Data;
using CSharpFunctionalExtensions;
using FileService.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Infrastructure.Postgres;

public class TransactionManager : ITransactionManager
{
    private readonly FileServiceDbContext _dbContext;
    private readonly ILogger<TransactionManager> _logger;

    public TransactionManager(FileServiceDbContext dbContext, ILogger<TransactionManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        IDbTransaction transaction1 = null;
        
        return transaction1;
        
        //return transaction.GetDbTransaction();
    }

    public async Task<Result<int, Error>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict during save");
            //return GeneralErrors.ConcurrencyConflict();
            return GeneralErrors.NotFound();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Operation cancelled during save");
            //return GeneralErrors.OperationCancelled();
            return GeneralErrors.NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during save");
            //return GeneralErrors.DatabaseError();
            return GeneralErrors.NotFound();
        }
    }
}
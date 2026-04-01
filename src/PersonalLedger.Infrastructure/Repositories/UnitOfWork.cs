using Microsoft.EntityFrameworkCore.Storage;
using PersonalLedger.Domain.Repositories;
using PersonalLedger.Infrastructure.Persistence;

namespace PersonalLedger.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        public IUnitOfWorkScope BeginScope()
        {
            var transaction = _context.Database.BeginTransaction();

            return new UnitOfWorkScope(_context, transaction);
        }
    }

    public class UnitOfWorkScope : IUnitOfWorkScope
    {
        private readonly ApplicationDbContext _context;
        private readonly IDbContextTransaction _transaction;
        private bool _committed;

        public UnitOfWorkScope(
            ApplicationDbContext context,
            IDbContextTransaction transaction)
        {
            _context = context;
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);

            _committed = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (!_committed)
                _transaction.Rollback();

            _transaction.Dispose();
        }
    }
}

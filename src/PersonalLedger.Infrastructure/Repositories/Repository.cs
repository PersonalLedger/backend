using Microsoft.EntityFrameworkCore;
using PersonalLedger.Domain.Repositories;
using PersonalLedger.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace PersonalLedger.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        private readonly IUnitOfWork unitOfWork;

        public Repository(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
            this.unitOfWork = unitOfWork;
        }

        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            await _dbSet.FindAsync(new object[] { id }, cancellationToken);

        public virtual async Task<T?> GetFirstAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) =>
            await _dbSet.FirstOrDefaultAsync(filter, cancellationToken);

        public virtual async Task<IEnumerable<T>> ListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) =>
            await _dbSet.AnyAsync(filter, cancellationToken);

        public async Task<int> CountAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) =>  
            await _dbSet.CountAsync(filter, cancellationToken);

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default) 
        {
            using (var scope = unitOfWork.BeginScope())
            {
                await _dbSet.AddAsync(entity, cancellationToken); 
                await scope.CommitAsync(cancellationToken);
            }
        }

        public async Task AddAsync(IEnumerable<T> entity, CancellationToken cancellationToken = default)
        {

            using (var scope = unitOfWork.BeginScope())
            {
                foreach (var item in entity)
                {
                    await _dbSet.AddAsync(item, cancellationToken);
                }

                await scope.CommitAsync(cancellationToken);
            }
        }

        public Task UpdateAsync(
        T entity,
        CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);

            return Task.CompletedTask;
        }

        public Task UpdateRangeAsync(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default)
        {
            _dbSet.UpdateRange(entities);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            T entity,
            CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);

            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default)
        {
            _dbSet.RemoveRange(entities);

            return Task.CompletedTask;
        }

        public async Task DeleteAsync(
            Expression<Func<T, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            var entities = await _dbSet
                .Where(filter)
                .ToListAsync(cancellationToken);

            _dbSet.RemoveRange(entities);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace NosBarrage.Database.Services
{
    public class DatabaseService<TEntity> : IDatabaseService<TEntity> where TEntity : class
    {
        private readonly NosBarrageContext _context;

        public DatabaseService(NosBarrageContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _context.Set<TEntity>().ToListAsync();
        }

        public async Task<TEntity> GetByPropertiesAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var entity = await _context.Set<TEntity>().SingleOrDefaultAsync(predicate);
            if (entity == null)
                return null;

            return entity;
        }

        public async Task<TEntity> GetAsync(int id)
        {
            var entity = await _context.Set<TEntity>().FindAsync(id);
            if (entity == null)
                return null;

            return entity;
        }

        public async Task AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}

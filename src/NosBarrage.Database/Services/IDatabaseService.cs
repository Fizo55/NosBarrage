using System.Linq.Expressions;

namespace NosBarrage.Database.Services;

public interface IDatabaseService<TEntity> where TEntity : class
{
    Task<TEntity> GetAsync(int id);
    Task<TEntity> GetByPropertiesAsync(Expression<Func<TEntity, bool>> predicate);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task AddAsync(TEntity entity);
    Task RemoveAsync(TEntity entity);
}

namespace NosBarrage.Database.Services;

public interface IDatabaseService<TEntity> where TEntity : class
{
    Task<TEntity> GetAsync(int id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task AddAsync(TEntity entity);
    Task RemoveAsync(TEntity entity);
}

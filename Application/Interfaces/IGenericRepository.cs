using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Interfaces;

public interface IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TEntity, TId>
{
    Task<TEntity> Add(TEntity entity);
    Task<TEntity> FirstOrDefault(Expression<Func<TEntity, bool>> predicate, bool tracking = true, params string[] includeProperties);
    Task<List<TEntity>> GetAll(bool tracking = true, params string[] includeProperties);
    Task<TEntity> GetById(TId id, bool tracking = true, params string[] includeProperties);
    Task<IEnumerable<TEntity>> GetWhere(Expression<Func<TEntity, bool>> predicate,
                                  Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
                                  int? top = null,
                                  int? skip = null,
                                  params string[] includeProperties);
    Task Remove(TId id);
    Task Remove(TEntity entity);
    Task<TEntity> Update(TEntity entity);
}

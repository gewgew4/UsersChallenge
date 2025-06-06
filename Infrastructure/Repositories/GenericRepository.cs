using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public class GenericRepository<TEntity, TId>(ApplicationDbContext context) : IGenericRepository<TEntity, TId>
    where TEntity : BaseEntity<TEntity, TId>
{
    protected readonly ApplicationDbContext _context = context;
    protected readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task<TEntity> Add(TEntity entity)
    {
        var result = await _dbSet.AddAsync(entity);
        return result.Entity;
    }

    public async Task<TEntity?> FirstOrDefault(Expression<Func<TEntity, bool>> predicate, bool tracking = true, params string[] includeProperties)
    {
        IQueryable<TEntity> query = _dbSet;

        if (!tracking)
            query = query.AsNoTracking();

        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }

        return await query.FirstOrDefaultAsync(predicate);
    }

    public IEnumerable<TEntity> GetAll(bool tracking = true, params string[] includeProperties)
    {
        IQueryable<TEntity> query = _dbSet;

        if (!tracking)
            query = query.AsNoTracking();

        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }

        return query;
    }

    public async Task<TEntity?> GetById(TId id, bool tracking = true, params string[] includeProperties)
    {
        IQueryable<TEntity> query = _dbSet;

        if (!tracking)
            query = query.AsNoTracking();

        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }

        return await query.FirstOrDefaultAsync(e => e.Id != null && e.Id.Equals(id));
    }

    public async Task<IEnumerable<TEntity>?> GetWhere(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? top = null,
        int? skip = null,
        params string[] includeProperties)
    {
        IQueryable<TEntity> query = _dbSet;

        query = query.Where(predicate);

        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }

        if (orderBy != null)
            query = orderBy(query);

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (top.HasValue)
            query = query.Take(top.Value);

        return await query.ToListAsync();
    }

    public async Task Remove(TId id)
    {
        var entity = await GetById(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public async Task Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task<TEntity> Update(TEntity entity)
    {
        _dbSet.Update(entity);
        return await Task.FromResult(entity);
    }
}
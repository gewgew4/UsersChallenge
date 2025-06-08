namespace Domain.Entities;

public abstract class BaseEntity<TEntity, TId> where TEntity : class
{
    public required TId Id { get; set; }
}
using BidGuard.Domain.Common;
using BidGuard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Infrastructure.Persistence.Repositories;

public class Repository<T>(BidGuardDbContext db) : IRepository<T> where T : BaseEntity
{
    protected BidGuardDbContext Db { get; } = db;
    protected DbSet<T> Set => Db.Set<T>();

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) => Set.AddRangeAsync(entities, ct);

    public void Remove(T entity) => Set.Remove(entity);

    public void RemoveRange(IEnumerable<T> entities) => Set.RemoveRange(entities);
}

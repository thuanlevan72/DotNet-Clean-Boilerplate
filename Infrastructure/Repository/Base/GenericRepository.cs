using Domain.Common.Pagination;
using Domain.Entities.Base;
using Domain.Repositories;
using Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Postgres.Repository;

public class GenericRepository<TEntity, TId> : IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TId>
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id! }, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }


    public virtual async Task<List<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> expression, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        if (!trackChanges)
        {
            return await _dbSet.Where(expression).AsNoTracking().ToListAsync();
        }

        return await _dbSet.Where(expression).ToListAsync();
    }

    public virtual async Task<PagedResponse<TEntity>> GetPagedAsync(
        PaginationRequest request,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        // 1. Đếm tổng số bản ghi
        var totalItems = await _dbSet.CountAsync(cancellationToken);

        // 2. Lấy dữ liệu phân trang
        var items = await _dbSet
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 3. Trả về Response bọc lại
        return new PagedResponse<TEntity>(items, totalItems, request.PageNumber, request.PageSize);
    }

    public virtual async Task<PagedResponse<TEntity>> GetPagedByConditionAsync(
        Expression<Func<TEntity, bool>> expression,
        PaginationRequest request,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(expression);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        // 1. Đếm tổng số bản ghi (Đã lọc theo điều kiện)
        var totalItems = await query.CountAsync(cancellationToken);

        // 2. Lấy dữ liệu phân trang (Đã lọc theo điều kiện)
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // 3. Trả về
        return new PagedResponse<TEntity>(items, totalItems, request.PageNumber, request.PageSize);
    }

    public virtual void Add(TEntity entity) => _dbSet.Add(entity);
    public virtual void AddRange(IEnumerable<TEntity> entities) => _dbSet.AddRange(entities);
    public virtual void Update(TEntity entity) => _dbSet.Update(entity);
    public virtual void UpdateRange(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);
    public virtual void Delete(TEntity entity) => _dbSet.Remove(entity);
    public virtual void DeleteRange(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);
}
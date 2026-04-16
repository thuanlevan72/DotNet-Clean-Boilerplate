using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Postgres.Repository;

public class TagRepository : GenericRepository<Tag, int>, ITagRepository
{
    public TagRepository(AppDbContext context) : base(context) { }

    public async Task<Tag?> GetByIdAsync(int id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    public async Task<List<Tag>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Tag>> GetTagsByUserIdAsync(Guid userId, List<int> tagIdS, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.UserId == userId &&  !t.IsDeleted && tagIdS.Contains(t.Id)).OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }
}
using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// ITagRepository: Specialized repository for Tag entity
/// 
/// Mô tả:
/// - Extend IGenericRepository<Tag, int> (note: int primary key, not Guid)
/// - Add domain-specific queries for tags
/// - Enforce security: Filter by UserId always
/// - Manage many-to-many relationship with TodoItems
/// 
/// Why int primary key:
/// - Tags are relatively static (users create few tags)
/// - int is more efficient than Guid for foreign keys
/// - Smaller storage, faster comparisons
/// - Sequential IDs easier to understand in queries
/// 
/// Security:
/// - All queries filter by UserId
/// - Prevent user A accessing user B's tags
/// - Null return = unauthorized or not found
/// 
/// Implementation:
/// - Infrastructure.Postgres/Repositories/TagRepository.cs
/// - Uses EF Core DbContext
/// - Manages join table TodoItemTags
/// </summary>
public interface ITagRepository : IGenericRepository<Tag, int>
{
    /// <summary>
    /// Lấy tag theo ID + UserId (Security-checked)
    /// 
    /// Mục đích:
    /// - Get single tag with user verification
    /// - Ensure tag belongs to current user
    /// - Prevent cross-user tag access
    /// 
    /// Tham số:
    /// - id: Tag ID (int, not Guid)
    /// - userId: Current user ID (authorization)
    /// - cancellationToken: Async cancellation
    /// 
    /// Trả về:
    /// - Tag nếu found AND belongs to user
    /// - Null nếu not found hoặc different user's tag
    /// 
    /// SQL (pseudo):
    /// SELECT * FROM Tags 
    /// WHERE Id = @id 
    ///   AND UserId = @userId 
    ///   AND IsDeleted = false
    /// 
    /// Ví dụ:
    /// var tag = await _tagRepository.GetByIdAsync(42, userId);
    /// if (tag == null) 
    ///     throw new NotFoundException("Tag not found");
    /// // Safe to use: verified user owns this tag
    /// </summary>
    Task<Tag?> GetByIdAsync(int id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả tags của user
    /// 
    /// Mục đích:
    /// - Query all user's tags
    /// - Filter by UserId
    /// - Exclude soft-deleted
    /// - Populate tag list ở UI
    /// 
    /// Tham số:
    /// - userId: User ID để filter
    /// - cancellationToken: Async cancellation
    /// 
    /// Trả về:
    /// - List<Tag>: All active tags for user
    /// - Empty list nếu user chưa create tags
    /// 
    /// SQL (pseudo):
    /// SELECT * FROM Tags 
    /// WHERE UserId = @userId 
    ///   AND IsDeleted = false
    /// ORDER BY Name ASC
    /// 
    /// Ví dụ:
    /// var tags = await _tagRepository.GetAllByUserIdAsync(userId);
    /// 
    /// // Response:
    /// // [
    /// //   { id: 1, name: "urgent" },
    /// //   { id: 2, name: "backend" },
    /// //   { id: 3, name: "client-facing" }
    /// // ]
    /// 
    /// UI usage:
    /// - Display tag list ở sidebar
    /// - Show tag filter chips
    /// - Multi-select tags khi creating todo
    /// - Tag autocomplete ở search
    /// 
    /// Performance:
    /// - Usually small (users create 10-50 tags)
    /// - No pagination needed typically
    /// - Can cache result (60-300 seconds TTL)
    /// 
    /// Ordering:
    /// - ORDER BY Name: Alphabetical for easy selection
    /// - Can also: ORDER BY CreatedAt DESC (recently created first)
    /// </summary>
    Task<List<Tag>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);



    Task<List<Tag>> GetTagsByUserIdAsync(Guid userId, List<int> tagIdS, CancellationToken cancellationToken = default);
}
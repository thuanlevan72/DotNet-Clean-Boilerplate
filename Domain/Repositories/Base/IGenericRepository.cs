using Domain.Common.Pagination;
using Domain.Entities.Base;
using System.Linq.Expressions;

namespace Domain.Repositories;

/// <summary>
/// IGenericRepository: Generic repository interface cho tất cả entities
/// 
/// Mô tả:
/// - Định nghĩa CRUD operations cho entity bất kỳ
/// - Generic với TEntity (entity type) và TId (id type)
/// - Constraint: TEntity phải kế thừa từ BaseEntity<TId>
/// - Dùng làm template base cho repository implementations
/// 
/// Pattern:
/// - Repository Pattern: Abstraction layer giữa Domain và Database
/// - Dependency Inversion: Domain không phụ thuộc vào EF Core
/// - Testability: Dễ mock trong unit tests
/// 
/// Cách sử dụng:
/// - Create concrete repository: ITodoItemRepository : IGenericRepository<TodoItem, Guid>
/// - Inject vào services: constructor(IGenericRepository<TodoItem, Guid> repository)
/// - Gọi methods: await repository.GetByIdAsync(id)
/// 
/// Note:
/// - Không gọi SaveAsync ở đây (dùng IUnitOfWork.SaveAsync)
/// - Repository chỉ track changes, unit of work commit tất cả changes
/// </summary>
public interface IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TId>
{
    /// <summary>
    /// Lấy entity theo ID
    /// 
    /// Mục đích:
    /// - Truy vấn một entity duy nhất dựa trên primary key
    /// - Phổ biến nhất: GetByIdAsync(Guid id)
    /// 
    /// Tham số:
    /// - id: Primary key của entity
    /// - cancellationToken: Cho phép hủy request khi cần
    /// 
    /// Trả về:
    /// - Entity nếu tìm thấy
    /// - Null nếu không tìm thấy
    /// 
    /// Ví dụ:
    /// var todo = await repository.GetByIdAsync(Guid.Parse("123..."));
    /// if (todo == null) throw new NotFoundException("Todo not found");
    /// </summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả entities
    /// 
    /// Mục đích:
    /// - Truy vấn toàn bộ entities từ database
    /// - Thường kết hợp với Where, OrderBy trong Application layer
    /// 
    /// Tham số:
    /// - cancellationToken: Cho phép hủy request
    /// 
    /// Trả về:
    /// - List<TEntity> (có thể rỗng nếu không có data)
    /// - Chứa tất cả entities, kể cả soft-deleted (filter ở application layer)
    /// 
    /// Warning:
    /// - Có thể slow nếu table có hàng triệu records
    /// - Nên thêm filtering logic ở application layer
    /// 
    /// Ví dụ:
    /// var todos = await repository.GetAllAsync();
    /// var activeTodos = todos.Where(x => !x.IsDeleted).ToList();
    /// </summary>
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách entities có phân trang (Dùng class Request/Response)
    /// </summary>
    Task<PagedResponse<TEntity>> GetPagedAsync(
        PaginationRequest request,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách entities có phân trang và điều kiện (Dùng class Request/Response)
    /// </summary>
    Task<PagedResponse<TEntity>> GetPagedByConditionAsync(
        Expression<Func<TEntity, bool>> expression,
        PaginationRequest request,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<TEntity>> GetPagedByConditionAsync(
        Expression<Func<TEntity, bool>> expression,
    PaginationRequest request,
    bool trackChanges = false,
    CancellationToken cancellationToken = default,
    params Expression<Func<TEntity, object>>[]? includes);


    Task<PagedResponse<TResult>> GetPagedByConditionAsync<TResult>(
    Expression<Func<TEntity, bool>> expression,
    Expression<Func<TEntity, TResult>> selector,
    PaginationRequest request,
    bool trackChanges = false,
    CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> expression, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm mới entity
    /// 
    /// Mục đích:
    /// - Mark entity để insert vào database
    /// - Không commit ngay, chỉ add vào change tracker
    /// 
    /// Tham số:
    /// - entity: Entity object cần insert
    /// 
    /// Cơ chế:
    /// - EF Core change tracker sẽ đánh dấu entity là Added
    /// - Khi SaveAsync được gọi → INSERT SQL statement sẽ execute
    /// 
    /// Ví dụ:
    /// var newTodo = new TodoItem { Title = "New task" };
    /// repository.Add(newTodo);
    /// await unitOfWork.SaveAsync();
    /// </summary>
    void Add(TEntity entity);

    /// <summary>
    /// Thêm mới nhiều entities
    /// 
    /// Mục đích:
    /// - Bulk insert - thêm nhiều entities cùng lúc
    /// - Hiệu năng tốt hơn Add() gọi nhiều lần
    /// 
    /// Tham số:
    /// - entities: Collection của entities cần insert
    /// 
    /// Ví dụ:
    /// var tags = new[] { "urgent", "backend", "client-facing" }
    ///     .Select(name => new Tag { Name = name })
    ///     .ToList();
    /// repository.AddRange(tags);
    /// await unitOfWork.SaveAsync();
    /// </summary>
    void AddRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Cập nhật entity
    /// 
    /// Mục đích:
    /// - Mark entity để update trong database
    /// - Entity phải có ID (primary key) để identify record cần update
    /// 
    /// Tham số:
    /// - entity: Entity object đã được modify
    /// 
    /// Cơ chế:
    /// - EF Core change tracker đánh dấu entity là Modified
    /// - So sánh current values vs. original values
    /// - Khi SaveAsync → UPDATE SQL statement chỉ update changed columns
    /// 
    /// Ví dụ:
    /// var todo = await repository.GetByIdAsync(id);
    /// todo.Title = "Updated title";
    /// todo.Status = TodoStatus.InProgress;
    /// repository.Update(todo);
    /// await unitOfWork.SaveAsync();
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Cập nhật nhiều entities
    /// 
    /// Mục đích:
    /// - Bulk update - cập nhật nhiều entities cùng lúc
    /// - Ví dụ: Mark multiple todos as complete
    /// 
    /// Tham số:
    /// - entities: Collection của entities cần update
    /// 
    /// Ví dụ:
    /// var todosToComplete = todos.Where(x => x.DueDate < DateTime.Now).ToList();
    /// todosToComplete.ForEach(x => x.Status = TodoStatus.Completed);
    /// repository.UpdateRange(todosToComplete);
    /// await unitOfWork.SaveAsync();
    /// </summary>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Xóa entity (Soft Delete)
    /// 
    /// Mục đích:
    /// - Mark entity để soft delete
    /// - Soft delete: không xóa vật lý, chỉ set IsDeleted = true
    /// - Giữ lại data để audit/restore sau
    /// 
    /// Tham số:
    /// - entity: Entity object cần xóa
    /// 
    /// Cơ chế:
    /// - Repository implementation sẽ set:
    ///   + IsDeleted = true
    ///   + DeletedAt = DateTimeOffset.UtcNow
    ///   + DeletedBy = currentUserId
    /// - Khi SaveAsync → UPDATE SQL statement (không DELETE)
    /// 
    /// Ví dụ:
    /// var todo = await repository.GetByIdAsync(id);
    /// repository.Delete(todo);
    /// await unitOfWork.SaveAsync();
    /// // Sau đó: SELECT WHERE !IsDeleted sẽ không trả về todo này
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>
    /// Xóa nhiều entities (Soft Delete)
    /// 
    /// Mục đích:
    /// - Bulk soft delete - xóa nhiều entities cùng lúc
    /// - Ví dụ: Delete all old completed todos
    /// 
    /// Tham số:
    /// - entities: Collection của entities cần xóa
    /// 
    /// Ví dụ:
    /// var oldCompletedTodos = todos.Where(x => 
    ///     x.Status == TodoStatus.Completed &&
    ///     x.CompletedAt < DateTime.Now.AddMonths(-6)).ToList();
    /// repository.DeleteRange(oldCompletedTodos);
    /// await unitOfWork.SaveAsync();
    /// </summary>
    void DeleteRange(IEnumerable<TEntity> entities);
}
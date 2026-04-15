using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

namespace Application.Features.TodoItems.Commands;

/// <summary>
/// CreateTodoCommand: CQRS Command để tạo mới một Todo
/// 
/// Mô tả:
/// - Định nghĩa input data cần thiết để tạo todo
/// - Implement IRequest<Guid> từ MediatR
/// - Record type để immutable và simple value object
/// - MediatR pipeline sẽ route đến CreateTodoCommandHandler
/// 
/// Flow:
/// 1. API Controller nhận POST /api/todos request
/// 2. Controller parse body thành CreateTodoCommand
/// 3. Controller gọi mediator.Send(command)
/// 4. MediatR pipeline: Validate → Authorize → Execute handler → Return result
/// 5. Handler tạo TodoItem → SaveChanges → Return todo.Id
/// 
/// Validation:
/// - Được handle bởi FluentValidation validator (tự động qua pipeline)
/// - Title: Required, 1-200 chars
/// - Priority: Valid enum value
/// - DueDate: >= today
/// - CategoryId: Must belong to current user (checked in handler)
/// </summary>
public record CreateTodoCommand(
    /// <summary>Tiêu đề công việc (bắt buộc)</summary>
    string Title,

    /// <summary>Mô tả chi tiết (tùy chọn)</summary>
    string? Description,

    /// <summary>Mức độ ưu tiên (Low/Medium/High/Urgent)</summary>
    PriorityLevel Priority,

    List<int>? Tags,

    /// <summary>Thời hạn hoàn thành (tùy chọn)</summary>
    DateTimeOffset? DueDate,

    /// <summary>Danh mục của todo (tùy chọn, phải thuộc của current user)</summary>
    Guid? CategoryId) : IRequest<Guid>;

/// <summary>
/// CreateTodoCommandHandler: Handler xử lý CreateTodoCommand
/// 
/// Mô tả:
/// - Implement IRequestHandler<CreateTodoCommand, Guid>
/// - Execute business logic để tạo todo
/// - Validate dữ liệu (category ownership, authorization)
/// - Sử dụng Unit of Work pattern để coordinate database operations
/// - Dùng distributed lock để prevent double-submission (click button quá nhanh)
/// 
/// Dependencies:
/// - ITodoItemRepository: Thêm/sửa todo
/// - ICategoryRepository: Kiểm tra category tồn tại
/// - IUnitOfWork: Manage transaction, SaveChanges
/// - ICurrentUserService: Lấy current user ID
/// - IDistributedLockService: Lock operation trong 5 giây
/// 
/// Security:
/// - Check user authenticated (throw UnauthorizedAccessException)
/// - Check category belongs to current user
/// - Tự động set UserId = currentUser (không cho user set UserId khác)
/// 
/// Error Handling:
/// - Transaction rollback nếu có error
/// - Double-click prevention: acquire lock hoặc throw exception
/// - Category not found: throw exception
/// </summary>
public class CreateTodoCommandHandler : IRequestHandler<CreateTodoCommand, Guid>
{
    private readonly ITodoItemRepository _todoRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDistributedLockService _lockService;
    private readonly ITagRepository _tagRepository;

    /// <summary>
    /// Constructor - Dependency Injection
    /// 
    /// Tham số:
    /// - todoRepository: Query/modify todos
    /// - categoryRepository: Query/modify categories
    /// - unitOfWork: Commit/rollback transaction
    /// - currentUserService: Lấy current user info từ JWT token
    /// - lockService: Redis-based distributed lock
    /// </summary>
    public CreateTodoCommandHandler(
        ITodoItemRepository todoRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDistributedLockService lockService,
        ITagRepository tagRepository)
    {
        _todoRepository = todoRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _lockService = lockService;
        _tagRepository = tagRepository;
    }

    /// <summary>
    /// Handle: Xử lý CreateTodoCommand request
    /// 
    /// Tham số:
    /// - request: CreateTodoCommand object chứa todo data
    /// - cancellationToken: Cho phép hủy operation (async cancellation)
    /// 
    /// Trả về:
    /// - Guid: ID của todo vừa tạo (client sử dụng để fetch/update todo)
    /// 
    /// Logic:
    /// 1. Kiểm tra user authenticated
    /// 2. Acquire distributed lock (prevent double-submit)
    /// 3. Begin transaction
    /// 4. Nếu có CategoryId: Kiểm tra category tồn tại và thuộc user
    /// 5. Tạo TodoItem entity
    /// 6. Add vào repository
    /// 7. Commit transaction
    /// 8. Rollback nếu exception
    /// 9. Return todo.Id
    /// 
    /// Exception:
    /// - UnauthorizedAccessException: User chưa đăng nhập
    /// - Exception "Bạn đang thao tác quá nhanh": Double-click detected
    /// - Exception "Danh mục không tồn tại": CategoryId không hợp lệ
    /// - DbUpdateException: Database error
    /// </summary>
    public async Task<Guid> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        /// <summary>Bước 1: Kiểm tra authentication</summary>
        if (!_currentUserService.IsAuthenticated)
            throw new UnauthorizedAccessException("Bạn chưa đăng nhập.");

        var userId = _currentUserService.UserId;

        /// <summary>
        /// Bước 2: Acquire distributed lock
        /// 
        /// Mục đích: Prevent double-submission attack
        /// - Nếu user click create button 2 lần quá nhanh (< 5 giây)
        /// - Request thứ 1 acquire lock → create todo
        /// - Request thứ 2 không thể acquire lock → throw exception
        /// - Lock expire sau 5 giây để user có thể thử lại
        /// 
        /// Lock key: $"create_todo_{userId}"
        /// - Unique per user (user khác có thể create todo cùng lúc)
        /// - Prevent multiple creates from same user within 5 seconds
        /// </summary>
        var lockToken = Guid.NewGuid().ToString();
        bool isLocked = await _lockService.AcquireLockAsync(
            $"create_todo_{userId}", 
            lockToken, 
            TimeSpan.FromSeconds(5));

        if (!isLocked) 
            throw new Exception("Bạn đang thao tác quá nhanh, vui lòng chờ!");

        /// <summary>Bước 3: Bắt đầu transaction</summary>
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            /// <summary>
            /// Bước 4: Validate category (nếu có)
            /// 
            /// Kiểm tra:
            /// - CategoryId phải tồn tại
            /// - CategoryId phải thuộc sở hữu của current user
            /// - Security: Prevent user B modify category của user A
            /// </summary>
            if (request.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(
                    request.CategoryId.Value, 
                    userId, 
                    cancellationToken);

                if (category == null) 
                    throw new Exception("Danh mục không tồn tại.");
            }


            /// <summary>
            /// Bước 5: Tạo TodoItem entity
            /// 
            /// Gán giá trị:
            /// - UserId: Tự động từ current user (không lấy từ request)
            /// - Title, Description, Priority, DueDate, CategoryId: Từ request
            /// - Status: Hardcode = Todo (mới tạo là chưa bắt đầu)
            /// - Audit fields (CreatedAt, CreatedBy): EF Core sẽ set tự động
            /// - Id: EF Core sẽ generate Guid mới
            /// </summary>
            var todo = new TodoItem
            {
                UserId = userId,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = TodoStatus.Todo,
                DueDate = request.DueDate,
                CategoryId = request.CategoryId,
               

            };

            // truy vấn tag nếu có 

            if (request.Tags?.Any() == true)
            {
                var tags = await _tagRepository.GetByConditionAsync(x=> request.Tags.Contains(x.Id), true,
                    cancellationToken);

                if (tags?.Any() == true)
                    todo.Tags = tags;
            }
           

            /// <summary>Bước 6: Add todo vào repository (change tracker)</summary>
            _todoRepository.Add(todo);

            /// <summary>
            /// Bước 7: Commit transaction
            /// 
            /// Thực thi:
            /// - SaveChangesAsync() ghi todo vào database
            /// - CommitTransactionAsync() commit transaction
            /// - Database sẽ generate auto-increment ID
            /// - Entity sẽ được refresh với ID vừa generate
            /// </summary>
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            /// <summary>Bước 8: Return todo ID cho client</summary>
            return todo.Id;
        }
        catch
        {
            /// <summary>
            /// Error handling: Rollback transaction
            /// 
            /// Nếu có exception ở bất kỳ bước nào:
            /// - RollbackTransactionAsync() undo tất cả changes
            /// - Todo không được insert vào database
            /// - Database quay lại state trước transaction bắt đầu
            /// </summary>
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }    
}

using Application.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Common.Pagination;
using Domain.Entities;
using Domain.Repositories;
using Domain.Shared;
using MediatR;

namespace Application.Features.TodoItems.Queries;

/// <summary>
/// GetMyTodosQuery: CQRS Query để lấy tất cả todos của current user
/// 
/// Mô tả:
/// - Không có input parameter (dùng current user từ JWT token)
/// - Trả về List<TodoDto> chứa tất cả todos
/// - Read-only operation (không modify database)
/// - Implement IRequest<List<TodoDto>> từ MediatR
/// 
/// Flow:
/// 1. API Controller nhận GET /api/todos request
/// 2. Controller tạo GetMyTodosQuery()
/// 3. Controller gọi mediator.Send(query)
/// 4. MediatR pipeline route đến GetMyTodosQueryHandler
/// 5. Handler query database → map to DTOs → return list
/// 
/// Caching:
/// - Có thể thêm caching ở pipeline hoặc handler
/// - Key: $"todos_{userId}"
/// - TTL: 5 phút (short cache vì todos thay đổi thường xuyên)
/// 
/// Filtering & Sorting:
/// - Cơ bản: Lấy tất cả todos
/// - Advanced: Có thể thêm optional parameters (filterByStatus, sortBy, etc.)
/// </summary>
public record GetMyTodosQuery(PaginationRequest PaginationRequest) : IRequest<PagedResponse<TodoDto>>;

/// <summary>
/// GetMyTodosQueryHandler: Handler xử lý GetMyTodosQuery
/// 
/// Mô tả:
/// - Implement IRequestHandler<GetMyTodosQuery, List<TodoDto>>
/// - Query database để lấy todos của current user
/// - Map domain entities sang DTOs (remove sensitive info)
/// - Return list để trả về client
/// 
/// Dependencies:
/// - ITodoItemRepository: Query todos từ database
/// - ICurrentUserService: Lấy current user ID (từ JWT token)
/// - IMapper: AutoMapper để convert TodoItem → TodoDto
/// 
/// Performance:
/// - Query: SELECT * FROM TodoItems WHERE UserId = ? AND !IsDeleted
/// - Memory: Load tất cả todos vào memory (cân nhắc pagination nếu quá nhiều)
/// - Mapping: AutoMapper sẽ map properties động
/// </summary>
public class GetMyTodosQueryHandler : IRequestHandler<GetMyTodosQuery, PagedResponse<TodoDto>>
{
    /// <summary>Repository để query todos từ database</summary>
    private readonly ITodoItemRepository _todoRepository;

    /// <summary>Service để lấy current user info (UserId, IsAuthenticated, etc.)</summary>
    private readonly ICurrentUserService _currentUserService;

    /// <summary>AutoMapper instance để convert TodoItem ↔ TodoDto</summary>
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor - Dependency Injection
    /// 
    /// Tham số:
    /// - todoRepository: Query todos
    /// - currentUserService: Lấy current user ID
    /// - mapper: Convert to DTO
    /// </summary>
    public GetMyTodosQueryHandler(
        ITodoItemRepository todoRepository, 
        ICurrentUserService currentUserService, 
        IMapper mapper)
    {
        _todoRepository = todoRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    /// <summary>
    /// Handle: Xử lý GetMyTodosQuery request
    /// 
    /// Tham số:
    /// - request: GetMyTodosQuery (không có input)
    /// - cancellationToken: Cho phép hủy operation
    /// 
    /// Trả về:
    /// - List<TodoDto>: Danh sách todos đã convert sang DTO
    /// 
    /// Logic:
    /// 1. Lấy current user ID từ service
    /// 2. Query repository để lấy tất cả todos của user
    /// 3. Map TodoItem entities → TodoDto objects
    /// 4. Return list
    /// 
    /// Ví dụ trả về:
    /// [
    ///   { id: "123", title: "Buy milk", priority: "High", status: "Todo" },
    ///   { id: "456", title: "Fix bug", priority: "Urgent", status: "InProgress" },
    /// ]
    /// 
    /// Notes:
    /// - GetAllByUserIdAsync tự động filter IsDeleted = false
    /// - AutoMapper sẽ map properties có cùng tên
    /// - DTO sẽ không chứa sensitive info (password, tokens, etc.)
    /// </summary>
    //public async Task<List<TodoDto>> Handle(
    //    GetMyTodosQuery request, 
    //    CancellationToken cancellationToken)
    //{
    //    /// <summary>
    //    /// Bước 1: Lấy current user ID
    //    /// - Từ JWT token decoded ở middleware
    //    /// - Đảm bảo chỉ lấy todos của user này
    //    /// </summary>
    //    var userId = _currentUserService.UserId;

    //    /// <summary>
    //    /// Bước 2: Query repository
    //    /// - GetAllByUserIdAsync: Custom method để query todos by user
    //    /// - Tự động filter: WHERE UserId = ? AND IsDeleted = false
    //    /// - Return List<TodoItem> từ database
    //    /// </summary>
    //    var todos = await _todoRepository.GetPagedAsync(request.PaginationRequest, false, cancellationToken);

    //    /// <summary>
    //    /// Bước 3: Map entities → DTOs
    //    /// - AutoMapper sẽ tự động map:
    //    ///   + TodoItem.Id → TodoDto.Id
    //    ///   + TodoItem.Title → TodoDto.Title
    //    ///   + TodoItem.Priority → TodoDto.Priority
    //    ///   + etc.
    //    /// - Lợi ích: Convert complex objects thành simpler DTOs
    //    /// - Security: Không expose internal fields (password, tokens, etc.)
    //    /// </summary>
    //    return _mapper.Map<List<TodoDto>>(todos);
    //}

    public async Task<PagedResponse<TodoDto>> Handle(GetMyTodosQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        // Tự động Build Expression (x => x.Status == 1 && x.Priority == 3 ...)
        // Gọn gàng và an toàn hơn
        var dynamicExpression = DynamicFilterBuilder.Build<TodoItem>(request?.PaginationRequest?.DynamicFilters);
        // 1. Gọi Repository để lấy PagedResponse của ENTITY
        var pagedEntities = await _todoRepository.GetPagedByConditionAsync(x => x.UserId == userId, request.PaginationRequest, trackChanges: false, cancellationToken);

        // 2. Dùng AutoMapper để map danh sách Items (Từ List<TodoItem> sang List<TodoDto>)
        var mappedItems = _mapper.Map<List<TodoDto>>(pagedEntities.Items);

        // 3. Khởi tạo lại PagedResponse mới với kiểu DTO, giữ nguyên các thông số phân trang cũ
        var pagedResponseDto = new PagedResponse<TodoDto>(
            mappedItems,
            pagedEntities.TotalItems,
            pagedEntities.CurrentPage,
            pagedEntities.PageSize
        );

        return pagedResponseDto;
    }
}
namespace Application.Dtos;

/// <summary>
/// TodoDto: Data Transfer Object cho TodoItem
/// 
/// Mô tả:
/// - DTO là lightweight version của domain entity
/// - Dùng để transfer data giữa layers (API ↔ Client)
/// - Remove sensitive/internal fields (passwords, tokens, internal IDs)
/// - Flatten nested objects hoặc include calculated fields
/// 
/// Pattern:
/// - Record type: Immutable, simple value object
/// - Gọi từ API endpoints khi trả về response
/// - Map từ TodoItem entity bằng AutoMapper
/// 
/// So sánh TodoItem entity vs TodoDto:
/// 
/// TodoItem Entity:
/// - Id, Title, Description, Priority, Status
/// - UserId, CategoryId, ParentTaskId
/// - DueDate, CompletedAt, CreatedAt, UpdatedAt
/// - IsDeleted, DeletedAt, DeletedBy
/// - Category (navigation), Tags (navigation), SubTasks (navigation)
/// 
/// TodoDto (exposed to client):
/// - Id, Title, Description, Priority, Status
/// - DueDate, CreatedAt (audit info for client)
/// - CategoryName (instead of CategoryId, easier to display)
/// - Không expose: UserId, IsDeleted, UpdatedAt, navigation properties
/// 
/// Ưu điểm:
/// - API response nhỏ hơn (bandwidth efficient)
/// - Security: Không expose internal IDs hoặc admin fields
/// - Flexibility: Có thể change entity structure mà không ảnh hưởng client
/// - Readability: CategoryName dễ hiểu hơn CategoryId
/// </summary>
public record TodoDto(
    /// <summary>
    /// Định danh duy nhất của todo (Primary Key)
    /// - Type: Guid (globally unique)
    /// - Dùng: Client sử dụng để fetch/update/delete todo này
    /// - Ví dụ: "550e8400-e29b-41d4-a716-446655440000"
    /// </summary>
    Guid Id,

    /// <summary>
    /// Tiêu đề công việc
    /// - Type: string (required)
    /// - Hiển thị: Ở list, detail view, notifications
    /// - Ví dụ: "Buy milk", "Fix bug login", "Complete quarterly report"
    /// </summary>
    string Title,

    /// <summary>
    /// Mô tả chi tiết công việc
    /// - Type: string? (nullable, optional)
    /// - Hiển thị: Ở detail view khi user expand
    /// - Ví dụ: "Q4 report includes revenue, expenses, growth metrics"
    /// </summary>
    string? Description,

    /// <summary>
    /// Mức độ ưu tiên (dùng string thay enum)
    /// - Type: string (converted from PriorityLevel enum)
    /// - Lý do convert: Enum không serialize thường, string dễ hơn
    /// - Giá trị: "Low", "Medium", "High", "Urgent"
    /// - Hiển thị: Color-coded badges ở UI
    /// - Ví dụ: "High" sẽ hiển thị badge đỏ
    /// </summary>
    string Priority,

    /// <summary>
    /// Trạng thái hiện tại công việc
    /// - Type: string (converted from TodoStatus enum)
    /// - Giá trị: "Todo", "InProgress", "InReview", "Completed", "Cancelled"
    /// - Hiển thị: Status badge với icon phù hợp
    /// - Ví dụ: "InProgress" → green badge với loading spinner
    /// </summary>
    string Status,

    /// <summary>
    /// Thời hạn hoàn thành công việc
    /// - Type: DateTimeOffset? (nullable, optional)
    /// - Hiển thị: Date picker ở form, deadline badge ở list
    /// - Logic: Nếu DueDate < now → highlight red (overdue)
    /// - Ví dụ: 2024-12-31T17:00:00+07:00 (include timezone)
    /// </summary>
    DateTimeOffset? DueDate,

    /// <summary>
    /// Thời điểm tạo công việc (Audit)
    /// - Type: DateTimeOffset (required)
    /// - Hiển thị: "Created 2 days ago" ở detail view
    /// - Lưu ý: UTC timestamp, frontend convert to local timezone
    /// - Ví dụ: 2024-12-20T10:30:00+07:00
    /// </summary>
    DateTimeOffset CreatedAt,

    /// <summary>
    /// Tên danh mục công việc
    /// - Type: string? (nullable - todo có thể không có category)
    /// - Lý do dùng tên thay vì ID: Client không cần category ID
    /// - Hiển thị: Category badge/tag ở list, detail view
    /// - Ví dụ: "Work", "Personal", "Shopping"
    /// 
    /// Note về mapping (AutoMapper):
    /// - TodoItem.Category.Name → TodoDto.CategoryName
    /// - Nếu TodoItem.Category = null → TodoDto.CategoryName = null
    /// - AutoMapper tự động handle nested property navigation
    /// </summary>
    CategoryDto? Category,

    /// <summary>
    /// 
    /// </summary>
    List<TagDto>? Tags
);
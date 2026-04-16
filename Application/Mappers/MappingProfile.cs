using Application.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

/// <summary>
/// MappingProfile: AutoMapper configuration profile
/// 
/// Mô tả:
/// - Định nghĩa mapping rules giữa domain entities ↔ DTOs
/// - Kế thừa từ AutoMapper Profile
/// - Registered globally khi app startup
/// - Dùng ở Application handlers để convert entity → DTO
/// 
/// AutoMapper Pattern:
/// - Convention-based: Tự động map properties cùng tên
/// - Custom mapping: Explicit rules cho complex conversions
/// - Flattening: Map nested objects sang flat DTOs
/// - Enum to string: Convert enum → string cho JSON serialization
/// 
/// Flow:
/// 1. Define mapping profiles trong constructor
/// 2. Register profiles ở startup (DependencyInjection.cs)
/// 3. Inject IMapper vào handlers/services
/// 4. Call _mapper.Map<DTO>(entity)
/// 5. AutoMapper executes mapping based on profile rules
/// 
/// Lợi ích:
/// - Giảm boilerplate: Không cần manual property assignment
/// - Maintainability: Centralized mapping rules
/// - Flexibility: Easy to add custom rules
/// - Performance: Compiled mapping expressions
/// 
/// Example mapping rules:
/// - TodoItem.Title → TodoDto.Title (automatic)
/// - TodoItem.Priority → TodoDto.Priority.ToString() (custom)
/// - TodoItem.Category.Name → TodoDto.CategoryName (nested flattening)
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Constructor - Định nghĩa tất cả mapping rules
    /// 
    /// Khi app startup:
    /// 1. AutoMapper scan tất cả Profile classes
    /// 2. Gọi constructor để collect mapping rules
    /// 3. Compile mappings thành mapping expressions
    /// 4. Cache compiled expressions để reuse
    /// 5. Khi Map được gọi → execute pre-compiled expression (fast)
    /// </summary>
    public MappingProfile()
    {
        /// <summary>
        /// Mapping 1: TodoItem Entity → TodoDto DTO
        /// 
        /// Kích hoạt khi:
        /// - _mapper.Map<TodoDto>(todoItem)
        /// - _mapper.Map<List<TodoDto>>(todoItems)
        /// 
        /// Convention-based (Automatic):
        /// - TodoItem.Id → TodoDto.Id ✓
        /// - TodoItem.Title → TodoDto.Title ✓
        /// - TodoItem.Description → TodoDto.Description ✓
        /// - TodoItem.DueDate → TodoDto.DueDate ✓
        /// - TodoItem.CreatedAt → TodoDto.CreatedAt ✓
        /// 
        /// Property Ignored (Not in DTO):
        /// - UserId: Security, don't expose to client
        /// - CategoryId: Use CategoryName instead
        /// - ParentTaskId: Internal relation, not needed in DTO
        /// - SubTasks: Navigation, not included
        /// - Tags: Navigation, would need separate mapping
        /// - IsDeleted, DeletedAt: Internal audit fields
        /// 
        /// Custom Mappings (ForMember):
        /// </summary>
        CreateMap<TodoItem, TodoDto>()
            /// <summary>
            /// Custom rule 1: Convert enum to string
            /// 
            /// Source: TodoItem.Priority = PriorityLevel.High (enum)
            /// Destination: TodoDto.Priority = "High" (string)
            /// 
            /// Lý do:
            /// - JSON serializer handle string tốt hơn enum
            /// - Client side: String dễ display, parse trong template
            /// - Flexibility: Có thể change display format (uppercase, camelCase, etc.)
            /// 
            /// Enum values sau ToString():
            /// - PriorityLevel.None → "None"
            /// - PriorityLevel.Low → "Low"
            /// - PriorityLevel.Medium → "Medium"
            /// - PriorityLevel.High → "High"
            /// - PriorityLevel.Urgent → "Urgent"
            /// </summary>
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))

            /// <summary>
            /// Custom rule 2: Convert status enum to string
            /// 
            /// Source: TodoItem.Status = TodoStatus.InProgress (enum)
            /// Destination: TodoDto.Status = "InProgress" (string)
            /// 
            /// Enum values sau ToString():
            /// - TodoStatus.Todo → "Todo"
            /// - TodoStatus.InProgress → "InProgress"
            /// - TodoStatus.InReview → "InReview"
            /// - TodoStatus.Completed → "Completed"
            /// - TodoStatus.Cancelled → "Cancelled"
            /// </summary>
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category));

        /// <summary>
        /// Mapping 2: Category Entity → CategoryDto DTO
        /// 
        /// Kích hoạt khi:
        /// - _mapper.Map<CategoryDto>(category)
        /// 
        /// Convention-based (Automatic):
        /// - Category.Id → CategoryDto.Id ✓
        /// - Category.Name → CategoryDto.Name ✓
        /// - Category.ColorHex → CategoryDto.ColorHex ✓
        /// - Category.CreatedAt → CategoryDto.CreatedAt ✓
        /// 
        /// Ignored fields (not in DTO):
        /// - UserId: Internal, don't expose
        /// - TodoItems: Navigation, not needed
        /// - IsDeleted, audit fields: Internal
        /// 
        /// Note: Hoàn toàn convention-based, không cần custom rules
        /// </summary>
        CreateMap<Category, CategoryDto>();

        /// <summary>
        /// Mapping 3: Tag Entity → TagDto DTO
        /// 
        /// Kích hoạt khi:
        /// - _mapper.Map<TagDto>(tag)
        /// 
        /// Convention-based (Automatic):
        /// - Tag.Id → TagDto.Id ✓
        /// - Tag.Name → TagDto.Name ✓
        /// - Tag.CreatedAt → TagDto.CreatedAt ✓
        /// 
        /// Ignored fields (not in DTO):
        /// - UserId: Internal, don't expose
        /// - TodoItems: Navigation, would be heavy to include
        /// - IsDeleted, audit fields: Internal
        /// 
        /// Note: Hoàn toàn convention-based, không cần custom rules
        /// </summary>
        CreateMap<Tag, TagDto>();
    }
}
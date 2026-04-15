using Application.Features.TodoItems.Commands;
using Application.Features.TodoItems.Queries;
using Domain.Common.Pagination;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize] // Bắt buộc phải có token JWT hợp lệ mới được gọi API
[ApiController]
[Route("api/todos")]
public class TodoItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TodoItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTodos([FromQuery] GetMyTodosQuery request)
    {
        var result = await _mediator.Send(request);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTodoById(Guid id)
    {
        var result = await _mediator.Send(new GetTodoByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoCommand command)
    {
        var newTodoId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetTodoById), new { id = newTodoId }, new { Message = "Tạo công việc thành công", Id = newTodoId });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateTodoStatus(Guid id, [FromBody] UpdateTodoStatusRequest request)
    {
        // Gắn id từ Route vào Command
        var command = new UpdateTodoCommand(id, request.NewStatus);

        await _mediator.Send(command);
        return Ok(new { Message = "Cập nhật trạng thái thành công" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTodo(Guid id)
    {
        await _mediator.Send(new DeleteTodoCommand(id));
        return Ok(new { Message = "Xóa công việc thành công" });
    }
}

// Record phụ để map Body cho API Update Status
public record UpdateTodoStatusRequest(TodoStatus NewStatus);
using Application.Dtos;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Auth;

public record LoginCommand(string Email, string Password, string deviceId) : IRequest<AuthResultDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(request.Email, request.Password, request.deviceId);
    }
}
using Application.Dtos;
using Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Auth
{
    public class LogoutRequest 
    {
        public string ClientRefreshToken { get; set; }
        public bool LogoutAllDevices { get; set; }
    }
    public record LogoutCommand(LogoutRequest LogoutRequest) : IRequest<string>;
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, string>
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUserService;
        public LogoutCommandHandler(IAuthService authService, ICurrentUserService currentUserService)
        {
            _authService = authService;
            _currentUserService = currentUserService;
        }
        public async Task<string> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy UserId của người đang gọi API (Đảm bảo tính chính chủ)
            var currentUserId = _currentUserService.UserId;

            // 2. Xử lý 2 lựa chọn
            if (request.LogoutRequest.LogoutAllDevices)
            {
                // Lựa chọn 1: Đăng xuất mọi thiết bị
                await _authService.RevokeAllUserTokensAsync(currentUserId);
                return "Đã đăng xuất thành công khỏi TẤT CẢ các thiết bị.";
            }
            else
            {
                // Lựa chọn 2: Đăng xuất thiết bị hiện tại (Truyền kèm currentUserId để bảo mật)
                await _authService.RevokeSingleTokenAsync(request.LogoutRequest.ClientRefreshToken, currentUserId);
                return "Đã đăng xuất thành công khỏi thiết bị hiện tại.";
            }

        }
    }
}

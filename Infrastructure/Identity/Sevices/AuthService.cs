using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Postgres.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Postgres.Identity.Sevices;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService; // Thêm dòng này

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration, AppDbContext dbContext, ICacheService cacheService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<AuthResultDto> LoginAsync(string email, string password, string deviceId)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return new AuthResultDto { IsSuccess = false, Message = "Email hoặc mật khẩu không đúng." };
        }
        await _userManager.UpdateAsync(user);

        // define the login provider used to store tokens
        string loginProvider = "MyApp_" + deviceId;

        var token = await GenerateJwtTokenAsync(user, loginProvider);

        var refreshToken = GenerateRefreshToken();
        // sau 3 ngày sẽ xóa token, bắt buộc người dùng phải đăng nhập lại để lấy token mới
        var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(3);


        // 4. Kiểm tra xem thiết bị này đã từng login chưa (có token cũ trong DB không)
        var tokenInDb = await _dbContext.Set<ApplicationToken>()
            .FirstOrDefaultAsync(t => t.UserId == user.Id &&
                                      t.LoginProvider == loginProvider &&
                                      t.Name == "RefreshToken");

        if (tokenInDb != null)
        {
            tokenInDb.Value = refreshToken;
            tokenInDb.RefreshTokenExpiryTime = refreshTokenExpiryTime;
            _dbContext.Update(tokenInDb);
        }
        else
        {
            var appToken = new ApplicationToken
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                Name = "RefreshToken",
                Value = refreshToken,
                RefreshTokenExpiryTime = refreshTokenExpiryTime
            };
            await _dbContext.Set<ApplicationToken>().AddAsync(appToken);
        }

        await _dbContext.SaveChangesAsync();

        return new AuthResultDto { IsSuccess = true, Token = token, RefreshToken = refreshToken, Message = "Đăng nhập thành công" };
    }

    // ==========================================
    // 2. REFRESH TOKEN (Cấp lại token mới khi token cũ hết hạn)
    // ==========================================
    public async Task<AuthResultDto> RefreshTokenAsync(string clientRefreshToken)
    {
        // 1. Tìm Token trong DB dựa trên chuỗi Refresh Token gửi lên
        var savedToken = await _dbContext.Set<ApplicationToken>()
            .FirstOrDefaultAsync(t => t.Value == clientRefreshToken && t.Name == "RefreshToken");

        // 2. Kiểm tra tính hợp lệ
        if (savedToken == null)
            throw new Exception("Refresh Token không tồn tại hoặc đã bị thu hồi!");

        // 3. Kiểm tra hạn sử dụng (Dùng cột Custom của bạn)
        if (savedToken.RefreshTokenExpiryTime <= DateTimeOffset.UtcNow)
        {
            // Có thể xóa luôn token rác này trong DB
            _dbContext.Set<ApplicationToken>().Remove(savedToken);
            await _dbContext.SaveChangesAsync();

            throw new Exception("Refresh Token đã hết hạn. Vui lòng đăng nhập lại!");
        }

        // 4. Lấy thông tin User
        var user = await _userManager.FindByIdAsync(savedToken.UserId.ToString());
        if (user == null) throw new Exception("User không tồn tại!");

        // 5. Sinh cặp Token mới (Xoay vòng token - Token Rotation để bảo mật)

        string newRefreshToken = GenerateRefreshToken();

        // 6. Cập nhật lại Refresh Token mới vào chính dòng DB hiện tại (Giữ nguyên LoginProvider/Device)
        savedToken.Value = newRefreshToken;
        savedToken.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(7);

        string newAccessToken = await GenerateJwtTokenAsync(user, savedToken.LoginProvider);

        await _dbContext.SaveChangesAsync();

        return new AuthResultDto { IsSuccess = true, Token = newAccessToken, RefreshToken = newRefreshToken, Message = "Đã lấy lại token thành công" };
    }

    public async Task<AuthResultDto> RegisterAsync(string email, string password, string fullName)
    {
        var userExists = await _userManager.FindByEmailAsync(email);
        if (userExists != null)
            return new AuthResultDto { IsSuccess = false, Message = "Email đã tồn tại." };

        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            return new AuthResultDto { IsSuccess = false, Errors = result.Errors.Select(e => e.Description) };

        // ==========================================
        // THÊM MỚI: TỰ ĐỘNG GÁN QUYỀN "User" CHO TÀI KHOẢN MỚI
        // ==========================================
        await _userManager.AddToRoleAsync(user, "User");

        return new AuthResultDto { IsSuccess = true, Message = "Đăng ký thành công" };
    }

    // ==========================================
    // 3A. ĐĂNG XUẤT 1 THIẾT BỊ (Thu hồi 1 Token cụ thể)
    // ==========================================
    public async Task RevokeSingleTokenAsync(string clientRefreshToken, Guid userId)
    {
        var tokenToRevoke = await _dbContext.Set<ApplicationToken>()
            .FirstOrDefaultAsync(t => t.Value == clientRefreshToken && t.Name == "RefreshToken" && t.UserId == userId);

        if (tokenToRevoke != null)
        {
            var deviceId = tokenToRevoke.LoginProvider; // Cắt bỏ tiền tố để lấy đúng DeviceId

            // 2. Xóa Token trong Database (Postgres)
            _dbContext.Set<ApplicationToken>().Remove(tokenToRevoke);
            await _dbContext.SaveChangesAsync();

            // 3. XÓA CACHE TRONG REDIS (Chặn đứng Access Token ngay lập tức)
            await _cacheService.RemoveAsync($"Session:{userId}:{deviceId}");
        }
    }

    // ==========================================
    // 3B. ĐĂNG XUẤT ALL (Thu hồi toàn bộ Token của User)
    // ==========================================
    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        // Lấy tất cả các dòng Token thuộc về UserId này
        var allTokens = await _dbContext.Set<ApplicationToken>()
            .Where(t => t.UserId == userId && t.Name == "RefreshToken")
            .ToListAsync();

        if (allTokens.Any())
        {
            // 1. Xóa toàn bộ Token trong Database (Postgres)
            _dbContext.Set<ApplicationToken>().RemoveRange(allTokens);
            await _dbContext.SaveChangesAsync();

            // 2. Vòng lặp dọn dẹp sạch sẽ Redis Cache của TẤT CẢ thiết bị
            foreach (var token in allTokens)
            {
                var deviceId = token.LoginProvider;
                await _cacheService.RemoveAsync($"Session:{userId}:{deviceId}");
            }
        }
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, string deviceId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Lấy danh sách quyền của User từ Database
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("deviceId", deviceId)
        };

        // ==========================================
        // THÊM MỚI: NHÉT CÁC ROLE VÀO JWT TOKEN
        // ==========================================
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
using Api.Services;
using Application.Interfaces;
using Domain.Entities; // Thay bằng namespace chứa ApplicationUser của bạn
using Hangfire.Logging;
using Infrastructure.Postgres.Data;
using Infrastructure.Postgres.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

namespace Api.Extensions
{
    public static class ApiServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddControllers();
            services.AddEndpointsApiExplorer();

            // 1. Cấu hình CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            // 2. Cấu hình JWT
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]!)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // ==========================================
                // THÊM MỚI: ĐOẠN CODE KIỂM TRA TOKEN CHẶT CHẼ
                // ==========================================
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        // 1. Resolve các service cần thiết
                        var cacheService = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
                        var appContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();


                        // 2. Lấy UserId từ Token (Claim NameIdentifier)
                        var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        // 3. Lấy deviceId Từ clamis
                        var deviceId = context.Principal?.FindFirst("deviceId")?.Value;


                        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(deviceId))
                        {
                            context.Fail("Token không chứa thông tin User và Claim hợp lệ.");
                            return;
                        }
                        /// cách này không tốt vì nó call quá nhiều lần vào database
                        //// 4. Kiểm tra xem thiết bị này đã từng login chưa (có token cũ trong DB không)
                        //var tokenInDb = await appContext.Set<ApplicationToken>()
                        //    .FirstOrDefaultAsync(t => t.UserId == Guid.Parse(userId) &&
                        //                              t.LoginProvider == deviceId &&
                        //                              t.Name == "RefreshToken");

                        //// NẾU TÌM KHÔNG THẤY -> Có nghĩa là thiết bị này đã bị Đăng xuất hoặc bị "Đá"
                        //if (tokenInDb == null)
                        //{
                        //    context.Fail("Phiên đăng nhập đã hết hạn hoặc bị thu hồi trên thiết bị này.");
                        //    return;
                        //}

                        //// 3. Tìm User trong Database
                        //var user = await userManager.FindByIdAsync(userId);

                        //// 4. Kiểm tra: User có bị xóa, hoặc bị Admin khóa (IsActive = false) không?
                        //// (Dựa vào thuộc tính IsActive trong ApplicationUser của bạn)
                        //if (user == null || !user.IsActive)
                        //{
                        //    // Đánh dấu Token này là KHÔNG HỢP LỆ -> Trả về lỗi 401 Unauthorized ngay lập tức
                        //    context.Fail("Tài khoản không tồn tại hoặc đã bị khóa.");
                        //}
                        // ====================================================
                        // KIỂM TRA 1: SESSION THIẾT BỊ (Bọc qua Redis)
                        // ====================================================

                        string sessionCacheKey = $"Session:{userId}:{deviceId}";

                        // Tự động tìm trong Redis, nếu không có mới chạy hàm query DB
                        var isSessionValid = await cacheService.GetOrSetAsync(
                            sessionCacheKey,
                            factory: async (cToken) =>
                            {
                                var loginProvider = $"{deviceId}";
                                var tokenInDb = await appContext.Set<ApplicationToken>()
                                    .FirstOrDefaultAsync(t => t.UserId == Guid.Parse(userId) &&
                                                              t.LoginProvider == loginProvider &&
                                                              t.Name == "RefreshToken");
                                return tokenInDb != null; // Trả về true nếu còn trong DB
                            },
                            slidingExpiration: TimeSpan.FromMinutes(15) // Cache tồn tại 15 phút (bằng tuổi thọ JWT)
                        );

                        if (!isSessionValid)
                        {
                            context.Fail("Phiên đăng nhập đã hết hạn hoặc bị thu hồi trên thiết bị này.");
                            return;
                        }

                        // ====================================================
                        // KIỂM TRA 2: TRẠNG THÁI USER ACTIVE (Bọc qua Redis)
                        // ====================================================
                        string userStatusCacheKey = $"UserActive:{userId}";

                        var isUserActive = await cacheService.GetOrSetAsync(
                            userStatusCacheKey,
                            factory: async (cToken) =>
                            {
                                var user = await userManager.FindByIdAsync(userId);
                                return user != null && user.IsActive;
                            },
                            absoluteExpiration: TimeSpan.FromMinutes(5) // Cache 5 phút để check ban account nhanh nhạy
                        );

                        if (!isUserActive)
                        {
                            context.Fail("Tài khoản không tồn tại hoặc đã bị khóa.");
                            return;
                        }
                    }
                };
            });

            // 3. Cấu hình Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Nhập 'Bearer [khoảng trắng] [token của bạn]'.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}
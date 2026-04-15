using Application.Interfaces; // Thêm using này
using Domain.Repositories;
using Infrastructure.Postgres.Data;
using Infrastructure.Postgres.Identity;
using Infrastructure.Postgres.Identity.Sevices;
using Infrastructure.Postgres.Repository;
using Infrastructure.Postgres.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructurePostgres(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Đăng ký AppDbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // 2. Đăng ký Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ==========================================
        // ĐÂY CHÍNH LÀ DÒNG BẠN ĐANG THIẾU ĐỂ FIX LỖI
        // ==========================================
        services.AddScoped<IAuthService, AuthService>();

        // 3. Đăng ký UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 4. Đăng ký Generic Repository
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

        // 5. Đăng ký Repositories cụ thể
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ITodoItemRepository, TodoItemRepository>();

        // ĐÃ XÓA: services.AddHostedService<RedisTrackingBatchWorker>(); -> Vì đã có Hangfire lo!

        return services;
    }
}
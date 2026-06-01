using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OtpAuth.Application.Abstractions;
using OtpAuth.Infrastructure.Auth;
using OtpAuth.Infrastructure.Identity;
using OtpAuth.Infrastructure.Persistence;
using OtpAuth.Infrastructure.Settings;
using OtpAuth.Infrastructure.Sms;

namespace OtpAuth.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// EF Core (MSSQL), Microsoft Identity, JWT üretici, OTP servisi ve SMS göndericiyi kaydeder.
    /// Program.cs içinden tek satırla çağrılır: <c>builder.Services.AddInfrastructure(builder.Configuration)</c>
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Ayarlar (appsettings.json) ---
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));

        // --- EF Core + MSSQL ---
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // --- Microsoft Identity (passwordless => sadece UserManager, cookie yok) ---
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<AppDbContext>();

        // --- Uygulama servisleri ---
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        // SMS gönderici HttpClient ile (gerçek GSM API çağrısı SmsSender içinde yapılacak).
        services.AddHttpClient<ISmsSender, SmsSender>();

        return services;
    }
}

using System.Reflection;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using pos_system_api.Core.Application.Common.Behaviors;
using pos_system_api.Core.Application.Inventory.Services;
using pos_system_api.Infrastructure.Auth;
using pos_system_api.Infrastructure.Auth.Authorization;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.API.Extensions;

/// <summary>
/// Extension methods for configuring application services
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        var assembly = Assembly.Load("pos_system_api");

        // Register MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register MediatR pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IEffectivePackagingService, EffectivePackagingService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("pos_system_api")));

        // Register repositories
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IDrugRepository, 
                          pos_system_api.Infrastructure.Data.Repositories.DrugRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.ICategoryRepository,
                          pos_system_api.Infrastructure.Data.Repositories.CategoryRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IShopRepository,
                          pos_system_api.Infrastructure.Data.Repositories.ShopRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.ISupplierRepository,
                          pos_system_api.Infrastructure.Data.Repositories.SupplierRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IInventoryRepository,
                          pos_system_api.Infrastructure.Data.Repositories.InventoryRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IStockAdjustmentRepository,
                          pos_system_api.Infrastructure.Data.Repositories.StockAdjustmentRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IStockTransferRepository,
                          pos_system_api.Infrastructure.Data.Repositories.StockTransferRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IStockCountRepository,
                          pos_system_api.Infrastructure.Data.Repositories.StockCountRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IInventoryAlertRepository,
                          pos_system_api.Infrastructure.Data.Repositories.InventoryAlertRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IUserRepository,
                          pos_system_api.Infrastructure.Data.Repositories.UserRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IShopUserRepository,
                          pos_system_api.Infrastructure.Data.Repositories.ShopUserRepository>();
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IShopPackagingOverrideRepository,
                          pos_system_api.Infrastructure.Data.Repositories.ShopPackagingOverrideRepository>();

        // Register authentication services
        services.AddScoped<JwtTokenService>();
        services.AddScoped<PasswordHasher>();
        
        // Register inventory alert service
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IInventoryAlertService,
                          pos_system_api.Infrastructure.Services.InventoryAlertService>();
        
        // Register barcode service
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IBarcodeService,
                          pos_system_api.Infrastructure.Services.BarcodeService>();
        
        // Register purchase order repository
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.IPurchaseOrderRepository,
                          pos_system_api.Infrastructure.Data.Repositories.PurchaseOrderRepository>();
        
        // Register sales order (cashier order) repository
        services.AddScoped<pos_system_api.Core.Application.Common.Interfaces.ISalesOrderRepository,
                          pos_system_api.Infrastructure.Data.Repositories.SalesOrderRepository>();

        return services;
    }

    public static IServiceCollection AddAPILayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        
        // Configure JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"] 
                            ?? throw new InvalidOperationException("JWT Secret Key not configured"))),
                    ClockSkew = TimeSpan.Zero
                };
            });

        // Configure Authorization Policies
        services.AddAuthorization(options =>
        {
            // SuperAdmin only - requires SuperAdmin system role
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("SuperAdmin"));

            // Shop Owner or Admin - requires SuperAdmin or Owner role in any shop
            options.AddPolicy("ShopOwnerOrAdmin", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("systemRole", "SuperAdmin") ||
                    context.User.Claims.Any(c => c.Type.EndsWith(":isOwner") && c.Value == "True")));

            // Shop Access - validates user has access to the requested shop
            options.AddPolicy("ShopAccess", policy =>
                policy.Requirements.Add(new ShopAccessRequirement()));
        });

        // Register authorization handler
        services.AddScoped<IAuthorizationHandler, ShopAccessHandler>();
        services.AddHttpContextAccessor(); // Required for ShopAccessHandler

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "POS System API",
                Version = "v1",
                Description = "Multi-Tenant Pharmacy POS System with JWT Authentication"
            });

            // Add JWT Authentication to Swagger
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Add CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        return services;
    }
}

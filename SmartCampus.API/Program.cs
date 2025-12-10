using Microsoft.EntityFrameworkCore;
using SmartCampus.DataAccess;
using SmartCampus.DataAccess.Repositories;
using SmartCampus.Business.Services;
using SmartCampus.Business.Mappings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SmartCampus.Entities;
using Microsoft.OpenApi.Models; // Explicitly added
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.DependencyInjection; // Explicitly added
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ensure PascalCase property names (C# convention)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true; // For debugging
    });
builder.Services.AddEndpointsApiExplorer();

// Add CORS - Frontend için gerekli
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://localhost:5174", 
                "http://localhost:5175",
                "https://localhost:5173",
                "https://localhost:5174",
                "https://localhost:5175"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Smart Campus API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// Configure MySQL Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CampusDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 23)), 
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// Add Identity
builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<Role>()
.AddEntityFrameworkStores<CampusDbContext>();

// Register Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Business Services
builder.Services.AddAutoMapper(typeof(SmartCampus.Business.Mappings.MappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<SmartCampus.Business.Validators.RegisterDtoValidator>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        // Map JWT 'sub' claim to NameIdentifier
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
});

var app = builder.Build();

// Auto-migrate database in Development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        try
        {
            var context = services.GetRequiredService<CampusDbContext>();
            
            // Check if database exists and can connect
            if (context.Database.CanConnect())
            {
                logger.LogInformation("Database connection successful. Applying migrations...");
                
                // Get pending migrations
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation($"Found {pendingMigrations.Count()} pending migration(s): {string.Join(", ", pendingMigrations)}");
                    // Apply pending migrations
                    context.Database.Migrate();
                    logger.LogInformation("Database migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("No pending migrations. Database is up to date.");
                    // Note: "pending model changes" warning from EF Core is expected during development
                    // It means the model has changed but no migration was created yet
                    // This is OK - the app will continue to work
                }
            }
            else
            {
                logger.LogWarning("Cannot connect to database. Please check your connection string.");
            }
        }
        catch (Exception ex)
        {
            // Check if it's just a "pending changes" warning
            if (ex.Message.Contains("pending changes") || ex.Message.Contains("Add a new migration"))
            {
                // This is just a warning, not a critical error
                // Log as warning and continue - app will work fine
                logger.LogWarning("⚠️ Model has pending changes (this is OK during development).");
                logger.LogWarning("💡 To fix: Run 'dotnet ef migrations add MigrationName --project ../SmartCampus.DataAccess --startup-project .'");
                logger.LogInformation("✅ Application will continue running...");
            }
            else
            {
                // Real database error - log as error
                logger.LogError(ex, "❌ An error occurred while migrating the database.");
            }
            // Don't throw - let the app start even if migration fails
            // This allows the developer to fix migration issues manually
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<SmartCampus.API.Middleware.ExceptionMiddleware>();

// CORS - Authentication'dan ÖNCE olmalı
app.UseCors("AllowFrontend");

// Development'ta HTTPS redirect'i devre dışı bırak (CORS sorununu önlemek için)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

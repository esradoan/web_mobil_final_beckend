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
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders; // Static files için
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use camelCase for JSON (standard for web APIs)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true; // For debugging
    });
builder.Services.AddEndpointsApiExplorer();

// Add CORS - Frontend için gerekli
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = new List<string>
        {
            // Local development ports
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175",
            "https://localhost:5173",
            "https://localhost:5174",
            "https://localhost:5175"
        };
        
        // Production frontend URL'i environment variable'dan al
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrEmpty(frontendUrl))
        {
            // Birden fazla URL varsa (virgülle ayrılmış) ekle
            var urls = frontendUrl.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var url in urls)
            {
                var trimmedUrl = url.Trim();
                if (!string.IsNullOrEmpty(trimmedUrl))
                {
                    // URL'i normalize et (trailing slash'i kaldır)
                    trimmedUrl = trimmedUrl.TrimEnd('/');
                    
                    if (!allowedOrigins.Contains(trimmedUrl))
                    {
                        allowedOrigins.Add(trimmedUrl);
                    }
                    
                    // HTTPS versiyonunu da ekle (eğer HTTP ise)
                    if (trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        var httpsUrl = trimmedUrl.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
                        if (!allowedOrigins.Contains(httpsUrl))
                        {
                            allowedOrigins.Add(httpsUrl);
                        }
                    }
                    // HTTP versiyonunu da ekle (eğer HTTPS ise ve production değilse)
                    else if (trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && 
                             !builder.Environment.IsProduction())
                    {
                        var httpUrl = trimmedUrl.Replace("https://", "http://", StringComparison.OrdinalIgnoreCase);
                        if (!allowedOrigins.Contains(httpUrl))
                        {
                            allowedOrigins.Add(httpUrl);
                        }
                    }
                }
            }
        }
        
        // Railway frontend URL'i environment variable'dan al (opsiyonel - backward compatibility)
        var railwayFrontendUrl = builder.Configuration["RailwayFrontendUrl"];
        if (!string.IsNullOrEmpty(railwayFrontendUrl))
        {
            var trimmedUrl = railwayFrontendUrl.Trim().TrimEnd('/');
            if (!allowedOrigins.Contains(trimmedUrl))
            {
                allowedOrigins.Add(trimmedUrl);
            }
        }
        
        // FrontendUrl'den de CORS için origin ekle (eğer farklıysa)
        var configFrontendUrl = builder.Configuration["FrontendUrl"];
        if (!string.IsNullOrEmpty(configFrontendUrl))
        {
            var trimmedUrl = configFrontendUrl.Trim().TrimEnd('/');
            if (!allowedOrigins.Contains(trimmedUrl))
            {
                allowedOrigins.Add(trimmedUrl);
            }
            // HTTPS versiyonunu da ekle
            if (trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                var httpsUrl = trimmedUrl.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
                if (!allowedOrigins.Contains(httpsUrl))
                {
                    allowedOrigins.Add(httpsUrl);
                }
            }
        }
        
        // CORS allowed origins'i logla (debug için)
        Console.WriteLine($"\n🌐 CORS Configuration:");
        Console.WriteLine($"   Allowed Origins ({allowedOrigins.Count}): {string.Join(", ", allowedOrigins)}");
        
        policy.WithOrigins(allowedOrigins.ToArray())
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
// Öncelik: 1) ConnectionStrings__DefaultConnection, 2) MYSQL* environment variables
var connectionString = (string?)null;
var connectionStringSource = "";

// MYSQL* environment variables (template replacement için)
var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");
var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER");
var mysqlPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? "railway";

// Öncelik 1: ConnectionStrings__DefaultConnection environment variable
var configConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(configConnectionString) && configConnectionString.Trim() != "")
{
    connectionString = configConnectionString;
    connectionStringSource = "ConnectionStrings__DefaultConnection";
    
    // Template variables'ı replace et (${VAR_NAME} formatı)
    if (connectionString.Contains("${"))
    {
        connectionString = connectionString
            .Replace("${MYSQLHOST}", mysqlHost ?? "")
            .Replace("${MYSQLPORT}", mysqlPort)
            .Replace("${MYSQLUSER}", mysqlUser ?? "")
            .Replace("${MYSQLPASSWORD}", mysqlPassword ?? "")
            .Replace("${MYSQLDATABASE}", mysqlDatabase);
        
        Console.WriteLine($"✅ Using connection string from ConnectionStrings__DefaultConnection (with template replacement)");
    }
    else
    {
        Console.WriteLine($"✅ Using connection string from ConnectionStrings__DefaultConnection");
    }
    
    // AllowPublicKeyRetrieval=True ve SslMode=Required ekle (yoksa)
    if (!connectionString.Contains("AllowPublicKeyRetrieval=", StringComparison.OrdinalIgnoreCase))
    {
        var separator = connectionString.EndsWith(";") ? "" : ";";
        connectionString = $"{connectionString}{separator}AllowPublicKeyRetrieval=True;";
    }
    
    if (!connectionString.Contains("SslMode=", StringComparison.OrdinalIgnoreCase))
    {
        var separator = connectionString.EndsWith(";") ? "" : ";";
        connectionString = $"{connectionString}{separator}SslMode=Required;";
    }
}

// Öncelik 2: MYSQL* environment variables (fallback)
if (string.IsNullOrEmpty(connectionString))
{
    if (!string.IsNullOrEmpty(mysqlHost) && !string.IsNullOrEmpty(mysqlUser) && !string.IsNullOrEmpty(mysqlPassword))
    {
        connectionString = $"Server={mysqlHost};Port={mysqlPort};Database={mysqlDatabase};User={mysqlUser};Password={mysqlPassword};AllowPublicKeyRetrieval=True;SslMode=Required;";
        connectionStringSource = "MYSQL* variables";
        Console.WriteLine($"✅ Using connection string from MYSQL* environment variables");
    }
}

// Connection string validation
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Connection string is not configured. " +
        "Please set either 'ConnectionStrings__DefaultConnection' environment variable " +
        "or Railway MySQL variables (MYSQLHOST, MYSQLUSER, MYSQLPASSWORD, MYSQLDATABASE)."
    );
}

// Log connection string (password masked)
var maskedConnectionString = connectionString.Contains("Password=") 
    ? connectionString.Substring(0, connectionString.IndexOf("Password=") + 9) + "***;" 
    : connectionString;
Console.WriteLine($"\n🔌 Connection String Configuration:");
Console.WriteLine($"   Source: {connectionStringSource}");
Console.WriteLine($"   Connection String: {maskedConnectionString}");

// Extract and log database name
var dbNameMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Database=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
if (dbNameMatch.Success)
{
    Console.WriteLine($"   Database: {dbNameMatch.Groups[1].Value}");
}

builder.Services.AddDbContext<CampusDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)), 
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
.AddEntityFrameworkStores<CampusDbContext>()
.AddDefaultTokenProviders();

// Register Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Business Services
builder.Services.AddAutoMapper(typeof(SmartCampus.Business.Mappings.MappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<SmartCampus.Business.Validators.RegisterDtoValidator>();

// Email Service: Use SMTPEmailService if configured, otherwise fallback to MockEmailService
// Öncelik: Environment variables > appsettings.json
var smtpHost = Environment.GetEnvironmentVariable("SmtpSettings__Host") ?? builder.Configuration["SmtpSettings:Host"];
var smtpPort = Environment.GetEnvironmentVariable("SmtpSettings__Port") ?? builder.Configuration["SmtpSettings:Port"] ?? "587";
var smtpUsername = Environment.GetEnvironmentVariable("SmtpSettings__Username") ?? builder.Configuration["SmtpSettings:Username"];
var smtpPassword = Environment.GetEnvironmentVariable("SmtpSettings__Password") ?? builder.Configuration["SmtpSettings:Password"];
var smtpFromEmail = Environment.GetEnvironmentVariable("SmtpSettings__FromEmail") ?? builder.Configuration["SmtpSettings:FromEmail"];
var smtpFromName = Environment.GetEnvironmentVariable("SmtpSettings__FromName") ?? builder.Configuration["SmtpSettings:FromName"] ?? "Smart Campus";
var smtpEnableSsl = Environment.GetEnvironmentVariable("SmtpSettings__EnableSsl") ?? builder.Configuration["SmtpSettings:EnableSsl"] ?? "true";

// SMTP settings'i configuration'a ekle (SMTPEmailService kullanacak)
if (!string.IsNullOrEmpty(smtpHost) && !string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
{
    // Configuration'a environment variable'lardan gelen değerleri ekle
    builder.Configuration["SmtpSettings:Host"] = smtpHost;
    builder.Configuration["SmtpSettings:Port"] = smtpPort;
    builder.Configuration["SmtpSettings:Username"] = smtpUsername;
    builder.Configuration["SmtpSettings:Password"] = smtpPassword;
    builder.Configuration["SmtpSettings:FromEmail"] = smtpFromEmail ?? smtpUsername;
    builder.Configuration["SmtpSettings:FromName"] = smtpFromName;
    builder.Configuration["SmtpSettings:EnableSsl"] = smtpEnableSsl;
    
    // Use real SMTP email service
    builder.Services.AddScoped<IEmailService, SMTPEmailService>();
    Console.WriteLine($"✅ SMTP Email Service configured - Host: {smtpHost}, Port: {smtpPort}, From: {smtpFromEmail ?? smtpUsername}");
}
else
{
    // Fallback to mock email service if SMTP not configured
    builder.Services.AddScoped<IEmailService, MockEmailService>();
    Console.WriteLine("⚠️ MockEmailService will be used - SMTP not fully configured");
}

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure JWT Authentication
// Öncelik: Environment variables > appsettings.json
var jwtSecret = Environment.GetEnvironmentVariable("JwtSettings__Secret") ?? builder.Configuration["JwtSettings:Secret"];
var jwtIssuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer") ?? builder.Configuration["JwtSettings:Issuer"] ?? "SmartCampusAPI";
var jwtAudience = Environment.GetEnvironmentVariable("JwtSettings__Audience") ?? builder.Configuration["JwtSettings:Audience"] ?? "SmartCampusClient";
var jwtAccessTokenExpiration = Environment.GetEnvironmentVariable("JwtSettings__AccessTokenExpirationMinutes") ?? builder.Configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15";
var jwtRefreshTokenExpiration = Environment.GetEnvironmentVariable("JwtSettings__RefreshTokenExpirationDays") ?? builder.Configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7";

// JWT Secret validation - Production'da mutlaka set edilmeli
if (string.IsNullOrEmpty(jwtSecret))
{
    var isProductionEnv = builder.Environment.IsProduction() || 
                          Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;
    
    if (isProductionEnv)
    {
        throw new InvalidOperationException(
            "JWT Secret is not configured. " +
            "Please set 'JwtSettings__Secret' environment variable in Railway. " +
            "Secret must be at least 32 characters long."
        );
    }
    else
    {
        // Development için default secret (güvenli değil, sadece development için)
        jwtSecret = "SuperSecretKeyForSmartCampusProject_MustBeVeryLong_AtLeast32Chars";
        Console.WriteLine("⚠️ Using default JWT Secret (NOT SECURE - Development only)");
    }
}

if (jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        $"JWT Secret must be at least 32 characters long. Current length: {jwtSecret.Length}"
    );
}

// Configuration'a environment variable'lardan gelen değerleri ekle
builder.Configuration["JwtSettings:Secret"] = jwtSecret;
builder.Configuration["JwtSettings:Issuer"] = jwtIssuer;
builder.Configuration["JwtSettings:Audience"] = jwtAudience;
builder.Configuration["JwtSettings:AccessTokenExpirationMinutes"] = jwtAccessTokenExpiration;
builder.Configuration["JwtSettings:RefreshTokenExpirationDays"] = jwtRefreshTokenExpiration;

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSecret);
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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        // Map JWT 'sub' claim to NameIdentifier
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
});

Console.WriteLine($"✅ JWT configured - Issuer: {jwtIssuer}, Audience: {jwtAudience}, AccessTokenExpiration: {jwtAccessTokenExpiration} minutes");

var app = builder.Build();

// Log email service status
using (var scope = app.Services.CreateScope())
{
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    if (emailService is SMTPEmailService)
    {
        logger.LogInformation("✅ SMTP Email Service aktif - Gerçek email gönderilecek");
    }
    else if (emailService is MockEmailService)
    {
        logger.LogWarning("⚠️  MockEmailService kullanılıyor. Gerçek email göndermek için appsettings.json'da SmtpSettings bölümünü doldurun.");
    }
}

// Auto-migrate database (both Development and Production)
// SKIP_MIGRATIONS environment variable ile migration'ı atlayabilirsiniz
var skipMigrations = Environment.GetEnvironmentVariable("SKIP_MIGRATIONS") == "true";
if (!skipMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        try
        {
            var context = services.GetRequiredService<CampusDbContext>();
            
            logger.LogInformation("🔍 Attempting to connect to database...");
            
            // Connection string'i logla (güvenlik için password gizli)
            var dbConnectionString = context.Database.GetConnectionString();
            if (!string.IsNullOrEmpty(dbConnectionString))
            {
                var maskedDbConn = dbConnectionString.Contains("Password=") 
                    ? dbConnectionString.Substring(0, dbConnectionString.IndexOf("Password=") + 9) + "***;" 
                    : dbConnectionString;
                logger.LogInformation($"📊 Database connection string: {maskedDbConn}");
            }
            
            // Retry mechanism for database connection
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(5);
            var connected = false;
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    connected = context.Database.CanConnect();
                    if (connected)
                    {
                        logger.LogInformation("✅ Database connection successful!");
                        break;
                    }
                }
                catch (Exception connectEx)
                {
                    logger.LogWarning($"⚠️ Connection attempt {i + 1}/{maxRetries} failed: {connectEx.Message}");
                    
                    if (i < maxRetries - 1)
                    {
                        logger.LogInformation($"⏳ Retrying in {retryDelay.TotalSeconds} seconds...");
                        Thread.Sleep(retryDelay);
                    }
                    else
                    {
                        logger.LogError("❌ Cannot connect to database after {MaxRetries} attempts.", maxRetries);
                        throw new Exception("Cannot connect to DB", connectEx);
                    }
                }
            }
            
            if (connected)
            {
                logger.LogInformation("🔄 Checking for pending migrations...");
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation($"📦 Found {pendingMigrations.Count()} pending migration(s): {string.Join(", ", pendingMigrations)}");
                    logger.LogInformation("🚀 Applying migrations...");
                    context.Database.Migrate();
                    logger.LogInformation("✅ Database migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("✅ No pending migrations. Database is up to date.");
                }
            }
        }
        catch (Exception ex)
        {
            if (app.Environment.IsProduction())
            {
                logger.LogError(ex, "❌ Critical error: Failed to migrate database in Production. Application cannot start.");
                throw;
            }
            else
            {
                logger.LogError(ex, "❌ An error occurred while migrating the database.");
                logger.LogWarning("⚠️ Application will continue, but database may not be up to date.");
            }
        }
    }
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("⚠️ SKIP_MIGRATIONS=true detected. Skipping database migrations.");
    logger.LogWarning("⚠️ Make sure to run migrations manually or remove this flag.");
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

// ⭐ Static Files Middleware - Profil resimleri için
app.UseStaticFiles(); // wwwroot için

// Uploads klasörünü serve et (profil resimleri burada)
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Cache 10 dakika
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=600");
    }
});

// Development'ta HTTPS redirect'i devre dışı bırak (CORS sorununu önlemek için)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Railway ve diğer platformlar için PORT environment variable'ını kullan
// Yerel geliştirmede PORT yoksa launchSettings.json kullanılır
var port = Environment.GetEnvironmentVariable("PORT");
var environment = app.Environment.EnvironmentName;
var isProduction = app.Environment.IsProduction();

Console.WriteLine($"\n🚀 Application Startup Configuration:");
Console.WriteLine($"   Environment: {environment}");
Console.WriteLine($"   IsProduction: {isProduction}");
Console.WriteLine($"   PORT Environment Variable: {(string.IsNullOrEmpty(port) ? "NOT SET (using launchSettings.json)" : port)}");

if (!string.IsNullOrEmpty(port))
{
    // Production (Railway, Heroku, vb.) - PORT environment variable set edilmiş
    if (!int.TryParse(port, out int portNumber) || portNumber <= 0 || portNumber > 65535)
    {
        throw new InvalidOperationException(
            $"Invalid PORT environment variable value: '{port}'. " +
            $"PORT must be a valid integer between 1 and 65535."
        );
    }
    
    var listenUrl = $"http://0.0.0.0:{portNumber}";
    Console.WriteLine($"✅ Starting application on: {listenUrl}");
    Console.WriteLine($"🌐 Application will be accessible on Railway's domain");
    Console.WriteLine($"📡 Listening on all network interfaces (0.0.0.0)");
    
    // Railway otomatik olarak HTTPS proxy yapar, bu yüzden HTTP dinliyoruz
    app.Run(listenUrl);
}
else
{
    // Development - launchSettings.json kullanılır
    Console.WriteLine($"ℹ️ PORT not set, using launchSettings.json (development mode)");
    Console.WriteLine($"   Default URLs: http://localhost:5226, https://localhost:7183");
    app.Run();
}

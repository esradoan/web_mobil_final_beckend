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
// Öncelik sırası: 1) MYSQL* environment variables, 2) MYSQL_URL, 3) ConnectionStrings__DefaultConnection (appsettings.json)
var connectionString = (string?)null;
var connectionStringSource = "";

// Önce Railway'nin otomatik MySQL variable'larını kontrol et (en güvenilir)
var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");
var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER");
var mysqlPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQLDATABASE");

// Debug: Environment variables'ları logla
Console.WriteLine($"\n🔍 MySQL Environment Variables Check:");
Console.WriteLine($"   MYSQLHOST: {(string.IsNullOrEmpty(mysqlHost) ? "NOT SET" : mysqlHost)}");
Console.WriteLine($"   MYSQLUSER: {(string.IsNullOrEmpty(mysqlUser) ? "NOT SET" : mysqlUser)}");
Console.WriteLine($"   MYSQLPASSWORD: {(string.IsNullOrEmpty(mysqlPassword) ? "NOT SET" : "***SET***")}");
Console.WriteLine($"   MYSQLDATABASE: {(string.IsNullOrEmpty(mysqlDatabase) ? "NOT SET" : mysqlDatabase)}");
Console.WriteLine($"   MYSQLPORT: {mysqlPort}");

// Öncelik 1: Ayrı ayrı MYSQL* variables kullan (en güvenilir)
if (!string.IsNullOrEmpty(mysqlHost) && !string.IsNullOrEmpty(mysqlUser) && !string.IsNullOrEmpty(mysqlPassword))
{
    // MYSQLDATABASE eksikse, Railway'nin varsayılan database adını kullan
    if (string.IsNullOrEmpty(mysqlDatabase))
    {
        mysqlDatabase = "railway";
        Console.WriteLine($"   ⚠️ MYSQLDATABASE not set, using default: railway");
    }
    
    // MySQL connection string formatı: Server=...;Database=...;User=...;Password=...;Port=...;
    // Railway internal network için SSL gerekmez
    // Database name validation - boş olamaz
    if (string.IsNullOrWhiteSpace(mysqlDatabase))
    {
        mysqlDatabase = "railway";
        Console.WriteLine($"   ⚠️ MYSQLDATABASE was empty, using default: railway");
    }
    
    // MySQL 8.0+ için AllowPublicKeyRetrieval=True gerekli (caching_sha2_password authentication için)
    // Pomelo.EntityFrameworkCore.MySql için "User" kullanılır (User ID değil)
    connectionString = $"Server={mysqlHost};Database={mysqlDatabase};User={mysqlUser};Password={mysqlPassword};Port={mysqlPort};SslMode=None;AllowPublicKeyRetrieval=True;";
    connectionStringSource = "MYSQL* variables";
    Console.WriteLine($"   ✅ Using MYSQL* variables to build connection string");
    Console.WriteLine($"   📊 Database name: {mysqlDatabase}");
    Console.WriteLine($"   📊 Connection string preview: Server={mysqlHost};Database={mysqlDatabase};User={mysqlUser};Password=***;Port={mysqlPort};AllowPublicKeyRetrieval=True;");
}
// Öncelik 2: MYSQL_URL variable'ını kontrol et (fallback)
else
{
    var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
    Console.WriteLine($"   MYSQL_URL: {(string.IsNullOrEmpty(mysqlUrl) ? "NOT SET" : "SET (length: " + mysqlUrl.Length + ")")}");
    
    if (!string.IsNullOrEmpty(mysqlUrl))
    {
        // MYSQL_URL formatı: mysql://user:password@host:port/database
        // Pomelo için Server=host;Database=database;User=user;Password=password;Port=port; formatına çevir
        if (mysqlUrl.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(mysqlUrl);
                var userInfo = uri.UserInfo.Split(':');
                var user = userInfo.Length > 0 ? userInfo[0] : "";
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                var host = uri.Host;
                var mysqlPortFromUrl = uri.Port > 0 ? uri.Port.ToString() : "3306";
                var database = uri.AbsolutePath.TrimStart('/');
                
                // Database name validation - boş olamaz
                if (string.IsNullOrWhiteSpace(database))
                {
                    // MYSQLDATABASE variable'ını kontrol et
                    database = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? "railway";
                    Console.WriteLine($"   ⚠️ Database name not found in MYSQL_URL, using: {database}");
                }
                
                // MySQL 8.0+ için AllowPublicKeyRetrieval=True gerekli (caching_sha2_password authentication için)
                connectionString = $"Server={host};Database={database};User={user};Password={password};Port={mysqlPortFromUrl};SslMode=None;AllowPublicKeyRetrieval=True;";
                connectionStringSource = "MYSQL_URL (parsed)";
                Console.WriteLine($"   ✅ Successfully parsed MYSQL_URL");
                Console.WriteLine($"   📊 Database name: {database}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to parse MYSQL_URL: {ex.Message}");
                // Parse başarısız olursa, connection string null kalır
                connectionString = null;
            }
        }
        else
        {
            // Zaten connection string formatındaysa direkt kullan
            // Ama database name kontrolü yap
            if (!mysqlUrl.Contains("Database=", StringComparison.OrdinalIgnoreCase) && 
                !mysqlUrl.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
            {
                // Database parametresi yoksa ekle
                var database = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? "railway";
                var separator = mysqlUrl.EndsWith(";") ? "" : ";";
                mysqlUrl = $"{mysqlUrl}{separator}Database={database};";
                Console.WriteLine($"   ⚠️ Database parameter not found in MYSQL_URL, added: Database={database}");
            }
            
            // AllowPublicKeyRetrieval=True ekle (yoksa)
            if (!mysqlUrl.Contains("AllowPublicKeyRetrieval=", StringComparison.OrdinalIgnoreCase))
            {
                var separator = mysqlUrl.EndsWith(";") ? "" : ";";
                mysqlUrl = $"{mysqlUrl}{separator}AllowPublicKeyRetrieval=True;";
                Console.WriteLine($"   ⚠️ AllowPublicKeyRetrieval not found in MYSQL_URL, added");
            }
            
            connectionString = mysqlUrl;
            connectionStringSource = "MYSQL_URL";
            Console.WriteLine($"   ✅ Using MYSQL_URL directly (not mysql:// format)");
        }
    }
}

// Öncelik 3: ConnectionStrings__DefaultConnection (appsettings.json veya environment variable) - fallback
if (string.IsNullOrEmpty(connectionString))
{
    var configConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(configConnectionString) && configConnectionString.Trim() != "")
    {
        connectionString = configConnectionString;
        connectionStringSource = "ConnectionStrings__DefaultConnection (appsettings.json)";
        Console.WriteLine($"   ⚠️ Using connection string from appsettings.json (fallback)");
        
        // AllowPublicKeyRetrieval=True ekle (yoksa ve Production değilse)
        // Production'da Railway MySQL kullanılmalı, appsettings.json fallback olmamalı
        if (!connectionString.Contains("AllowPublicKeyRetrieval=", StringComparison.OrdinalIgnoreCase))
        {
            var separator = connectionString.EndsWith(";") ? "" : ";";
            connectionString = $"{connectionString}{separator}AllowPublicKeyRetrieval=True;";
            Console.WriteLine($"   ⚠️ Added AllowPublicKeyRetrieval=True to connection string");
        }
        
        // Database name validation
        if (!connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase) && 
            !connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
        {
            var database = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? "railway";
            var separator = connectionString.EndsWith(";") ? "" : ";";
            connectionString = $"{connectionString}{separator}Database={database};";
            Console.WriteLine($"   ⚠️ Database parameter not found, added: Database={database}");
        }
    }
}

// Connection string validation ve detaylı logging
if (string.IsNullOrEmpty(connectionString))
{
    var availableVars = new List<string>();
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLHOST"))) availableVars.Add("MYSQLHOST");
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLUSER"))) availableVars.Add("MYSQLUSER");
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLPASSWORD"))) availableVars.Add("MYSQLPASSWORD");
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLDATABASE"))) availableVars.Add("MYSQLDATABASE");
    
    throw new InvalidOperationException(
        $"Connection string is not configured. " +
        $"Please set either 'ConnectionStrings__DefaultConnection' environment variable " +
        $"or Railway MySQL variables (MYSQLHOST, MYSQLUSER, MYSQLPASSWORD, MYSQLDATABASE). " +
        $"Available variables: {(availableVars.Any() ? string.Join(", ", availableVars) : "none")}"
    );
}

// Connection string'de Database parametresinin varlığını kontrol et
if (!connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase) && 
    !connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        $"Connection string does not contain Database parameter. " +
        $"Source: {connectionStringSource}. " +
        $"Connection string (masked): {connectionString.Replace("Password=", "Password=***").Substring(0, Math.Min(200, connectionString.Length))}"
    );
}

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
    var isProduction = builder.Environment.IsProduction() || 
                       Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;
    
    if (isProduction)
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

// Log connection string (password'u gizle) - app build edildikten sonra
var tempLogger = app.Services.GetRequiredService<ILogger<Program>>();
var maskedConnectionString = connectionString.Contains("Password=") 
    ? connectionString.Substring(0, connectionString.IndexOf("Password=") + 9) + "***;" 
    : connectionString;
tempLogger.LogInformation($"🔌 Connection string source: {connectionStringSource}");
tempLogger.LogInformation($"🔌 Using connection string: {maskedConnectionString}");

// Connection string'den database adını çıkar ve logla
var dbNameMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Database=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
if (dbNameMatch.Success)
{
    tempLogger.LogInformation($"📊 Database name from connection string: {dbNameMatch.Groups[1].Value}");
}
else
{
    tempLogger.LogWarning("⚠️ Database name not found in connection string!");
}

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
            
            // Check if database exists and can connect
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
                    logger.LogWarning($"⚠️ Connection attempt {i + 1}/{maxRetries} failed");
                    logger.LogWarning($"   Error: {connectEx.Message}");
                    
                    // Inner exception varsa onu da logla
                    if (connectEx.InnerException != null)
                    {
                        logger.LogWarning($"   Inner: {connectEx.InnerException.Message}");
                    }
                    
                    // Database ile ilgili özel hata mesajları
                    if (connectEx.Message.Contains("Unknown database", StringComparison.OrdinalIgnoreCase) ||
                        connectEx.Message.Contains("database ''", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogError("❌ Database does not exist or database name is empty!");
                        logger.LogError("💡 Solution: Check MYSQLDATABASE variable in Railway MySQL service");
                        logger.LogError("💡 Or create the database manually in MySQL");
                    }
                    
                    if (i < maxRetries - 1)
                    {
                        logger.LogInformation($"⏳ Retrying in {retryDelay.TotalSeconds} seconds...");
                        Thread.Sleep(retryDelay);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            if (connected)
            {
                logger.LogInformation("🔄 Checking for pending migrations...");
                
                // Get pending migrations
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation($"📦 Found {pendingMigrations.Count()} pending migration(s): {string.Join(", ", pendingMigrations)}");
                    logger.LogInformation("🚀 Applying migrations...");
                    // Apply pending migrations
                    context.Database.Migrate();
                    logger.LogInformation("✅ Database migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("✅ No pending migrations. Database is up to date.");
                }
            }
            else
            {
                logger.LogError("❌ Cannot connect to database after {MaxRetries} attempts.", maxRetries);
                logger.LogError("💡 Please check:");
                logger.LogError("   1. MySQL service is running on Railway");
                logger.LogError("   2. Connection string is correct (ConnectionStrings__DefaultConnection or MYSQL* variables)");
                logger.LogError("   3. Database name exists (MYSQLDATABASE variable)");
                logger.LogError("   4. Network connectivity between services");
                
                // In Production, fail fast if database connection fails
                if (app.Environment.IsProduction())
                {
                    throw new Exception("Cannot connect to database in Production. Application cannot start.");
                }
            }
        }
        catch (Exception ex)
        {
            // In Production, migration failures should prevent app startup
            if (app.Environment.IsProduction())
            {
                logger.LogError(ex, "❌ Critical error: Failed to migrate database in Production. Application cannot start.");
                logger.LogError("💡 To skip migrations temporarily, set SKIP_MIGRATIONS=true environment variable");
                throw; // Fail fast in Production
            }
            else
            {
                // In Development, allow app to start even if migration fails
                if (ex.Message.Contains("pending changes") || ex.Message.Contains("Add a new migration"))
                {
                    logger.LogWarning("⚠️ Model has pending changes (this is OK during development).");
                    logger.LogWarning("💡 To fix: Run 'dotnet ef migrations add MigrationName --project ../SmartCampus.DataAccess --startup-project .'");
                    logger.LogInformation("✅ Application will continue running...");
                }
                else
                {
                    logger.LogError(ex, "❌ An error occurred while migrating the database.");
                    logger.LogWarning("⚠️ Application will continue, but database may not be up to date.");
                }
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

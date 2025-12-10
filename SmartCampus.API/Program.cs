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
                if (!string.IsNullOrEmpty(url) && !allowedOrigins.Contains(url))
                {
                    allowedOrigins.Add(url);
                }
            }
        }
        
        // Railway frontend URL'i environment variable'dan al (opsiyonel - backward compatibility)
        var railwayFrontendUrl = builder.Configuration["RailwayFrontendUrl"];
        if (!string.IsNullOrEmpty(railwayFrontendUrl) && !allowedOrigins.Contains(railwayFrontendUrl))
        {
            allowedOrigins.Add(railwayFrontendUrl);
        }
        
        // FrontendUrl'den de CORS için origin ekle (eğer farklıysa)
        var configFrontendUrl = builder.Configuration["FrontendUrl"];
        if (!string.IsNullOrEmpty(configFrontendUrl) && !allowedOrigins.Contains(configFrontendUrl))
        {
            allowedOrigins.Add(configFrontendUrl);
            // HTTPS versiyonunu da ekle
            if (configFrontendUrl.StartsWith("http://"))
            {
                allowedOrigins.Add(configFrontendUrl.Replace("http://", "https://"));
            }
        }
        
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
    
    connectionString = $"Server={mysqlHost};Database={mysqlDatabase};User={mysqlUser};Password={mysqlPassword};Port={mysqlPort};SslMode=None;";
    connectionStringSource = "MYSQL* variables";
    Console.WriteLine($"   ✅ Using MYSQL* variables to build connection string");
    Console.WriteLine($"   📊 Database name: {mysqlDatabase}");
    Console.WriteLine($"   📊 Connection string preview: Server={mysqlHost};Database={mysqlDatabase};User={mysqlUser};Password=***;Port={mysqlPort};");
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
                
                connectionString = $"Server={host};Database={database};User={user};Password={password};Port={mysqlPortFromUrl};SslMode=None;";
                connectionStringSource = "MYSQL_URL (parsed)";
                Console.WriteLine($"   ✅ Successfully parsed MYSQL_URL");
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
var smtpSettings = builder.Configuration.GetSection("SmtpSettings");
var smtpHost = smtpSettings["Host"];
var smtpUsername = smtpSettings["Username"];
var smtpPassword = smtpSettings["Password"];

if (!string.IsNullOrEmpty(smtpHost) && !string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
{
    // Use real SMTP email service
    builder.Services.AddScoped<IEmailService, SMTPEmailService>();
}
else
{
    // Fallback to mock email service if SMTP not configured
    builder.Services.AddScoped<IEmailService, MockEmailService>();
}

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
Console.WriteLine($"\n🔌 PORT Environment Variable: {(string.IsNullOrEmpty(port) ? "NOT SET" : port)}");

if (!string.IsNullOrEmpty(port))
{
    // Production (Railway, Heroku, vb.) - PORT environment variable set edilmiş
    var listenUrl = $"http://0.0.0.0:{port}";
    Console.WriteLine($"✅ Starting application on: {listenUrl}");
    Console.WriteLine($"🌐 Application will be accessible on Railway's domain");
    app.Run(listenUrl);
}
else
{
    // Development - launchSettings.json kullanılır
    Console.WriteLine($"⚠️ PORT not set, using launchSettings.json (development mode)");
    app.Run();
}

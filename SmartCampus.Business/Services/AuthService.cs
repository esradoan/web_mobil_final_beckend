using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;

namespace SmartCampus.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly SmartCampus.DataAccess.Repositories.IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public AuthService(UserManager<User> userManager, IMapper mapper, IConfiguration configuration, SmartCampus.DataAccess.Repositories.IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _userManager = userManager;
            _mapper = mapper;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }



        public async Task<TokenDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null) throw new Exception("Invalid credentials");

            // Check if locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                 throw new Exception("Account is locked due to multiple failed login attempts. Try again later.");
            }

            if (!await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                // Increment failed count
                await _userManager.AccessFailedAsync(user);
                
                if (await _userManager.IsLockedOutAsync(user))
                {
                     throw new Exception("Account is locked due to multiple failed login attempts. Try again later.");
                }

                throw new Exception("Invalid credentials");
            }

            // Reset failed count on success
            await _userManager.ResetAccessFailedCountAsync(user);

            // Check Email Confirmation
            if (_userManager.Options.SignIn.RequireConfirmedEmail && !await _userManager.IsEmailConfirmedAsync(user))
            {
                 // thrown if enforced
            }

            // Log activity (ignore errors if table doesn't exist yet)
            try
            {
            await LogActivityAsync(user.Id, "Login", "User logged in via password.");
            }
            catch
            {
                // Ignore logging errors - table might not exist yet
            }

            return await GenerateTokensAsync(user, loginDto.RememberMe);
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // 1. Check if exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new Exception("User with this email already exists");
            }

            // 2. Validate Role-Specific Requirements BEFORE creating user
            if (registerDto.Role == UserRole.Student)
            {
                if (string.IsNullOrEmpty(registerDto.StudentNumber) || registerDto.DepartmentId == null)
                    throw new Exception("Student Number and Department are required for Students.");

                // Verify Department exists
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(registerDto.DepartmentId.Value);
                if (department == null)
                    throw new Exception($"Department with ID {registerDto.DepartmentId.Value} does not exist.");
            }
            else if (registerDto.Role == UserRole.Faculty)
            {
                if (string.IsNullOrEmpty(registerDto.EmployeeNumber) || registerDto.DepartmentId == null)
                    throw new Exception("Employee Number and Department are required for Faculty.");

                // Verify Department exists
                var department = await _unitOfWork.Repository<Department>().GetByIdAsync(registerDto.DepartmentId.Value);
                if (department == null)
                    throw new Exception($"Department with ID {registerDto.DepartmentId.Value} does not exist.");
            }

            // 3. Map DTO to Entity
            var user = _mapper.Map<User>(registerDto);
            user.UserName = registerDto.Email; // Identity requires UserName
            user.CreatedAt = DateTime.UtcNow;

            // 4. Create User (Identity handles Hashing and Saving)
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Registration failed: {errors}");
            }

            // 5. Assign Role to User in Identity (CRITICAL: Must be done before creating role-specific entry)
            string roleName = registerDto.Role == UserRole.Student ? "Student" :
                             registerDto.Role == UserRole.Faculty ? "Faculty" :
                             registerDto.Role == UserRole.Admin ? "Admin" : "Student";
            
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                // Rollback: Delete the user if role assignment fails
                await _userManager.DeleteAsync(user);
                var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new Exception($"Failed to assign role '{roleName}': {roleErrors}");
            }
            Console.WriteLine($"✅ Role '{roleName}' assigned to user {user.Id}");

            // 6. Create Role-Specific Entry (now we know everything is valid)
            try
            {
            if (registerDto.Role == UserRole.Student)
            {
                var student = new Student
                {
                    UserId = user.Id,
                        StudentNumber = registerDto.StudentNumber!,
                        DepartmentId = registerDto.DepartmentId!.Value
                };
                await _unitOfWork.Repository<Student>().AddAsync(student);
            }
            else if (registerDto.Role == UserRole.Faculty)
            {
                var faculty = new Faculty
                {
                    UserId = user.Id,
                        EmployeeNumber = registerDto.EmployeeNumber!,
                        DepartmentId = registerDto.DepartmentId!.Value,
                    Title = "Instructor" 
                };
                await _unitOfWork.Repository<Faculty>().AddAsync(faculty);
            }

            await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                // Rollback: Delete the user if Student/Faculty creation fails
                try
                {
                    await _userManager.DeleteAsync(user);
                }
                catch
                {
                    // Ignore deletion errors, log in production
                }
                
                // Log inner exception for debugging
                var innerException = ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "";
                Console.WriteLine($"❌ ERROR creating role-specific entry: {ex.Message}{innerException}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                
                throw new Exception($"Failed to create role-specific entry: {ex.Message}{innerException}. Please check if Department exists in database.");
            }

            // 5. Generate Email Verification Token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            
            // URL-encode the token for safe transmission in URL
            var encodedToken = System.Net.WebUtility.UrlEncode(token);
            
            // Generate verification URL
            // In development: use localhost, in production: use actual frontend URL
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
            var verificationUrl = $"{frontendUrl}/verify-email?userId={user.Id}&token={encodedToken}";
            var body = SmartCampus.Business.Helpers.EmailTemplates.GetVerificationEmail(user.FirstName ?? user.Email!, verificationUrl);

            await _emailService.SendEmailAsync(user.Email!, "Welcome to Smart Campus - Verify Email", body);

            // Return user with verification URL for frontend
            return new RegisterResponseDto
            {
                User = _mapper.Map<UserDto>(user),
                VerificationUrl = verificationUrl,
                VerificationToken = encodedToken // For development - show in UI if mock email
            };
        }

        public async Task VerifyEmailAsync(string userId, string token)
        {
            Console.WriteLine($"\n🔍 VERIFY EMAIL DEBUG:");
            Console.WriteLine($"   User ID: {userId}");
            Console.WriteLine($"   Token (first 50 chars): {token.Substring(0, Math.Min(50, token.Length))}...");
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) 
            {
                Console.WriteLine($"❌ User not found: {userId}");
                throw new Exception("User not found");
            }

            Console.WriteLine($"   User Email: {user.Email}");
            Console.WriteLine($"   Current EmailConfirmed: {user.EmailConfirmed}");
            Console.WriteLine($"   Received token length: {token.Length}");
            Console.WriteLine($"   User SecurityStamp: {user.SecurityStamp}");

            // URL decode token if it's encoded (from email link)
            // Frontend artık token'ı decode ediyor, ama yine de kontrol edelim
            var decodedToken = token;
            
            // Token'da % karakteri varsa, encode edilmiş demektir (frontend decode etmemiş)
            if (token.Contains("%"))
            {
                Console.WriteLine($"   ⚠️ Token contains % - frontend decode etmemiş, backend decode ediyor...");
                var decodeAttempts = 0;
                var maxDecodeAttempts = 5;
                
                while (decodeAttempts < maxDecodeAttempts)
                {
                    var previousToken = decodedToken;
                    decodedToken = System.Net.WebUtility.UrlDecode(decodedToken);
                    
                    // Eğer decode işlemi token'ı değiştirmediyse, decode tamamlanmış demektir
                    if (decodedToken == previousToken)
                    {
                        break;
                    }
                    decodeAttempts++;
                }
                
                Console.WriteLine($"   Token decode attempts: {decodeAttempts}");
            }
            else
            {
                Console.WriteLine($"   ✅ Token does not contain % - frontend decode etmiş, using as-is");
            }
            
            var tokenToUse = decodedToken;
            
            Console.WriteLine($"   Token length: {token.Length} -> {tokenToUse.Length} (after decode)");
            Console.WriteLine($"   Token changed: {tokenToUse != token}");
            Console.WriteLine($"   Token (first 30 chars): {tokenToUse.Substring(0, Math.Min(30, tokenToUse.Length))}...");
            Console.WriteLine($"   Token (last 30 chars): ...{tokenToUse.Substring(Math.Max(0, tokenToUse.Length - 30))}");
            Console.WriteLine($"   Token contains +: {tokenToUse.Contains("+")}");
            Console.WriteLine($"   Token contains /: {tokenToUse.Contains("/")}");
            Console.WriteLine($"   Token contains =: {tokenToUse.Contains("=")}");

            // Identity token doğrulama
            // Not: Token'ın tamamının geldiğinden emin ol (email client'lar bazen URL'yi kesebilir)
            var result = await _userManager.ConfirmEmailAsync(user, tokenToUse);
            
            Console.WriteLine($"   ConfirmEmailAsync result: {result.Succeeded}");
            if (!result.Succeeded)
            {
                var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"❌ Errors: {errorMessages}");
                
                // Token'ın tamamının gelip gelmediğini kontrol et
                // Identity token'ları genellikle 200+ karakterdir
                if (tokenToUse.Length < 100)
                {
                    Console.WriteLine($"⚠️ WARNING: Token length is very short ({tokenToUse.Length} chars). Email client may have truncated the URL!");
                    throw new Exception("Email verification failed: Token appears to be truncated. Please use the link directly from the email, or request a new verification email.");
                }
                
                throw new Exception("Email verification failed: " + errorMessages);
            }
            
            // Email doğrulandı - FindByIdAsync çağırmaya gerek yok (concurrency hatasına neden olabilir)
            // ConfirmEmailAsync başarılı olduğunda email zaten doğrulanmış oluyor
            Console.WriteLine($"✅ Email verification successful for user {userId}!\n");
        }

        public async Task ResendVerificationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // To prevent email enumeration, return silently
                return;
            }

            // Check if email is already verified
            if (user.EmailConfirmed)
            {
                throw new Exception("Email is already verified");
            }

            // Generate new verification token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = System.Net.WebUtility.UrlEncode(token);
            
            // Generate verification URL
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
            var verificationUrl = $"{frontendUrl}/verify-email?userId={user.Id}&token={encodedToken}";
            
            // Debug: Log verification URL
            Console.WriteLine($"\n📧 VERIFICATION EMAIL DEBUG:");
            Console.WriteLine($"   User ID: {user.Id}");
            Console.WriteLine($"   User Email: {user.Email}");
            Console.WriteLine($"   Frontend URL: {frontendUrl}");
            Console.WriteLine($"   Verification URL: {verificationUrl}");
            Console.WriteLine($"   Token (first 50 chars): {token.Substring(0, Math.Min(50, token.Length))}...");
            Console.WriteLine($"   Encoded Token (first 50 chars): {encodedToken.Substring(0, Math.Min(50, encodedToken.Length))}...\n");
            
            var body = SmartCampus.Business.Helpers.EmailTemplates.GetVerificationEmail(user.FirstName ?? user.Email!, verificationUrl);

            await _emailService.SendEmailAsync(user.Email!, "Verify your email - Smart Campus", body);
            
            Console.WriteLine($"✅ Verification email sent to: {user.Email}");
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // To prevent email enumeration, we might want to return silently
                // But for now/dev, let's throw or just return
                return; 
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // URL-encode the token for safe transmission in URL
            var encodedToken = System.Net.WebUtility.UrlEncode(token);
            
            // Generate reset URL
            // In development: use localhost, in production: use actual frontend URL
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
            var resetUrl = $"{frontendUrl}/reset-password?email={System.Net.WebUtility.UrlEncode(user.Email!)}&token={encodedToken}";
            var body = SmartCampus.Business.Helpers.EmailTemplates.GetPasswordResetEmail(resetUrl);

            // Also log raw token for easy copy-paste in development
            Console.WriteLine($"\n🔑 RAW TOKEN (use this in Swagger): {token}\n");

            await _emailService.SendEmailAsync(user.Email!, "Reset your password", body);
        }

        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            Console.WriteLine($"\n🔑 RESET PASSWORD REQUEST:");
            Console.WriteLine($"   Email: {email}");
            Console.WriteLine($"   Token length (received): {token?.Length ?? 0}");
            Console.WriteLine($"   Token (first 50): {token?.Substring(0, Math.Min(50, token?.Length ?? 0))}...");
            
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                Console.WriteLine($"❌ User not found: {email}");
                throw new Exception("Invalid request");
            }

            // Token decoding - handle potential double encoding (like in VerifyEmailAsync)
            string tokenToUse = token;
            
            // Check if token contains URL encoding characters
            if (token.Contains("%"))
            {
                Console.WriteLine($"   Token contains % - attempting URL decode...");
                // Try multiple decodes in case of double encoding
                var decoded = System.Net.WebUtility.UrlDecode(token);
                var decoded2 = System.Net.WebUtility.UrlDecode(decoded);
                
                // Use the most decoded version that's different from original
                if (decoded2 != token && decoded2 != decoded)
                {
                    tokenToUse = decoded2;
                    Console.WriteLine($"   Double decoded token (length: {tokenToUse.Length})");
                }
                else if (decoded != token)
                {
                    tokenToUse = decoded;
                    Console.WriteLine($"   Single decoded token (length: {tokenToUse.Length})");
                }
            }
            else
            {
                Console.WriteLine($"   Token does not contain % - using as-is");
            }
            
            Console.WriteLine($"   Token length (final): {tokenToUse.Length}");
            Console.WriteLine($"   Token (first 30 chars): {tokenToUse.Substring(0, Math.Min(30, tokenToUse.Length))}...");
            Console.WriteLine($"   Token (last 30 chars): ...{tokenToUse.Substring(Math.Max(0, tokenToUse.Length - 30))}");

            var result = await _userManager.ResetPasswordAsync(user, tokenToUse, newPassword);
            
            Console.WriteLine($"   ResetPasswordAsync result: {result.Succeeded}");
            
            if (!result.Succeeded)
            {
                var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"❌ Errors: {errorMessages}");
                
                // Check if token might be truncated
                if (tokenToUse.Length < 100)
                {
                    Console.WriteLine($"⚠️ WARNING: Token length is very short ({tokenToUse.Length} chars). Email client may have truncated the URL!");
                    throw new Exception("Şifre sıfırlama başarısız: Token kesilmiş görünüyor. Lütfen email'deki linki doğrudan kullanın veya yeni bir şifre sıfırlama linki isteyin.");
                }
                
                throw new Exception("Şifre sıfırlama başarısız: " + errorMessages);
            }
            
            Console.WriteLine($"✅ Password reset successful for user: {email}\n");
            await LogActivityAsync(user.Id, "ResetPassword", "User reset their password via email token.");
        }

        public async Task<TokenDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken) || refreshToken == "undefined" || refreshToken.Trim() == "")
                throw new Exception("Refresh token is required");

            // Trim token to handle any whitespace issues
            refreshToken = refreshToken.Trim();

            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            
            // Get all tokens for the user and filter in memory (for debugging)
            // In production, you might want to add UserId to the query
            var allTokens = await tokenRepo.FindAsync(t => t.Token == refreshToken);
            var storedToken = allTokens.FirstOrDefault();

            if (storedToken == null) 
            {
                // For debugging: Get all recent tokens to see what's in the database
                var recentTokens = await tokenRepo.GetAllAsync();
                var recentTokenList = recentTokens.Take(5).Select(t => new { 
                    Id = t.Id, 
                    UserId = t.UserId, 
                    TokenPreview = t.Token.Substring(0, Math.Min(20, t.Token.Length)),
                    ExpiryDate = t.ExpiryDate,
                    IsRevoked = t.IsRevoked
                }).ToList();
                
                // Log for debugging - in production, don't expose this
                throw new Exception($"Invalid refresh token. Token not found in database. Looking for: {refreshToken.Substring(0, Math.Min(20, refreshToken.Length))}...");
            }
            if (storedToken.ExpiryDate < DateTime.UtcNow) throw new Exception("Refresh token expired");
            if (storedToken.IsRevoked) throw new Exception("Refresh token revoked");

            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
            if (user == null) throw new Exception("User not found");

            // Revoke current token
            storedToken.IsRevoked = true;
            tokenRepo.Update(storedToken);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(user.Id, "RefreshToken", "User refreshed their access token.");

            return await GenerateTokensAsync(user);
        }

        public async Task LogoutAsync(int userId)
        {
             var tokenRepo = _unitOfWork.Repository<RefreshToken>();
             var tokens = await tokenRepo.FindAsync(t => t.UserId == userId && !t.IsRevoked);
             
             foreach (var token in tokens)
             {
                 token.IsRevoked = true;
                 tokenRepo.Update(token);
             }
             await _unitOfWork.CompleteAsync();
             
             await LogActivityAsync(userId, "Logout", "User logged out.");
        }

        private async Task LogActivityAsync(int userId, string action, string description = "")
        {
            try 
            {
                var now = DateTime.UtcNow;
                var log = new UserActivityLog
                {
                    UserId = userId,
                    Action = action,
                    Description = description,
                    Timestamp = now,
                    CreatedAt = now,  // BaseEntity property
                    UpdatedAt = now,   // BaseEntity property - must be set for NOT NULL column
                    IsDeleted = false  // BaseEntity property
                    // IpAddress could be passed down or retrieved if IHttpContextAccessor was injected
                };
                await _unitOfWork.Repository<UserActivityLog>().AddAsync(log);
                await _unitOfWork.CompleteAsync();
            }
            catch 
            {
                // Logging should not break the main flow
            }
        }

        private async Task<TokenDto> GenerateTokensAsync(User user, bool rememberMe = false)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            // Get user roles from Identity
            var roles = await _userManager.GetRolesAsync(user);
            Console.WriteLine($"🔑 Generating token for user {user.Id} with roles: [{string.Join(", ", roles)}]");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            
            // Add role claims to JWT token (CRITICAL for [Authorize(Roles = "...")] to work)
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role)); // Also add as "role" for compatibility
            }
            
            Console.WriteLine($"✅ Added {roles.Count} role claim(s) to JWT token");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpirationMinutes"]!)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            var refreshToken = Guid.NewGuid().ToString();
            
            // Save Refresh Token - extend expiry if RememberMe is checked
            var refreshTokenExpiryDays = rememberMe ? 30 : 7;
            var now = DateTime.UtcNow;
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = now.AddDays(refreshTokenExpiryDays),
                IsRevoked = false,
                CreatedAt = now,
                UpdatedAt = now,  // BaseEntity property - must be set for NOT NULL column
                IsDeleted = false  // BaseEntity property
            };
            
            await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity);
            await _unitOfWork.CompleteAsync();

            return new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = tokenDescriptor.Expires.Value
            };
        }
    }
}

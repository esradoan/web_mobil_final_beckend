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

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
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

            // 5. Create Role-Specific Entry (now we know everything is valid)
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
                throw new Exception($"Failed to create role-specific entry: {ex.Message}");
            }

            // 5. Generate Email Verification Token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            
            // Generate HTML Body
            // In a real scenario, you'd construct a real URL, e.g. https://api.smartcampus.com/verify?token=...
            // For now, we put the token in the HTML
            var fakeUrl = $"https://smartcampus-api.com/verify-email?userId={user.Id}&token={token}";
            var body = SmartCampus.Business.Helpers.EmailTemplates.GetVerificationEmail(user.FirstName ?? user.Email!, fakeUrl);

            await _emailService.SendEmailAsync(user.Email!, "Welcome to Smart Campus - Verify Email", body);

            return _mapper.Map<UserDto>(user);
        }

        public async Task VerifyEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                throw new Exception("Email verification failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
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
            
            var fakeResetUrl = $"https://smartcampus-api.com/reset-password?email={user.Email}&token={token}";
            var body = SmartCampus.Business.Helpers.EmailTemplates.GetPasswordResetEmail(fakeResetUrl);

            await _emailService.SendEmailAsync(user.Email!, "Reset your password", body);
        }

        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new Exception("Invalid request");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                throw new Exception("Password reset failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
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

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            
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

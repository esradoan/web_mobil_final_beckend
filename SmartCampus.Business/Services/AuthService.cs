using System;
using System.IdentityModel.Tokens.Jwt;
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
            // 1. Find User by Email
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                throw new Exception("Invalid credentials");
            }
            
            if (!_userManager.Options.SignIn.RequireConfirmedEmail || await _userManager.IsEmailConfirmedAsync(user))
            {
                // Proceed
            }
            else 
            {
                 // Optional: throw exception if email not confirmed
                 // throw new Exception("Email not confirmed");
            }

            // 2. Generate Tokens
            return await GenerateTokensAsync(user);
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            // 1. Check if exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new Exception("User with this email already exists");
            }

            // 2. Map DTO to Entity
            var user = _mapper.Map<User>(registerDto);
            user.UserName = registerDto.Email; // Identity requires UserName
            user.CreatedAt = DateTime.UtcNow;

            // 3. Create User (Identity handles Hashing and Saving)
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Registration failed: {errors}");
            }

            // 4. Create Role-Specific Entry
            if (registerDto.Role == UserRole.Student)
            {
                if (string.IsNullOrEmpty(registerDto.StudentNumber) || registerDto.DepartmentId == null)
                    throw new Exception("Student Number and Department are required for Students.");

                var student = new Student
                {
                    UserId = user.Id,
                    StudentNumber = registerDto.StudentNumber,
                    DepartmentId = registerDto.DepartmentId.Value
                };
                await _unitOfWork.Repository<Student>().AddAsync(student);
            }
            else if (registerDto.Role == UserRole.Faculty)
            {
                if (registerDto.DepartmentId == null) 
                     if (string.IsNullOrEmpty(registerDto.EmployeeNumber) || registerDto.DepartmentId == null)
                        throw new Exception("Employee Number and Department are required for Faculty.");

                var faculty = new Faculty
                {
                    UserId = user.Id,
                    EmployeeNumber = registerDto.EmployeeNumber,
                    DepartmentId = registerDto.DepartmentId.Value,
                    Title = "Instructor" 
                };
                await _unitOfWork.Repository<Faculty>().AddAsync(faculty);
            }

            await _unitOfWork.CompleteAsync();

            // 5. Generate Email Verification Token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            // In real app, encode token for URL
            // var encodedToken = System.Web.HttpUtility.UrlEncode(token); 
            // Send Email
            await _emailService.SendEmailAsync(user.Email!, "Verify your email", $"Your verification token is: {token}. Use this to verify your email via POST /api/v1/auth/verify-email");

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

        public async Task<TokenDto> RefreshTokenAsync(string refreshToken)
        {
            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            var storedToken = (await tokenRepo.FindAsync(t => t.Token == refreshToken)).FirstOrDefault();

            if (storedToken == null) throw new Exception("Invalid refresh token");
            if (storedToken.ExpiryDate < DateTime.UtcNow) throw new Exception("Refresh token expired");
            if (storedToken.IsRevoked) throw new Exception("Refresh token revoked");

            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
            if (user == null) throw new Exception("User not found");

            // Revoke current token
            storedToken.IsRevoked = true;
            tokenRepo.Update(storedToken);
            await _unitOfWork.CompleteAsync();

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
        }

        private async Task<TokenDto> GenerateTokensAsync(User user)
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
            
            // Save Refresh Token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7), // per PDF requirement
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
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

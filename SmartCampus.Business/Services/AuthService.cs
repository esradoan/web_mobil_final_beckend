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

        public AuthService(UserManager<User> userManager, IMapper mapper, IConfiguration configuration, SmartCampus.DataAccess.Repositories.IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _mapper = mapper;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        public async Task<TokenDto> LoginAsync(LoginDto loginDto)
        {
            // 1. Find User by Email
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                throw new Exception("Invalid credentials");
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
                if (registerDto.DepartmentId == null) // Title/EmployeeNumber might be optional or auto-generated? Let's check DTO. Assuming EmployeeNumber required.
                     // The PDF Requirements say: Faculty tablosu (user_id, employee_number, title, department_id)
                     // Code below assumes Title is optional or handled elsewhere, but EmployeeNumber should be passed.
                     if (string.IsNullOrEmpty(registerDto.EmployeeNumber) || registerDto.DepartmentId == null)
                        throw new Exception("Employee Number and Department are required for Faculty.");

                var faculty = new Faculty
                {
                    UserId = user.Id,
                    EmployeeNumber = registerDto.EmployeeNumber,
                    DepartmentId = registerDto.DepartmentId.Value,
                    Title = "Instructor" // Default title or add to DTO
                };
                await _unitOfWork.Repository<Faculty>().AddAsync(faculty);
            }

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<UserDto>(user);
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

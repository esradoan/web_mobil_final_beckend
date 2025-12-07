using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartCampus.Business.DTOs;
using SmartCampus.DataAccess.Repositories;
using SmartCampus.Entities;
using BCrypt.Net;

namespace SmartCampus.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<TokenDto> LoginAsync(LoginDto loginDto)
        {
            // 1. Find User
            var users = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == loginDto.Email);
            var user = users.FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new Exception("Invalid credentials");
            }

            // 2. Generate Tokens
            return GenerateTokens(user);
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            // 1. Check if exists
            var existingUsers = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == registerDto.Email);
            if (existingUsers.Any())
            {
                throw new Exception("User with this email already exists");
            }

            // 2. Map DTO to Entity
            var user = _mapper.Map<User>(registerDto);

            // 3. Hash Password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
            user.CreatedAt = DateTime.UtcNow;

            // 4. Save
            await _unitOfWork.Repository<User>().AddAsync(user);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<UserDto>(user);
        }

        private TokenDto GenerateTokens(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
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

            // Simple Refresh Token (should be saved to DB in real implementation, skipping for brevity in this step)
            var refreshToken = Guid.NewGuid().ToString();

            return new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = tokenDescriptor.Expires.Value
            };
        }
    }
}

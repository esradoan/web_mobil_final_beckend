using AutoMapper;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Mappings;
using SmartCampus.Entities;
using Xunit;

namespace SmartCampus.Tests.Mappings
{
    public class MappingProfileTests
    {
        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;

        public MappingProfileTests()
        {
            _configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile()); // Fix: Use the correct constructor for MapperConfiguration  
            });

            _mapper = _configuration.CreateMapper();
        }

        [Fact]
        public void Configuration_ShouldBeValid()
        {
            // Assert  
            _configuration.AssertConfigurationIsValid();
        }

        [Fact]
        public void Should_Map_RegisterDto_To_User_Correctly()
        {
            // Arrange  
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "SecretPassword123"
            };

            // Act  
            var user = _mapper.Map<User>(registerDto);

            // Assert  
            Assert.Equal(registerDto.Email, user.Email);
            Assert.Equal(registerDto.FirstName, user.FirstName);
            Assert.Equal(registerDto.LastName, user.LastName);
            Assert.Null(user.PasswordHash); // Should be ignored/null  
        }

        [Fact]
        public void Should_Map_User_To_UserDto_Correctly()
        {
            // Arrange  
            var user = new User
            {
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Id = 123
            };

            // Act  
            var userDto = _mapper.Map<UserDto>(user);

            // Assert  
            Assert.Equal(user.Email, userDto.Email);
            Assert.Equal(user.FirstName, userDto.FirstName);
            Assert.Equal(user.LastName, userDto.LastName);
            Assert.Equal(user.Id, userDto.Id);
        }
    }
}

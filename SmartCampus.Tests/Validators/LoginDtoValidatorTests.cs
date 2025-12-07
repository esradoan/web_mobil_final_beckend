using FluentValidation.TestHelper;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Validators;
using Xunit;

namespace SmartCampus.Tests.Validators
{
    public class LoginDtoValidatorTests
    {
        private readonly LoginDtoValidator _validator;

        public LoginDtoValidatorTests()
        {
            _validator = new LoginDtoValidator();
        }

        [Fact]
        public void Should_Not_Have_Error_When_Credentials_Are_Valid()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            // Act
            var result = _validator.TestValidate(loginDto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Empty()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "",
                Password = "Password123!"
            };

            // Act
            var result = _validator.TestValidate(loginDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Invalid()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "invalid-email",
                Password = "Password123!"
            };

            // Act
            var result = _validator.TestValidate(loginDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Is_Empty()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = ""
            };

            // Act
            var result = _validator.TestValidate(loginDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }
}

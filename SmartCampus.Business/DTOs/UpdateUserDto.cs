using SmartCampus.Entities;

namespace SmartCampus.Business.DTOs
{
    public class UpdateUserDto
    {
        public string? Email { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserRole? Role { get; set; } // Admin can change user roles
        public int? DepartmentId { get; set; } // Required when changing to Student or Faculty
        // Add other updatable fields as needed
    }
}

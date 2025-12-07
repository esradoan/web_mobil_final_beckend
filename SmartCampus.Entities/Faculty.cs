using System;

namespace SmartCampus.Entities
{
    public class Faculty : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string EmployeeNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty; // e.g., Assoc. Prof. Dr.
        
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
    }
}

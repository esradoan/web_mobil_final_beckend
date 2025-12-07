using System;

namespace SmartCampus.Entities
{
    public class Department : BaseEntity
    {
        public string Name { get; set; } = string.Empty; // e.g., Computer Engineering
        public string Code { get; set; } = string.Empty; // e.g., CENG
        public string FacultyName { get; set; } = string.Empty; // e.g., Faculty of Engineering (School name)
    }
}

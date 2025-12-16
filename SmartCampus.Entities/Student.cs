using System;

namespace SmartCampus.Entities
{
    public class Student : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string StudentNumber { get; set; } = string.Empty;
        
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public decimal GPA { get; set; }
        public decimal CGPA { get; set; }
        
        /// <summary>
        /// Öğrencinin aktif/pasif durumu. Pasif öğrenciler ders kaydı yapamaz, yoklama veremez.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}

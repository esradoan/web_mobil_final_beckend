using System;

namespace SmartCampus.Entities
{
    public interface IAuditEntity : IEntity
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
        bool IsDeleted { get; set; }
    }
}

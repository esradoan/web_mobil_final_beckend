using System;
using System.ComponentModel.DataAnnotations;

namespace SmartCampus.Entities
{
    public class IoTSensor : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "Generic"; // Temperature, Humidity, Energy, Occupancy

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Offline, Maintenance

        // Caching the last known value for quick dashboard access
        public double? LastValue { get; set; }
        [MaxLength(20)]
        public string? Unit { get; set; }
        public DateTime? LastUpdate { get; set; }
        
        public bool IsActive { get; set; } = true;
        public virtual ICollection<SensorData> Data { get; set; } = new List<SensorData>();
    }
}

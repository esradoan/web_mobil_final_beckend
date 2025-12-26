using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCampus.Entities
{
    public class SensorData : BaseEntity
    {
        public int SensorId { get; set; }
        [ForeignKey("SensorId")]
        public virtual IoTSensor? Sensor { get; set; }

        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

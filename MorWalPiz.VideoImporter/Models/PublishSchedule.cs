using System;
using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.VideoImporter.Models
{
    public class PublishSchedule
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Days of the week as a bitmask (1=Monday, 2=Tuesday, 4=Wednesday, 8=Thursday, 16=Friday, 32=Saturday, 64=Sunday)
        /// </summary>
        public int DaysOfWeek { get; set; }
        
        /// <summary>
        /// Time to publish on the specified days
        /// </summary>
        public TimeSpan PublishTime { get; set; }
        
        
        /// <summary>
        /// Whether this schedule is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Created date for tracking
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Multi-tenant support
        public int TenantId { get; set; }
    }
}

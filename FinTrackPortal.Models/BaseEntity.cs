namespace FinTrackPortal.Models
{
    /// <summary>
    /// Audit columns shared by all database entities.
    /// Every table includes CreatedBy, ModifiedBy, CreatedDate, ModifiedDate, and IsActive.
    /// </summary>
    public class BaseEntity
    { 
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}

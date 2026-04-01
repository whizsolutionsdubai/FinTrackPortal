using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Maps to the [dbo].[Groups] table. Each group has an auto-generated
    /// <see cref="GroupCode"/> (e.g. "DF-A1B2") created in the repository layer.
    /// </summary>
    [Table("Groups", Schema = "dbo")]
    public class Group : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long GroupId { get; set; }

        [Required]
        [StringLength(150)]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(10)]
        public string GroupCode { get; set; } = string.Empty;

        public long? CreatedByMemberId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinTrackPortal.Models
{
    [Table("Groups", Schema = "dbo")]
    public class Group : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long GroupId { get; set; }

        [Required]
        [StringLength(150)]
        public string GroupName { get; set; } = string.Empty;

        public long? CreatedByMemberId { get; set; }
    }
}

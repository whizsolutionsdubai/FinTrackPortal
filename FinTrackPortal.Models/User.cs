using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Maps to the [dbo].[Users] table. Holds login credentials
    /// and is linked to a <see cref="Member"/> via MemberId.
    /// </summary>
    [Table("Users", Schema = "dbo")]
    public class User : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string EmailAddress { get; set; } = string.Empty;
        [StringLength(100)]
        public string Mobile { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;  // hashed

        // Navigation
        public ICollection<Member> Members { get; set; } = new List<Member>();
    }
}

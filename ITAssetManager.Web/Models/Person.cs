using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManager.Web.Models
{
    public class Person
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        // UI-only helper. Do NOT use inside EF queries.
        [NotMapped]
        public string FullName
        {
            get => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
                ? "Unknown"
                : $"{FirstName} {LastName}".Trim();
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var parts = value.Split(' ', 2);
                    FirstName = parts.Length > 0 ? parts[0] : "";
                    LastName = parts.Length > 1 ? parts[1] : "";
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ITAssetManager.Web.Models
{
    public class SoftwareProduct
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Vendor")]
        public string? Vendor { get; set; }

        [StringLength(50)]
        [Display(Name = "Version")]
        public string? Version { get; set; }

        [StringLength(100)]
        [Display(Name = "Publisher")]
        public string? Publisher { get; set; }

        [StringLength(50)]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Relationships
        public ICollection<SoftwareLicense> Licenses { get; set; } = new List<SoftwareLicense>();
        public ICollection<LicenseAssignment> Assignments { get; set; } = new List<LicenseAssignment>();

        // Calculated properties from related licenses
        [NotMapped]
        [Display(Name = "Total Licenses")]
        public int TotalLicenses => Licenses?.Count ?? 0;

        [NotMapped]
        [Display(Name = "Total Seats Purchased")]
        public int TotalSeatsPurchased => Licenses?.Sum(l => l.SeatsPurchased ?? 0) ?? 0;

        [NotMapped]
        [Display(Name = "Total Seats Assigned")]
        public int TotalSeatsAssigned => Licenses?.Sum(l => l.SeatsAssigned ?? 0) ?? 0;

        [NotMapped]
        [Display(Name = "Total Seats Available")]
        public int SeatsAvailable => TotalSeatsPurchased - TotalSeatsAssigned;

        [NotMapped]
        [Display(Name = "Next Expiry Date")]
        public DateTime? NextExpiryDate => Licenses?
            .Where(l => l.ExpiryDate.HasValue)
            .OrderBy(l => l.ExpiryDate)
            .Select(l => l.ExpiryDate)
            .FirstOrDefault();

        [NotMapped]
        [Display(Name = "Total Cost")]
        [DataType(DataType.Currency)]
        public decimal TotalCost => Licenses?.Sum(l => l.Cost ?? 0) ?? 0;
    }
}
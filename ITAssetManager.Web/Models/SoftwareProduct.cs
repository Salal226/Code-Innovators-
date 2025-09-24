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
        public string Name { get; set; } = string.Empty;

        public string? Vendor { get; set; }
        public string? Version { get; set; }
        public decimal? UnitCost { get; set; }

        // Optional categorization (your views mentioned them)
        public string? Publisher { get; set; }
        public string? Category { get; set; }

        // Properties that your views expect - adding them directly to SoftwareProduct
        public string? LicenseKey { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LicenseExpiry { get; set; }

        // Alias for LicenseExpiry (if your views use both names)
        [NotMapped]
        public DateTime? ExpiryDate
        {
            get => LicenseExpiry;
            set => LicenseExpiry = value;
        }

        // Seat management properties
        public int SeatCount { get; set; }
        public int SeatsAssigned { get; set; }
        public int SeatsInUse { get; set; }
        public int SeatsPurchased { get; set; }

        // Calculated properties based on related licenses
        [NotMapped]
        public int TotalSeats => Licenses.Sum(l => l.SeatsPurchased ?? 0);

        [NotMapped]
        public int TotalSeatsAssigned => Licenses.Sum(l => l.SeatsAssigned ?? 0);

        [NotMapped]
        public int TotalSeatsInUse => Assignments.Count(a => a.AssignedOn.HasValue);

        [NotMapped]
        public int SeatsAvailable => TotalSeats - TotalSeatsAssigned;

        [NotMapped]
        public DateTime? NextExpiryDate => Licenses
            .Where(l => l.ExpiryDate.HasValue)
            .OrderBy(l => l.ExpiryDate)
            .Select(l => l.ExpiryDate)
            .FirstOrDefault();

        [NotMapped]
        public bool IsExpired => NextExpiryDate.HasValue && NextExpiryDate.Value < DateTime.Today;

        [NotMapped]
        public bool IsExpiringSoon => NextExpiryDate.HasValue && NextExpiryDate.Value <= DateTime.Today.AddDays(30);

        // Relationships
        public ICollection<SoftwareLicense> Licenses { get; set; } = new List<SoftwareLicense>();
        public ICollection<LicenseAssignment> Assignments { get; set; } = new List<LicenseAssignment>();
    }
}
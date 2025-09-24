using System;
using System.ComponentModel.DataAnnotations;

namespace ITAssetManager.Web.Models
{
    public class SoftwareLicense
    {
        public int Id { get; set; }

        // Foreign key to SoftwareProduct
        public int SoftwareProductId { get; set; }
        public SoftwareProduct? Product { get; set; }

        [StringLength(200)]
        public string? LicenseKey { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        public int? SeatsPurchased { get; set; }
        public int? SeatsAssigned { get; set; }

        [DataType(DataType.Currency)]
        public decimal? Cost { get; set; }

        [StringLength(100)]
        public string? Vendor { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Calculated properties
        public int SeatsAvailable => (SeatsPurchased ?? 0) - (SeatsAssigned ?? 0);
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today;
        public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Today.AddDays(30);
    }
}
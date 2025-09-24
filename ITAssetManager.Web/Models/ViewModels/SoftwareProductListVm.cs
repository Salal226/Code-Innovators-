namespace ITAssetManager.Web.Models.ViewModels
{
    public class SoftwareProductListVm
    {
        // Identity / basic info
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? Publisher { get; set; }
        public string? Category { get; set; }

        // LIST COLUMNS (make sure your Index view binds to these names)
        public int Licenses { get; set; }        // count of license rows (or seats purchased when no licenses)
        public int TotalSeats { get; set; }      // sum of seats (or product-level fallback)
        public int InUse { get; set; }           // seats currently in use (or product-level fallback)

        // Convenience
        public int SeatsAvailable => Math.Max(0, TotalSeats - InUse);

        // Expiry
        public DateTime? NextExpiry { get; set; }    // earliest license expiry (or product-level fallback)
        public bool IsExpired => NextExpiry.HasValue && NextExpiry.Value.Date < DateTime.Today;
        public bool IsExpiringSoon => NextExpiry.HasValue && NextExpiry.Value.Date <= DateTime.Today.AddDays(30);
        public string ExpiryStatus =>
            !NextExpiry.HasValue ? "-" :
            IsExpired ? "Expired" :
            IsExpiringSoon ? "Expiring Soon" : "Active";

        // Optional product-level fields (used on Details/Edit or as fallbacks in controller)
        public string? LicenseKey { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? LicenseExpiry { get; set; } // single product-level expiry, if your schema has it

        // Optional: keep these if your pages still expect them somewhere else
        public int? SeatCount { get; set; }          // product-level seat count (nullable if DB column is)
        public int? SeatsAssigned { get; set; }
        public int? SeatsPurchased { get; set; }

        // Human-readable seat status (useful in tooltips/UI)
        public string SeatStatus => $"{InUse}/{TotalSeats} seats used";
    }
}

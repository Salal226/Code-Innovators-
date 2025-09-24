using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManager.Web.Models
{
    public class Asset
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string? AssetTag { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? Model { get; set; }

        [StringLength(50)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [DataType(DataType.Currency)]
        public decimal? PurchaseCost { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        // ---- WARRANTY ----
        // Map to the existing DB column named WarrantyEnd
        [Column("WarrantyEnd")]
        public DateTime? WarrantyExpiry { get; set; }

        // Optional UI alias (NOT mapped). Do NOT use in EF queries.
        [NotMapped]
        [DataType(DataType.Date)]
        public DateTime? WarrantyEnd
        {
            get => WarrantyExpiry;
            set => WarrantyExpiry = value;
        }

        [StringLength(20)]
        public string? Status { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // FKs
        public int? LocationId { get; set; }
        public int? PersonId { get; set; }

        // Navigation
        public Location? Location { get; set; }
        public Person? Person { get; set; }

        // Back-compat alias for Person
        [NotMapped]
        public int? AssignedToPersonId
        {
            get => PersonId;
            set => PersonId = value;
        }

        [NotMapped]
        public Person? AssignedToPerson
        {
            get => Person;
            set => Person = value;
        }

        // Collections
        // BEFORE (causes the error if you used ICollection)
        public ICollection<LicenseAssignment> LicenseAssignments { get; set; } = new List<LicenseAssignment>();
        public ICollection<MaintenanceTicket> Tickets { get; set; } = new List<MaintenanceTicket>();

    }
}

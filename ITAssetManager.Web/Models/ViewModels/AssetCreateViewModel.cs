using System.ComponentModel.DataAnnotations;

namespace ITAssetManager.Web.Models.ViewModels
{
    public class AssetCreateViewModel
    {
        // Asset core properties
        [Required]
        [StringLength(100)]
        [Display(Name = "Asset Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Brand")]
        public string? Brand { get; set; }

        [Required]
        [Display(Name = "Asset Tag")]
        public string AssetTag { get; set; } = "";

        [StringLength(100)]
        [Display(Name = "Category")]
        public string Category { get; set; } = "Laptop";

        [StringLength(100)]
        [Display(Name = "Model")]
        public string? Model { get; set; }

        [StringLength(50)]
        [Display(Name = "Serial Number")]
        public string? SerialNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime? PurchaseDate { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Purchase Cost")]
        public decimal? PurchaseCost { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Warranty End")]
        public DateTime? WarrantyEnd { get; set; }

        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Choose existing (dropdowns)
        [Display(Name = "Assigned To")]
        public int? AssignedToPersonId { get; set; }

        [Display(Name = "Location")]
        public int? LocationId { get; set; }

        // OR add new Person inline
        [Display(Name = "New Person Name")]
        public string? NewPersonName { get; set; }

        [EmailAddress]
        [Display(Name = "New Person Email")]
        public string? NewPersonEmail { get; set; }

        // OR add new Location inline
        [Display(Name = "New Building")]
        public string? NewLocationBuilding { get; set; }

        [Display(Name = "New Room")]
        public string? NewLocationRoom { get; set; }

        [Display(Name = "New Lab #")]
        public string? NewLocationLabNumber { get; set; }
    }
}
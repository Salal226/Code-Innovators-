using System.ComponentModel.DataAnnotations;

namespace ITAssetManager.Web.Models.ViewModels
{
    public class AssetCreateViewModel
    {
        // Asset core
        [Required] public string AssetTag { get; set; } = "";
        public string Category { get; set; } = "Laptop";
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        [DataType(DataType.Date)] public DateTime? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }
        [DataType(DataType.Date)] public DateTime? WarrantyEnd { get; set; }
        public string Status { get; set; } = "Active";

        // Choose existing (dropdowns)
        [Display(Name = "Assigned To")]
        public int? AssignedToPersonId { get; set; }

        [Display(Name = "Location")]
        public int? LocationId { get; set; }

        // OR add new Person inline
        [Display(Name = "New Person Name")]
        public string? NewPersonName { get; set; }
        [EmailAddress, Display(Name = "New Person Email")]
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

using System;
using System.ComponentModel.DataAnnotations;

namespace ITAssetManager.Web.Models
{
    public class LicenseAssignment
    {
        public int Id { get; set; }

        [Display(Name = "Software Product")]
        public int SoftwareProductId { get; set; }
        public SoftwareProduct? SoftwareProduct { get; set; }

        [Display(Name = "Asset")]
        public int? AssetId { get; set; }
        public Asset? Asset { get; set; }

        [Display(Name = "Assigned To")]
        public int? PersonId { get; set; }
        public Person? Person { get; set; }

        [Display(Name = "Assigned On")]
        [DataType(DataType.Date)]
        public DateTime? AssignedOn { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
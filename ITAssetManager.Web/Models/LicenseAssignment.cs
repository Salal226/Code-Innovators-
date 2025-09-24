using System;
using System.ComponentModel.DataAnnotations;

namespace ITAssetManager.Web.Models
{
    public class LicenseAssignment
    {
        public int Id { get; set; }
        public int SoftwareProductId { get; set; }
        public SoftwareProduct? SoftwareProduct { get; set; }
        public int? AssetId { get; set; }
        public Asset? Asset { get; set; }
        public int? PersonId { get; set; }
        public Person? Person { get; set; }
        public DateTime? AssignedOn { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
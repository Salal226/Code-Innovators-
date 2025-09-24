using System.ComponentModel.DataAnnotations;

namespace ITAssetManager.Web.Models
{
    public class Location
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string? Building { get; set; }

        [StringLength(50)]
        public string? Room { get; set; }

        [StringLength(50)]
        public string? Floor { get; set; }

        [StringLength(50)]
        public string? LabNumber { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Department { get; set; }

        public string DisplayName =>
            !string.IsNullOrEmpty(LabNumber)
                ? $"{Building} - Lab {LabNumber}"
                : !string.IsNullOrEmpty(Room)
                    ? $"{Building} - {Room}"
                    : Building ?? "Unknown Location";
    }
}
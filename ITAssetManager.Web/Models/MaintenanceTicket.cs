using System;
using System.ComponentModel.DataAnnotations;

namespace ITAssetManager.Web.Models
{
    public class MaintenanceTicket
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public Asset? Asset { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Status { get; set; }
    }
}